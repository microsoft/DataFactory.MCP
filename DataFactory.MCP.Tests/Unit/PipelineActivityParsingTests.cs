using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using DataFactory.MCP.Models.Pipeline.Definition;
using DataFactory.MCP.Parsing;
using Xunit;

namespace DataFactory.MCP.Tests.Unit;

public class PipelineActivityParsingTests
{
    [Fact]
    public void ActivityParse_PreservesUnknownContentAndReadsRequiredFields()
    {
        const string json = """
            {"name":"Future","type":"FutureActivity","typeProperties":["opaque",null],
             "future":{"enabled":true,"values":[1,"café"]}}
            """;

        var document = PipelineActivityDocument.Parse(json);

        Assert.Equal("Future", document.Name);
        Assert.Equal("FutureActivity", document.Type);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(json), document.Content));
    }

    [Theory]
    [InlineData("not-json{", "Invalid activityJson format")]
    [InlineData("null", "activityJson must be a JSON object")]
    [InlineData("[]", "activityJson must be a JSON object")]
    [InlineData("17", "activityJson must be a JSON object")]
    public void ActivityParse_InvalidRoot_Throws(string json, string message)
    {
        var exception = Assert.Throws<ArgumentException>(() => PipelineActivityDocument.Parse(json));

        Assert.Contains(message, exception.Message);
        if (message.StartsWith("Invalid", StringComparison.Ordinal))
            Assert.IsAssignableFrom<JsonException>(exception.InnerException);
    }

    [Theory]
    [InlineData("""{"name":"New","type":"Wait",}""")]
    [InlineData("""{/*comment*/"name":"New","type":"Wait"}""")]
    public void ActivityParse_NonStrictJson_ThrowsWithInnerJsonException(string json)
    {
        var exception = Assert.Throws<ArgumentException>(() => PipelineActivityDocument.Parse(json));

        Assert.IsAssignableFrom<JsonException>(exception.InnerException);
    }

    [Theory]
    [InlineData("""{"name":17,"type":"Wait"}""", true)]
    [InlineData("""{"name":"New","type":{}}""", false)]
    [InlineData("""{"name":"New","type":true}""", false)]
    public void ActivityAccessors_NonStringKinds_Throw(string json, bool readName)
    {
        var document = PipelineActivityDocument.Parse(json);

        Assert.Throws<InvalidOperationException>(() =>
            _ = readName ? document.Name : document.Type);
    }

    [Fact]
    public void ActivityAccessors_MissingOrNull_ReturnNull()
    {
        var document = PipelineActivityDocument.Parse("""{"name":null}""");

        Assert.Null(document.Name);
        Assert.Null(document.Type);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void DependsOnOverride_NullOrEmpty_LeavesInlineValue(string? dependsOnJson)
    {
        var document = PipelineActivityDocument.Parse(
            """{"name":"New","type":"Wait","dependsOn":{"invalid":"but untouched"}}""");

        document.ApplyDependsOnOverride(dependsOnJson);

        Assert.IsType<JsonObject>(document.Content["dependsOn"]);
    }

    [Theory]
    [InlineData(" ", "Invalid dependsOnJson format")]
    [InlineData("not-json{", "Invalid dependsOnJson format")]
    [InlineData("null", "dependsOnJson must be a JSON array")]
    [InlineData("{}", "dependsOnJson must be a JSON array")]
    public void DependsOnOverride_InvalidInput_Throws(string json, string message)
    {
        var document = PipelineActivityDocument.Parse("""{"name":"New","type":"Wait"}""");

        var exception = Assert.Throws<ArgumentException>(
            () => document.ApplyDependsOnOverride(json));

        Assert.Contains(message, exception.Message);
        if (message.StartsWith("Invalid", StringComparison.Ordinal))
            Assert.IsAssignableFrom<JsonException>(exception.InnerException);
    }

    [Fact]
    public void DependsOnOverride_ArrayReplacesInlineValueAndCanClearIt()
    {
        var document = PipelineActivityDocument.Parse(
            """{"name":"New","type":"Wait","dependsOn":{"invalid":true},"future":null}""");

        document.ApplyDependsOnOverride("""[null,{"activity":"First","future":[1]}]""");
        Assert.True(JsonNode.DeepEquals(
            JsonNode.Parse("""[null,{"activity":"First","future":[1]}]"""),
            document.Content["dependsOn"]));

        document.ApplyDependsOnOverride("[]");
        Assert.Empty(Assert.IsType<JsonArray>(document.Content["dependsOn"]));
        Assert.True(document.Content.ContainsKey("future"));
    }

    [Fact]
    public void ContentParseAndSerialize_PreservesSemanticContentAndUsesOnePartEnvelope()
    {
        const string json = """
            {"name":"café","futureRoot":[null,2],"properties":{"future":{"x":true},
             "activities":[{"name":"A","type":"Future","dependsOn":[{"activity":null,"future":3}],
             "typeProperties":["opaque",null]}]}}
            """;
        var document = PipelineContentDocument.Parse(CreateDefinition(json));

        var definition = document.ToDefinition();

        var part = Assert.Single(definition.Parts);
        Assert.Equal("pipeline-content.json", part.Path);
        Assert.Equal("InlineBase64", part.PayloadType);
        var serialized = Encoding.UTF8.GetString(Convert.FromBase64String(part.Payload));
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(json), JsonNode.Parse(serialized)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetActivities_MissingOrNull_CreateOnlyWhenRequested(bool explicitNull)
    {
        var document = PipelineContentDocument.Parse(CreateDefinition(explicitNull
            ? """{"properties":{"activities":null}}"""
            : """{"properties":{}}"""));

        Assert.Throws<ArgumentException>(() => document.GetActivities(createIfMissing: false));
        Assert.Empty(document.GetActivities(createIfMissing: true));
        Assert.IsType<JsonArray>(document.Content["properties"]!["activities"]);
    }

    [Fact]
    public void GetActivities_WrongKind_ThrowsWithoutReplacingIt()
    {
        var document = PipelineContentDocument.Parse(
            CreateDefinition("""{"properties":{"activities":{}}}"""));

        Assert.Throws<InvalidOperationException>(
            () => document.GetActivities(createIfMissing: true));
        Assert.IsType<JsonObject>(document.Content["properties"]!["activities"]);
    }

    [Theory]
    [InlineData("not-base64", typeof(FormatException))]
    [InlineData("bm90LWpzb257", typeof(JsonException))]
    [InlineData("bnVsbA==", typeof(ArgumentException))]
    [InlineData("W10=", typeof(InvalidOperationException))]
    [InlineData("e30=", typeof(ArgumentException))]
    [InlineData("eyJwcm9wZXJ0aWVzIjpudWxsfQ==", typeof(ArgumentException))]
    [InlineData("eyJwcm9wZXJ0aWVzIjpbXX0=", typeof(InvalidOperationException))]
    public void ContentParse_InvalidPayload_Throws(string payload, Type exceptionType)
    {
        var definition = DefinitionWithPayload(payload);

        var exception = Record.Exception(() => PipelineContentDocument.Parse(definition));
        Assert.IsAssignableFrom(exceptionType, exception);
    }

    [Fact]
    public void ContentParse_MalformedServiceEnvelope_ThrowsDeliberately()
    {
        Assert.Throws<InvalidOperationException>(() => PipelineContentDocument.Parse(null!));
        Assert.Throws<InvalidOperationException>(() => PipelineContentDocument.Parse(
            new PipelineDefinition { Parts = null! }));
        Assert.Throws<InvalidOperationException>(() => PipelineContentDocument.Parse(
            new PipelineDefinition { Parts = [null!] }));
        Assert.Throws<InvalidOperationException>(() => PipelineContentDocument.Parse(
            new PipelineDefinition
            {
                Parts = [new PipelineDefinitionPart { Path = "pipeline-content.json", Payload = null! }]
            }));
        Assert.Throws<ArgumentException>(() => PipelineContentDocument.Parse(new PipelineDefinition()));
    }

    private static PipelineDefinition CreateDefinition(string json) =>
        DefinitionWithPayload(Convert.ToBase64String(Encoding.UTF8.GetBytes(json)));

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
}
