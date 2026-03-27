---
name: tester.testing-patterns
description: "xUnit testing patterns and conventions for DataFactory.MCP. Use when writing tests, fixing test failures, or adding test coverage."
---

# Testing in DataFactory.MCP

## Test Stack

- **Framework:** xUnit (`[Fact]`, `[Theory]`, `[InlineData]`)
- **Runner:** `xunit.runner.visualstudio`
- **Skippable:** `Xunit.SkippableFact` (`[SkippableFact]`, `Skip.If(...)`)
- **Coverage:** coverlet
- **Target:** net10.0

## Test Location

All tests live in `DataFactory.MCP.Tests/`.

## Commands

| Task | Command |
|------|---------|
| Run all | `dotnet test` |
| Verbose | `dotnet test -v normal` |
| Filter | `dotnet test --filter "FullyQualifiedName~TestName"` |
| Coverage | `dotnet test --collect:"XPlat Code Coverage"` |

## Naming Convention

```
MethodName_Scenario_ExpectedResult
```

Examples:
- `Authenticate_WithValidCredentials_ReturnsToken`
- `ListGateways_WhenNotAuthenticated_ThrowsException`
- `CreateConnection_WithInvalidType_ReturnsError`

## Test Patterns

### Unit Test (Arrange-Act-Assert)

```csharp
public class MyServiceTests
{
    [Fact]
    public void MethodName_Scenario_ExpectedResult()
    {
        // Arrange
        var service = new MyService();

        // Act
        var result = service.DoSomething();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("expected", result.Value);
    }
}
```

### Parameterized Tests

```csharp
[Theory]
[InlineData("input1", "expected1")]
[InlineData("input2", "expected2")]
public void MethodName_WithVariousInputs_ReturnsExpected(string input, string expected)
{
    var result = MyService.Process(input);
    Assert.Equal(expected, result);
}
```

### Skippable Tests (for environment-dependent tests)

```csharp
[SkippableFact]
public void IntegrationTest_RequiresAuth()
{
    Skip.If(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AZURE_CLIENT_ID")),
        "Requires Azure credentials");
    // test body
}
```

## What to Test

- **Tools:** Parameter validation, response formatting, error handling
- **Services:** API call construction, response parsing, error translation
- **Models:** Serialization/deserialization roundtrips
- **Edge cases:** Null inputs, empty collections, unauthenticated state, API errors

## Evaluation Scenarios

Reference `evals/` for integration test scenarios:
- `authentication.eval.md`, `connections.eval.md`, `dataflows.eval.md`
- `gateways.eval.md`, `pipelines.eval.md`, `multi-step.eval.md`
