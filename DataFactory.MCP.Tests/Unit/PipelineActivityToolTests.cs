using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Handlers.Pipeline;
using DataFactory.MCP.Models;
using DataFactory.MCP.Models.Pipeline;
using DataFactory.MCP.Models.Pipeline.Definition;
using DataFactory.MCP.Models.Pipeline.Schedule;
using DataFactory.MCP.Services;
using DataFactory.MCP.Tools.Pipeline;
using Xunit;

namespace DataFactory.MCP.Tests.Unit;

/// <summary>
/// Offline tool-boundary regressions. Fake service completion does not establish Fabric
/// schema acceptance, completed backend validation, or pipeline runtime validity.
/// </summary>
public class PipelineActivityToolTests
{
    private const string WorkspaceId = "00000000-0000-0000-0000-000000000001";
    private const string PipelineId = "00000000-0000-0000-0000-000000000002";
    private const string Target = """
        {"name":"Target","type":"Wait","policy":{"retry":3},"typeProperties":{"waitTimeInSeconds":5},"oldOnly":true}
        """;
    private const string Sibling = """
        {
          "name":"Sibling","type":"TridentNotebook","dependsOn":[],
          "typeProperties":{
            "notebookId":"00000000-0000-0000-0000-000000000003",
            "workspaceId":"00000000-0000-0000-0000-000000000004",
            "parameters":{"mode":{"value":"preserve café","type":"string"}}
          },
          "futureSibling":{"enabled":true,"values":[1,null,"keep"]}
        }
        """;
    private const string Tail = """{"name":"Tail","type":"Wait","typeProperties":{"waitTimeInSeconds":1}}""";
    private const string ForEachContainer = """
        {
          "name":"Container","type":"ForEach","dependsOn":[],
          "typeProperties":{
            "items":{"value":"@pipeline().parameters.items","type":"Expression"},
            "isSequential":true,
            "activities":[
              {"name":"Inner","type":"Wait","typeProperties":{"waitTimeInSeconds":1},"futureChild":{"text":"keep café","nullable":null}},
              {"name":"AfterInner","type":"Wait","dependsOn":[{"activity":"Inner","dependencyConditions":["Succeeded"]}],"typeProperties":{"waitTimeInSeconds":2}}
            ],
            "futureContainer":{"values":[false,{"keep":"nested"}]}
          },
          "futureActivity":{"activities":[{"name":"Unmodeled","value":17}]}
        }
        """;
    private const string SwitchContainer = """
        {
          "name":"Container","type":"Switch","dependsOn":[],
          "typeProperties":{
            "on":{"value":"@pipeline().parameters.mode","type":"Expression"},
            "cases":[
              {"value":"one","activities":[
                {"name":"Inner","type":"Wait","typeProperties":{"waitTimeInSeconds":1}},
                {"name":"AfterInner","type":"Wait","dependsOn":[{"activity":"Inner","dependencyConditions":["Completed"]}]}
              ],"futureCase":{"preserve":true}},
              {"value":"two","activities":[{"name":"OtherBranch","type":"FutureActivity","typeProperties":{"opaque":[1,null]}}]}
            ],
            "defaultActivities":[
              {"name":"Fallback","type":"Wait","typeProperties":{"waitTimeInSeconds":2}},
              {"name":"AfterFallback","type":"Wait","dependsOn":[{"activity":"Fallback","dependencyConditions":["Failed"]}]}
            ],
            "futureContainer":{"values":["keep",null]}
          }
        }
        """;

    public static TheoryData<string, string> RequiredActivityFieldCases => new()
    {
        { """{"type":"Wait"}""", Messages.InvalidParameterEmpty("activity.name") },
        { """{"name":null,"type":"Wait"}""", Messages.InvalidParameterEmpty("activity.name") },
        { """{"name":"","type":"Wait"}""", Messages.InvalidParameterEmpty("activity.name") },
        { """{"name":" ","type":"Wait"}""", Messages.InvalidParameterEmpty("activity.name") },
        { """{"name":"New"}""", Messages.InvalidParameterEmpty("activity.type") },
        { """{"name":"New","type":null}""", Messages.InvalidParameterEmpty("activity.type") },
        { """{"name":"New","type":""}""", Messages.InvalidParameterEmpty("activity.type") },
        { """{"name":"New","type":" "}""", Messages.InvalidParameterEmpty("activity.type") }
    };

    [Theory]
    [InlineData("Target", "Replaced", 3)]
    [InlineData("target", "Added", 4)]
    public async Task UpsertPipelineActivityAsync_ExactTopLevelName_ReplacesWholeObjectOrAppendsInOrder(
        string name, string operation, int count)
    {
        // Arrange
        var service = CreateService(CreateContent(Sibling, Target, Tail));
        var tool = CreateTool(service);
        var activity = $$$"""{"name":"{{{name}}}","type":"WebActivity","typeProperties":{"method":"GET"}}""";
        var expected = operation == "Replaced"
            ? CreateContent(Sibling, activity, Tail)
            : CreateContent(Sibling, Target, Tail, activity);

        // Act
        var result = await tool.UpsertPipelineActivityAsync(WorkspaceId, PipelineId, activity);

        // Assert: omitted old policy/typeProperties/extension fields are not merged back.
        AssertUpsertSuccess(result, name, "WebActivity", operation, count);
        AssertSubmittedContent(service, expected);
    }

    [Fact]
    public async Task UpsertPipelineActivityAsync_DuplicateTopLevelNames_ReplacesFirstMatchOnly()
    {
        const string first = """{"name":"Duplicate","type":"Wait","position":1}""";
        const string second = """{"name":"Duplicate","type":"Wait","position":2}""";
        const string replacement = """{"name":"Duplicate","type":"Future","position":3}""";
        var service = CreateService(CreateContent(first, second, Sibling));
        var tool = CreateTool(service);

        var result = await tool.UpsertPipelineActivityAsync(
            WorkspaceId, PipelineId, replacement);

        AssertUpsertSuccess(result, "Duplicate", "Future", "Replaced", 3);
        AssertSubmittedContent(service, CreateContent(replacement, second, Sibling));
    }

    [Theory]
    [InlineData("ForEach")]
    [InlineData("Switch")]
    public async Task UpsertPipelineActivityAsync_NestedOnlyName_AppendsTopLevelWithoutChangingChild(string containerType)
    {
        var container = GetContainer(containerType);
        var service = CreateService(CreateContent(container, Sibling));
        var tool = CreateTool(service);
        const string activity = """{"name":"Inner","type":"WebActivity","typeProperties":{"method":"POST"}}""";

        var result = await tool.UpsertPipelineActivityAsync(WorkspaceId, PipelineId, activity);

        AssertUpsertSuccess(result, "Inner", "WebActivity", "Added", 3);
        AssertSubmittedContent(service, CreateContent(container, Sibling, activity));
    }

    [Theory]
    [InlineData("ForEach", "Inner")]
    [InlineData("Switch", "Inner")]
    [InlineData("Switch", "Fallback")]
    public async Task RemovePipelineActivityAsync_NestedOnlyName_ReturnsNotFoundWithoutSaving(
        string containerType, string name)
    {
        var original = CreateContent(GetContainer(containerType), Sibling);
        var service = CreateService(original);
        var tool = CreateTool(service);

        var result = await tool.RemovePipelineActivityAsync(WorkspaceId, PipelineId, name);

        AssertError(result, "ValidationError", $"Activity '{name}' not found");
        AssertNoUpdate(service, expectedGetCount: 1);
        AssertJsonEqual(original, DecodeContent(service.Definition));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ActivityMutation_IdenticalTopLevelAndNestedNames_OnlyTargetsTopLevel(bool remove)
    {
        const string oldTopLevel = """{"name":"Inner","type":"Wait","oldOnly":true}""";
        const string replacement = """{"name":"Inner","type":"WebActivity","typeProperties":{}}""";
        var service = CreateService(CreateContent(ForEachContainer, oldTopLevel, Sibling));
        var tool = CreateTool(service);

        var result = remove
            ? await tool.RemovePipelineActivityAsync(WorkspaceId, PipelineId, "Inner")
            : await tool.UpsertPipelineActivityAsync(WorkspaceId, PipelineId, replacement);

        if (remove)
            AssertRemoveSuccess(result, "Inner", 2);
        else
            AssertUpsertSuccess(result, "Inner", "WebActivity", "Replaced", 3);
        AssertSubmittedContent(service, remove
            ? CreateContent(ForEachContainer, Sibling)
            : CreateContent(ForEachContainer, replacement, Sibling));
    }

    [Theory]
    [InlineData("ForEach", false)]
    [InlineData("ForEach", true)]
    [InlineData("Switch", false)]
    [InlineData("Switch", true)]
    public async Task ActivityMutation_UnrelatedSibling_PreservesContainerBranchesAndUnknownContent(
        string containerType, bool remove)
    {
        // Nested sibling dependencies intentionally have no top-level targets.
        var container = GetContainer(containerType);
        const string replacement = """{"name":"Target","type":"Wait","typeProperties":{"waitTimeInSeconds":9}}""";
        var service = CreateService(CreateContent(container, Target, Sibling));
        var tool = CreateTool(service);

        var result = remove
            ? await tool.RemovePipelineActivityAsync(WorkspaceId, PipelineId, "Target")
            : await tool.UpsertPipelineActivityAsync(WorkspaceId, PipelineId, replacement);

        if (remove)
            AssertRemoveSuccess(result, "Target", 2);
        else
            AssertUpsertSuccess(result, "Target", "Wait", "Replaced", 3);
        AssertSubmittedContent(service, remove
            ? CreateContent(container, Sibling)
            : CreateContent(container, replacement, Sibling));
    }

    [Fact]
    public async Task UpsertPipelineActivityAsync_WholeContainerReplacement_DoesNotMergeOmittedChildren()
    {
        const string replacement = """
            {
              "name":"Container","type":"ForEach",
              "typeProperties":{"items":[1],"activities":[
                {"name":"ReplacementChild","type":"Wait","typeProperties":{"waitTimeInSeconds":7}}
              ]}
            }
            """;
        var service = CreateService(CreateContent(Sibling, ForEachContainer, Tail));
        var tool = CreateTool(service);

        var result = await tool.UpsertPipelineActivityAsync(WorkspaceId, PipelineId, replacement);

        AssertUpsertSuccess(result, "Container", "ForEach", "Replaced", 3);
        AssertSubmittedContent(service, CreateContent(Sibling, replacement, Tail));
    }

    [Theory]
    [InlineData("Inner")]
    [InlineData("Fallback")]
    [InlineData("Missing")]
    [InlineData("container")]
    public async Task UpsertPipelineActivityAsync_DependencyWithoutExactTopLevelTarget_DoesNotSave(string dependency)
    {
        var service = CreateService(CreateContent(SwitchContainer, Sibling));
        var tool = CreateTool(service);
        var activity = $$"""
            {"name":"New","type":"Wait","dependsOn":[{"activity":"{{dependency}}","dependencyConditions":["Succeeded"]}]}
            """;

        var result = await tool.UpsertPipelineActivityAsync(WorkspaceId, PipelineId, activity);

        AssertError(result, "ValidationError", $"depends on '{dependency}' which does not exist");
        AssertNoUpdate(service, expectedGetCount: 1);
    }

    [Theory]
    [InlineData("Missing", "Succeeded", "does not exist")]
    [InlineData("Broken", "Succeeded", "cannot depend on itself")]
    [InlineData("Sibling", "InvalidCondition", "Invalid dependency condition")]
    public async Task UpsertPipelineActivityAsync_ExistingTopLevelDependencyViolation_DoesNotSave(
        string dependency, string condition, string error)
    {
        var broken = $$"""
            {"name":"Broken","type":"Wait","dependsOn":[{"activity":"{{dependency}}","dependencyConditions":["{{condition}}"]}]}
            """;
        var service = CreateService(CreateContent(Sibling, broken));
        var tool = CreateTool(service);

        var result = await tool.UpsertPipelineActivityAsync(WorkspaceId, PipelineId, Tail);

        AssertError(result, "ValidationError", error);
        AssertNoUpdate(service, expectedGetCount: 1);
    }

    [Fact]
    public async Task RemovePipelineActivityAsync_TopLevelDependent_PreventsRemoval()
    {
        const string dependent = """
            {"name":"Dependent","type":"Wait","dependsOn":[{"activity":"Target","dependencyConditions":["Succeeded"]}]}
            """;
        var service = CreateService(CreateContent(Target, dependent, Sibling));
        var tool = CreateTool(service);

        var result = await tool.RemovePipelineActivityAsync(WorkspaceId, PipelineId, "Target");

        AssertError(result, "ValidationError", "Cannot remove activity 'Target' because it is referenced by: Dependent");
        AssertNoUpdate(service, expectedGetCount: 1);
    }

    [Fact]
    public async Task RemovePipelineActivityAsync_DifferentNameCase_ReturnsNotFoundWithoutSaving()
    {
        var service = CreateService(CreateContent(Target, Sibling));
        var tool = CreateTool(service);

        var result = await tool.RemovePipelineActivityAsync(WorkspaceId, PipelineId, "target");

        AssertError(result, "ValidationError", "Activity 'target' not found");
        AssertNoUpdate(service, expectedGetCount: 1);
    }

    [Theory]
    [InlineData("Succeeded")]
    [InlineData("Failed")]
    [InlineData("Skipped")]
    [InlineData("Completed")]
    [InlineData("sUcCeEdEd")]
    [InlineData(null)]
    public async Task UpsertPipelineActivityAsync_DependsOnOverride_ReplacesInlineDependenciesBeforeValidation(
        string? condition)
    {
        const string activity = """
            {"name":"New","type":"Wait","dependsOn":[{"activity":"New","dependencyConditions":["InvalidCondition"]}],"typeProperties":{"waitTimeInSeconds":1}}
            """;
        var dependencies = condition is null
            ? "[]"
            : $$"""[{"activity":"Sibling","dependencyConditions":["{{condition}}"]}]""";
        var expectedActivity = ParseObject(activity);
        expectedActivity["dependsOn"] = JsonNode.Parse(dependencies);
        var service = CreateService(CreateContent(Sibling));
        var tool = CreateTool(service);

        var result = await tool.UpsertPipelineActivityAsync(WorkspaceId, PipelineId, activity, dependencies);

        AssertUpsertSuccess(result, "New", "Wait", "Added", 2);
        AssertSubmittedContent(service, CreateContent(Sibling, expectedActivity.ToJsonString()));
    }

    [Theory]
    [InlineData("""{"name":"New","type":"TridentNotebook"}""")]
    [InlineData("""{"name":"New","type":"TridentNotebook","typeProperties":{"notebookId":"notebook-only"}}""")]
    [InlineData("""{"name":"New","type":"WebActivity","typeProperties":{}}""")]
    [InlineData("""{"name":"New","type":"Copy","typeProperties":{}}""")]
    [InlineData("""{"name":"New","type":"DataflowActivity","typeProperties":{}}""")]
    [InlineData("""{"name":"New","type":"FutureActivity","typeProperties":{"unknown":[1,null,{"preserve":true}]}}""")]
    [InlineData("""{"name":"New","type":"WebActivity","typeProperties":["opaque",{"future":null}]}""")]
    [InlineData("""{"name":"New","type":"TridentNotebook","typeProperties":"opaque"}""")]
    [InlineData("""{"name":"New","type":"FutureActivity","typeProperties":42}""")]
    [InlineData("""{"name":"New","type":"FutureActivity","typeProperties":null}""")]
    public async Task UpsertPipelineActivityAsync_OpaqueTypePayload_IsForwardedWithoutLocalSchemaWarnings(string activity)
    {
        // These include incomplete and non-object payloads, not claims of Fabric acceptance.
        var service = CreateService(CreateContent(Sibling));
        var tool = CreateTool(service);

        var result = await tool.UpsertPipelineActivityAsync(WorkspaceId, PipelineId, activity);

        AssertUpsertSuccess(result, "New", ParseObject(activity)["type"]!.GetValue<string>(), "Added", 2);
        // Full semantic equality also rules out synthesized IDs/properties/defaults.
        AssertSubmittedContent(service, CreateContent(Sibling, activity));
    }

    [Theory]
    [InlineData("null", null, "activityJson must be a JSON object")]
    [InlineData("[]", null, "activityJson must be a JSON object")]
    [InlineData("""{"name":"New","type":"Wait"}""", "not-json{", "Invalid dependsOnJson format")]
    [InlineData("""{"name":"New","type":"Wait"}""", "{}", "dependsOnJson must be a JSON array")]
    [InlineData("""{"name":"New","type":"Wait"}""", "null", "dependsOnJson must be a JSON array")]
    [InlineData("""{"name":"New","type":"Wait","dependsOn":[{"activity":"New","dependencyConditions":["Succeeded"]}]}""", null, "cannot depend on itself")]
    [InlineData("""{"name":"New","type":"Wait","dependsOn":[{"activity":"Sibling","dependencyConditions":["InvalidCondition"]}]}""", null, "Invalid dependency condition")]
    public async Task UpsertPipelineActivityAsync_InvalidLocalInput_FailsBeforeServiceCalls(
        string activity, string? dependencies, string error)
    {
        var service = CreateService(CreateContent(Sibling));
        var tool = CreateTool(service);

        var result = await tool.UpsertPipelineActivityAsync(WorkspaceId, PipelineId, activity, dependencies);

        AssertError(result, "ValidationError", error);
        AssertNoUpdate(service, expectedGetCount: 0);
    }

    [Theory]
    [MemberData(nameof(RequiredActivityFieldCases))]
    public async Task UpsertPipelineActivityAsync_InvalidRequiredActivityField_UsesCentralizedMessage(
        string activity, string error)
    {
        var service = CreateService(CreateContent(Sibling));
        var tool = CreateTool(service);

        var result = await tool.UpsertPipelineActivityAsync(WorkspaceId, PipelineId, activity);

        AssertError(result, "ValidationError", error);
        AssertNoUpdate(service, expectedGetCount: 0);
    }

    [Fact]
    public async Task UpsertPipelineActivityAsync_UsesInjectedValidationServiceWithExactActivityLabels()
    {
        var service = CreateService(CreateContent(Sibling));
        var validation = new RecordingValidationService();
        var tool = new PipelineTool(service, validation, new PipelineHandler(service));

        var result = await tool.UpsertPipelineActivityAsync(
            WorkspaceId, PipelineId, """{"name":"New","type":"Wait"}""");

        AssertUpsertSuccess(result, "New", "Wait", "Added", 2);
        Assert.Equal(
            new[] { "workspaceId", "pipelineId", "activityJson", "activity.name", "activity.type" },
            validation.RequiredStringLabels);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public async Task ActivityMutation_ServiceFailure_ReturnsMappedErrorWithoutSuccessOrWarnings(
        bool remove, bool failUpdate, bool authenticationFailure)
    {
        const string failureMessage = "offline fake service failure";
        Exception failure = authenticationFailure
            ? new UnauthorizedAccessException(failureMessage)
            : new HttpRequestException(failureMessage);
        var service = CreateService(CreateContent(Target, Sibling));
        if (failUpdate)
            service.UpdateException = failure;
        else
            service.GetException = failure;
        var tool = CreateTool(service);

        var result = remove
            ? await tool.RemovePipelineActivityAsync(WorkspaceId, PipelineId, "Target")
            : await tool.UpsertPipelineActivityAsync(WorkspaceId, PipelineId, Tail);

        AssertError(result, authenticationFailure ? "AuthenticationError" : "HttpRequestError", failureMessage);
        if (failUpdate)
            AssertSubmittedContent(service, remove ? CreateContent(Sibling) : CreateContent(Target, Sibling, Tail));
        else
            AssertNoUpdate(service, expectedGetCount: 1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ActivityMutation_InvalidOperationServiceFailure_DoesNotReturnSuccess(bool remove)
    {
        var service = CreateService(CreateContent(Target, Sibling));
        service.GetException = new InvalidOperationException("invalid service state");
        var tool = CreateTool(service);

        var result = remove
            ? await tool.RemovePipelineActivityAsync(WorkspaceId, PipelineId, "Target")
            : await tool.UpsertPipelineActivityAsync(WorkspaceId, PipelineId, Tail);

        AssertOperationError(result, "invalid service state");
        AssertNoUpdate(service, expectedGetCount: 1);
    }

    [Theory]
    [InlineData(false, "MalformedBase64", "OperationError", "Base-64")]
    [InlineData(true, "MalformedBase64", "OperationError", "Base-64")]
    [InlineData(false, "MalformedJson", "OperationError", null)]
    [InlineData(true, "MalformedJson", "OperationError", null)]
    [InlineData(false, "NullRoot", "ValidationError", "Failed to parse pipeline content JSON")]
    [InlineData(true, "NullRoot", "ValidationError", "Failed to parse pipeline content JSON")]
    [InlineData(false, "ArrayRoot", "OperationError", "must be an object")]
    [InlineData(true, "ArrayRoot", "OperationError", "must be an object")]
    [InlineData(false, "MissingProperties", "ValidationError", "missing 'properties' object")]
    [InlineData(true, "MissingProperties", "ValidationError", "missing 'properties' object")]
    [InlineData(false, "NullProperties", "ValidationError", "missing 'properties' object")]
    [InlineData(true, "NullProperties", "ValidationError", "missing 'properties' object")]
    [InlineData(false, "WrongProperties", "OperationError", "'properties' must be an object")]
    [InlineData(true, "WrongProperties", "OperationError", "'properties' must be an object")]
    [InlineData(false, "WrongActivities", "OperationError", "'activities' must be an array")]
    [InlineData(true, "WrongActivities", "OperationError", "'activities' must be an array")]
    [InlineData(false, "NullDefinition", "OperationError", "definition is null")]
    [InlineData(true, "NullDefinition", "OperationError", "definition is null")]
    [InlineData(false, "NullParts", "OperationError", "parts are null")]
    [InlineData(true, "NullParts", "OperationError", "parts are null")]
    [InlineData(false, "NullPart", "OperationError", "contains a null part")]
    [InlineData(true, "NullPart", "OperationError", "contains a null part")]
    [InlineData(false, "NullPayload", "OperationError", "payload is null")]
    [InlineData(true, "NullPayload", "OperationError", "payload is null")]
    [InlineData(false, "AbsentContentPart", "ValidationError", "pipeline-content.json")]
    [InlineData(true, "AbsentContentPart", "ValidationError", "pipeline-content.json")]
    public async Task ActivityMutation_MalformedPipelineContent_DoesNotSave(
        bool remove, string scenario, string expectedError, string? expectedMessage)
    {
        var service = new FakePipelineService(CreateMalformedDefinition(scenario));
        var tool = CreateTool(service);

        var result = remove
            ? await tool.RemovePipelineActivityAsync(WorkspaceId, PipelineId, "Target")
            : await tool.UpsertPipelineActivityAsync(WorkspaceId, PipelineId, Target);

        if (expectedError == "OperationError")
            AssertOperationError(result, expectedMessage);
        else
        {
            Assert.NotNull(expectedMessage);
            AssertError(result, expectedError, expectedMessage);
        }
        AssertNoUpdate(service, expectedGetCount: 1);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ActivityMutation_MissingOrNullActivities_UpsertCreatesItButRemoveDoesNotSave(
        bool remove, bool explicitNull)
    {
        var content = CreateContent();
        if (explicitNull)
            content["properties"]!["activities"] = null;
        else
            content["properties"]!.AsObject().Remove("activities");
        var service = CreateService(content);
        var tool = CreateTool(service);

        var result = remove
            ? await tool.RemovePipelineActivityAsync(WorkspaceId, PipelineId, "Target")
            : await tool.UpsertPipelineActivityAsync(WorkspaceId, PipelineId, Target);

        if (remove)
        {
            AssertError(result, "ValidationError", "Pipeline has no activities array");
            AssertNoUpdate(service, expectedGetCount: 1);
        }
        else
        {
            AssertUpsertSuccess(result, "Target", "Wait", "Added", 1);
            AssertSubmittedContent(service, CreateContent(Target));
        }
    }

    [Fact]
    public async Task WebActivityTemplate_CheckedInFile_UsesConnectionBackedFieldsAndSubmitsUnchanged()
    {
        var template = ReadTemplate("activity-web.json");
        Assert.Equal("WebActivity", template["type"]!.GetValue<string>());
        var properties = Assert.IsType<JsonObject>(template["typeProperties"]);
        Assert.Equal("RELATIVE_URL", properties["relativeUrl"]!.GetValue<string>());
        Assert.Equal("METHOD", properties["method"]!.GetValue<string>());
        Assert.False(properties.ContainsKey("url"));
        Assert.False(template.ContainsKey("url"));
        Assert.False(properties.ContainsKey("externalReferences"));
        var references = Assert.IsType<JsonObject>(template["externalReferences"]);
        Assert.Equal("CONNECTION_ID", references["connection"]!.GetValue<string>());

        await AssertTemplateSubmittedUnchangedAsync(template);
    }

    [Fact]
    public async Task NotebookActivityTemplate_CheckedInFile_KeepsNotebookWorkspaceDistinctFromPipelineWorkspace()
    {
        var template = ReadTemplate("activity-notebook.json");
        Assert.Equal("TridentNotebook", template["type"]!.GetValue<string>());
        var properties = Assert.IsType<JsonObject>(template["typeProperties"]);
        Assert.Equal("NOTEBOOK_ID", properties["notebookId"]!.GetValue<string>());
        Assert.Equal("NOTEBOOK_WORKSPACE_ID", properties["workspaceId"]!.GetValue<string>());
        Assert.NotEqual(WorkspaceId, properties["workspaceId"]!.GetValue<string>());
        Assert.False(template.ContainsKey("workspaceId"));

        await AssertTemplateSubmittedUnchangedAsync(template);
    }

    [Theory]
    [InlineData(nameof(PipelineTool.UpsertPipelineActivityAsync))]
    [InlineData(nameof(PipelineTool.RemovePipelineActivityAsync))]
    public void ActivityTool_MethodDescription_ExposesTopLevelContract(string methodName)
    {
        var method = typeof(PipelineTool).GetMethod(methodName);
        Assert.NotNull(method);
        var attribute = method.GetCustomAttribute<DescriptionAttribute>();
        Assert.NotNull(attribute);
        Assert.Contains("top-level", attribute.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("properties.activities", attribute.Description, StringComparison.Ordinal);
        Assert.Contains("nested", attribute.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("case-sensitive", attribute.Description, StringComparison.OrdinalIgnoreCase);
        if (methodName == nameof(PipelineTool.UpsertPipelineActivityAsync))
        {
            Assert.Contains("replac", attribute.Description, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("container", attribute.Description, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Theory]
    [InlineData(nameof(PipelineTool.UpsertPipelineActivityAsync), "activityJson")]
    [InlineData(nameof(PipelineTool.UpsertPipelineActivityAsync), "dependsOnJson")]
    [InlineData(nameof(PipelineTool.RemovePipelineActivityAsync), "activityName")]
    public void ActivityTool_ParameterDescription_ExposesTopLevelScope(string methodName, string parameterName)
    {
        var method = typeof(PipelineTool).GetMethod(methodName);
        Assert.NotNull(method);
        var parameter = Assert.Single(method.GetParameters(), candidate => candidate.Name == parameterName);
        var attribute = parameter.GetCustomAttribute<DescriptionAttribute>();
        Assert.NotNull(attribute);
        Assert.Contains("top-level", attribute.Description, StringComparison.OrdinalIgnoreCase);
    }

    private static PipelineTool CreateTool(FakePipelineService service) =>
        new(service, new ValidationService(), new PipelineHandler(service));

    private static FakePipelineService CreateService(JsonObject content) =>
        new(new PipelineDefinition
        {
            Parts = new List<PipelineDefinitionPart>
            {
                new()
                {
                    Path = "pipeline-content.json",
                    PayloadType = "InlineBase64",
                    Payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(content.ToJsonString()))
                }
            }
        });

    private static PipelineDefinition? CreateMalformedDefinition(string scenario) => scenario switch
    {
        "MalformedBase64" => DefinitionWithPayload("not-base64"),
        "MalformedJson" => DefinitionWithContent("not-json{"),
        "NullRoot" => DefinitionWithContent("null"),
        "ArrayRoot" => DefinitionWithContent("[]"),
        "MissingProperties" => DefinitionWithContent("""{"name":"Pipeline"}"""),
        "NullProperties" => DefinitionWithContent("""{"properties":null}"""),
        "WrongProperties" => DefinitionWithContent("""{"properties":[]}"""),
        "WrongActivities" => DefinitionWithContent("""{"properties":{"activities":{}}}"""),
        "NullDefinition" => null,
        "NullParts" => new PipelineDefinition { Parts = null! },
        "NullPart" => new PipelineDefinition { Parts = [null!] },
        "NullPayload" => DefinitionWithPayload(null!),
        "AbsentContentPart" => new PipelineDefinition
        {
            Parts =
            [
                new PipelineDefinitionPart
                {
                    Path = "other.json",
                    Payload = "",
                    PayloadType = "InlineBase64"
                }
            ]
        },
        _ => throw new ArgumentOutOfRangeException(nameof(scenario))
    };

    private static PipelineDefinition DefinitionWithContent(string content) =>
        DefinitionWithPayload(Convert.ToBase64String(Encoding.UTF8.GetBytes(content)));

    private static PipelineDefinition DefinitionWithPayload(string payload) =>
        new()
        {
            Parts =
            [
                new PipelineDefinitionPart
                {
                    Path = "pipeline-content.json",
                    Payload = payload,
                    PayloadType = "InlineBase64"
                }
            ]
        };

    private static string GetContainer(string type) => type switch
    {
        "ForEach" => ForEachContainer,
        "Switch" => SwitchContainer,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    private static JsonObject CreateContent(params string[] activities)
    {
        var content = ParseObject("""
            {
              "name":"Offline pipeline",
              "futureRoot":{"preserve":[true,null,{"text":"café"}]},
              "properties":{
                "parameters":{"items":{"type":"Array","defaultValue":[1,2]},"mode":{"type":"String","defaultValue":"one"}},
                "variables":{"state":{"type":"String","defaultValue":"ready"}},
                "annotations":["keep"],
                "futureProperties":{"enabled":false}
              }
            }
            """);
        content["properties"]!["activities"] = new JsonArray(activities.Select(activity => JsonNode.Parse(activity)).ToArray());
        return content;
    }

    private static JsonObject ParseObject(string json) => Assert.IsType<JsonObject>(JsonNode.Parse(json));

    private static JsonObject DecodeContent(PipelineDefinition definition)
    {
        var part = Assert.Single(definition.Parts);
        Assert.Equal("pipeline-content.json", part.Path);
        Assert.Equal("InlineBase64", part.PayloadType);
        return ParseObject(Encoding.UTF8.GetString(Convert.FromBase64String(part.Payload)));
    }

    private static void AssertJsonEqual(JsonNode expected, JsonNode actual) =>
        Assert.True(JsonNode.DeepEquals(expected, actual),
            $"Expected JSON: {expected.ToJsonString()}{Environment.NewLine}Actual JSON: {actual.ToJsonString()}");

    private static void AssertSubmittedContent(FakePipelineService service, JsonObject expected)
    {
        Assert.Equal(1, service.GetCount);
        Assert.Equal(1, service.UpdateCount);
        Assert.Equal((WorkspaceId, PipelineId), service.GetIds);
        Assert.Equal((WorkspaceId, PipelineId), service.UpdateIds);
        Assert.NotNull(service.SubmittedDefinition);
        AssertJsonEqual(expected, DecodeContent(service.SubmittedDefinition));
    }

    private static void AssertNoUpdate(FakePipelineService service, int expectedGetCount)
    {
        Assert.Equal(expectedGetCount, service.GetCount);
        Assert.Equal(0, service.UpdateCount);
        Assert.Null(service.SubmittedDefinition);
        Assert.Null(service.UpdateIds);
        if (expectedGetCount == 0)
            Assert.Null(service.GetIds);
        else
            Assert.Equal((WorkspaceId, PipelineId), service.GetIds);
    }

    private static JsonObject AssertSuccess(string result)
    {
        var response = ParseObject(result);
        Assert.True(response["success"]!.GetValue<bool>());
        Assert.Equal(WorkspaceId, response["workspaceId"]!.GetValue<string>());
        Assert.Equal(PipelineId, response["pipelineId"]!.GetValue<string>());
        Assert.False(response.ContainsKey("warnings"));
        Assert.False(response.ContainsKey("error"));
        return response;
    }

    private static void AssertUpsertSuccess(string result, string name, string type, string operation, int count)
    {
        var response = AssertSuccess(result);
        Assert.Equal(name, response["activityName"]!.GetValue<string>());
        Assert.Equal(type, response["activityType"]!.GetValue<string>());
        Assert.Equal(operation, response["operation"]!.GetValue<string>());
        Assert.Equal(count, response["totalActivityCount"]!.GetValue<int>());
    }

    private static void AssertRemoveSuccess(string result, string name, int count)
    {
        var response = AssertSuccess(result);
        Assert.Equal(name, response["removedActivity"]!.GetValue<string>());
        Assert.Equal(count, response["remainingActivityCount"]!.GetValue<int>());
    }

    private static void AssertError(string result, string error, string message)
    {
        var response = ParseObject(result);
        Assert.False(response["success"]!.GetValue<bool>());
        Assert.Equal(error, response["error"]!.GetValue<string>());
        Assert.Contains(message, response["message"]!.GetValue<string>());
        Assert.False(response.ContainsKey("warnings"));
        Assert.False(response.ContainsKey("operation"));
    }

    private static void AssertOperationError(string result, string? message)
    {
        var response = ParseObject(result);
        Assert.False(response["success"]!.GetValue<bool>());
        Assert.Equal("OperationError", response["error"]!.GetValue<string>());
        if (message is not null)
            Assert.Contains(message, response["message"]!.GetValue<string>());
        Assert.NotNull(response["operation"]);
        Assert.False(response.ContainsKey("warnings"));
    }

    private static JsonObject ReadTemplate(string fileName)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        for (var depth = 0; depth < 12 && directory is not null; depth++, directory = directory.Parent)
        {
            var templatePath = Path.Combine(directory.FullName, "claude-skills", "templates", fileName);
            if (!File.Exists(templatePath))
                continue;

            // Skip checked-in guidance comments only here; the tool still receives strict JSON.
            return Assert.IsType<JsonObject>(JsonNode.Parse(File.ReadAllText(templatePath),
                documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip }));
        }

        throw new FileNotFoundException(
            $"Could not locate claude-skills\\templates\\{fileName} within 12 ancestors of '{AppContext.BaseDirectory}'.");
    }

    private static async Task AssertTemplateSubmittedUnchangedAsync(JsonObject template)
    {
        var service = CreateService(CreateContent(Sibling));
        var tool = CreateTool(service);
        var activityJson = template.ToJsonString();

        var result = await tool.UpsertPipelineActivityAsync(WorkspaceId, PipelineId, activityJson);

        AssertUpsertSuccess(result, template["name"]!.GetValue<string>(), template["type"]!.GetValue<string>(), "Added", 2);
        AssertSubmittedContent(service, CreateContent(Sibling, activityJson));
    }

    private sealed class FakePipelineService(PipelineDefinition? definition) : IFabricPipelineService
    {
        public PipelineDefinition Definition { get; } = definition!;
        public PipelineDefinition? SubmittedDefinition { get; private set; }
        public int GetCount { get; private set; }
        public int UpdateCount { get; private set; }
        public (string WorkspaceId, string PipelineId)? GetIds { get; private set; }
        public (string WorkspaceId, string PipelineId)? UpdateIds { get; private set; }
        public Exception? GetException { get; set; }
        public Exception? UpdateException { get; set; }

        public Task<PipelineDefinition> GetPipelineDefinitionAsync(string workspaceId, string pipelineId)
        {
            GetCount++;
            GetIds = (workspaceId, pipelineId);
            return GetException is { } exception
                ? Task.FromException<PipelineDefinition>(exception)
                : Task.FromResult(Definition);
        }

        public Task UpdatePipelineDefinitionAsync(string workspaceId, string pipelineId, PipelineDefinition definition)
        {
            UpdateCount++;
            UpdateIds = (workspaceId, pipelineId);
            SubmittedDefinition = definition;
            return UpdateException is { } exception ? Task.FromException(exception) : Task.CompletedTask;
        }

        public Task<ListPipelinesResponse> ListPipelinesAsync(string workspaceId, string? continuationToken = null) =>
            throw new NotSupportedException();
        public Task<CreatePipelineResponse> CreatePipelineAsync(string workspaceId, CreatePipelineRequest request) =>
            throw new NotSupportedException();
        public Task<Pipeline> GetPipelineAsync(string workspaceId, string pipelineId) =>
            throw new NotSupportedException();
        public Task<Pipeline> UpdatePipelineAsync(string workspaceId, string pipelineId, UpdatePipelineRequest request) =>
            throw new NotSupportedException();
        public Task<string?> RunPipelineAsync(string workspaceId, string pipelineId, JsonElement? executionData = null) =>
            throw new NotSupportedException();
        public Task<ItemJobInstance> GetPipelineJobInstanceAsync(string workspaceId, string pipelineId, string jobInstanceId) =>
            throw new NotSupportedException();
        public Task<ItemSchedule> CreatePipelineScheduleAsync(string workspaceId, string pipelineId, CreateScheduleRequest request) =>
            throw new NotSupportedException();
        public Task<ListSchedulesResponse> ListPipelineSchedulesAsync(string workspaceId, string pipelineId, string? continuationToken = null) =>
            throw new NotSupportedException();
        public Task<ItemSchedule> GetPipelineScheduleAsync(string workspaceId, string pipelineId, string scheduleId) =>
            throw new NotSupportedException();
        public Task<ItemSchedule> SetPipelineScheduleEnabledAsync(string workspaceId, string pipelineId, string scheduleId, bool enabled) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingValidationService : IValidationService
    {
        private readonly ValidationService _inner = new();

        public List<string> RequiredStringLabels { get; } = [];

        public void ValidateAndThrow<T>(T obj, string parameterName) where T : class =>
            _inner.ValidateAndThrow(obj, parameterName);

        public IList<ValidationResult> Validate<T>(T obj) where T : class =>
            _inner.Validate(obj);

        public void ValidateRequiredString(
            string value, string parameterName, int? maxLength = null)
        {
            RequiredStringLabels.Add(parameterName);
            _inner.ValidateRequiredString(value, parameterName, maxLength);
        }

        public void ValidateGuid(string value, string parameterName) =>
            _inner.ValidateGuid(value, parameterName);
    }
}
