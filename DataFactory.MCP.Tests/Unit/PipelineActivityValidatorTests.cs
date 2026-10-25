using System.Text.Json.Nodes;
using DataFactory.MCP.Validation;
using Xunit;

namespace DataFactory.MCP.Tests.Unit;

public class PipelineActivityValidatorTests
{
    [Fact]
    public void ValidateActivityDependencies_SelfDependency_Throws()
    {
        var activity = ParseObject(
            """{"dependsOn":[{"activity":"Current","dependencyConditions":["Succeeded"]}]}""");

        var exception = Assert.Throws<ArgumentException>(() =>
            PipelineActivityValidator.ValidateActivityDependencies(activity, "Current"));

        Assert.Contains("Activity 'Current' cannot depend on itself", exception.Message);
    }

    [Fact]
    public void ValidateActivityDependencies_InvalidCondition_Throws()
    {
        var activity = ParseObject(
            """{"dependsOn":[{"activity":"Other","dependencyConditions":["Invalid"]}]}""");

        var exception = Assert.Throws<ArgumentException>(() =>
            PipelineActivityValidator.ValidateActivityDependencies(activity, "Current"));

        Assert.Contains("Invalid dependency condition 'Invalid'", exception.Message);
    }

    [Fact]
    public void ValidateActivityDependencies_NullAndAbsentFields_AreTolerated()
    {
        PipelineActivityValidator.ValidateActivityDependencies(ParseObject("{}"), "Current");
        PipelineActivityValidator.ValidateActivityDependencies(
            ParseObject("""{"dependsOn":null}"""), "Current");
        PipelineActivityValidator.ValidateActivityDependencies(
            ParseObject("""
                {"dependsOn":[null,{"activity":null,"dependencyConditions":[null]},
                  {"dependencyConditions":null},{}]}
                """),
            "Current");
    }

    [Theory]
    [InlineData("""{"dependsOn":{}}""")]
    [InlineData("""{"dependsOn":[17]}""")]
    [InlineData("""{"dependsOn":[{"activity":17}]}""")]
    [InlineData("""{"dependsOn":[{"dependencyConditions":{}}]}""")]
    [InlineData("""{"dependsOn":[{"dependencyConditions":[17]}]}""")]
    public void ValidateActivityDependencies_MalformedNonNullKinds_Throw(string json)
    {
        Assert.Throws<InvalidOperationException>(() =>
            PipelineActivityValidator.ValidateActivityDependencies(ParseObject(json), "Current"));
    }

    [Theory]
    [InlineData("Succeeded")]
    [InlineData("Failed")]
    [InlineData("Skipped")]
    [InlineData("Completed")]
    [InlineData("sUcCeEdEd")]
    public void ValidateActivityGraph_AllSupportedConditionSpellings_AreAccepted(string condition)
    {
        var activities = ParseArray($$"""
            [{"name":"First"},{"name":"Current","dependsOn":[
              {"activity":"First","dependencyConditions":["{{condition}}"]}]}]
            """);

        PipelineActivityValidator.ValidateActivityGraph(activities);
    }

    [Fact]
    public void ValidateActivityGraph_ExactNamesAndNestedScope_ArePreserved()
    {
        var wrongCase = ParseArray("""
            [{"name":"Target"},{"name":"Current","dependsOn":[{"activity":"target"}]}]
            """);
        var nestedOnly = ParseArray("""
            [{"name":"Container","typeProperties":{"activities":[{"name":"Inner"}]}},
             {"name":"Current","dependsOn":[{"activity":"Inner"}]}]
            """);

        Assert.Throws<ArgumentException>(
            () => PipelineActivityValidator.ValidateActivityGraph(wrongCase));
        Assert.Throws<ArgumentException>(
            () => PipelineActivityValidator.ValidateActivityGraph(nestedOnly));
    }

    [Fact]
    public void ValidateActivityGraph_InvalidDependencyInsideNestedContainer_IsIgnored()
    {
        var activities = ParseArray("""
            [{"name":"Container","typeProperties":{"activities":[
              {"name":"Inner","dependsOn":[
                {"activity":"Missing","dependencyConditions":["Invalid"]}]}]}}]
            """);

        PipelineActivityValidator.ValidateActivityGraph(activities);
    }

    [Theory]
    [InlineData("""[{"name":"Current","dependsOn":{}}]""")]
    [InlineData("""[{"name":"Current","dependsOn":[17]}]""")]
    [InlineData("""[{"name":"Current","dependsOn":[{"activity":17}]}]""")]
    [InlineData("""[{"name":"Current","dependsOn":[{"dependencyConditions":{}}]}]""")]
    [InlineData("""[{"name":"Current","dependsOn":[{"dependencyConditions":[17]}]}]""")]
    [InlineData("""[17]""")]
    [InlineData("""[{"name":17}]""")]
    public void ValidateActivityGraph_MalformedNonNullKinds_Throw(string json)
    {
        Assert.Throws<InvalidOperationException>(() =>
            PipelineActivityValidator.ValidateActivityGraph(ParseArray(json)));
    }

    [Fact]
    public void ValidateActivityGraph_NullAndAbsentDependencyFields_AreTolerated()
    {
        var activities = ParseArray("""
            [null,{"name":"First","dependsOn":null},
             {"name":"Current","dependsOn":[null,{"activity":null,"dependencyConditions":[null]},
               {"dependencyConditions":null},{}]}]
            """);

        PipelineActivityValidator.ValidateActivityGraph(activities);
    }

    [Fact]
    public void ValidateActivityGraph_TwoNodeCycle_IsNotRejected()
    {
        var activities = ParseArray("""
            [{"name":"A","dependsOn":[{"activity":"B"}]},
             {"name":"B","dependsOn":[{"activity":"A"}]}]
            """);

        PipelineActivityValidator.ValidateActivityGraph(activities);
    }

    [Fact]
    public void ValidateActivityGraph_InvalidExistingSibling_IsRejected()
    {
        var activities = ParseArray("""
            [{"name":"MissingTarget","dependsOn":[{"activity":"Missing"}]},{"name":"New"}]
            """);

        var exception = Assert.Throws<ArgumentException>(() =>
            PipelineActivityValidator.ValidateActivityGraph(activities));

        Assert.Contains("does not exist", exception.Message);
    }

    [Fact]
    public void ValidateRemoval_ReportsNamedAndUnnamedTopLevelDependents()
    {
        var activities = ParseArray("""
            [{"name":"Target"},{"name":"Direct","dependsOn":[{"activity":"Target"}]},
             {"dependsOn":[{"activity":"Target"}]},
             {"name":"Container","typeProperties":{"activities":[
               {"name":"Nested","dependsOn":[{"activity":"Target"}]}]}}]
            """);

        var exception = Assert.Throws<ArgumentException>(() =>
            PipelineActivityValidator.ValidateRemoval(activities, "Target"));

        Assert.Contains("Direct, (unnamed)", exception.Message);
        Assert.DoesNotContain("Nested", exception.Message);
    }

    [Fact]
    public void ValidateRemoval_SameNameDuplicate_IsSkipped()
    {
        var activities = ParseArray("""
            [{"name":"Target"},{"name":"Target","dependsOn":[{"activity":"Target"}]}]
            """);

        PipelineActivityValidator.ValidateRemoval(activities, "Target");
    }

    private static JsonObject ParseObject(string json) =>
        Assert.IsType<JsonObject>(JsonNode.Parse(json));

    private static JsonArray ParseArray(string json) =>
        Assert.IsType<JsonArray>(JsonNode.Parse(json));
}
