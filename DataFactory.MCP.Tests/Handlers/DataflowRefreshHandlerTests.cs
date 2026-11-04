using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Extensions;
using DataFactory.MCP.Handlers.Dataflow;
using DataFactory.MCP.Models;
using DataFactory.MCP.Models.Dataflow.BackgroundTask;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using Xunit;

namespace DataFactory.MCP.Tests.Handlers;

public class DataflowRefreshHandlerTests
{
    [Fact]
    public void AddDataFactoryMcpServices_ResolvesHandlerWithoutNotificationHost()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataFactoryMcpServices();

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<DataflowRefreshHandler>());
    }

    [Fact]
    public async Task StartAsync_WithEmptyWorkspaceId_ReturnsValidationError()
    {
        var service = new StubRefreshService();
        var handler = new DataflowRefreshHandler(service);

        var result = await handler.StartAsync("", "dataflow-id");

        Assert.False(result.IsSuccess);
        Assert.Equal("validation", result.ErrorType);
        Assert.Equal(Messages.InvalidParameterEmpty("workspaceId"), result.Error);
        Assert.False(service.WasCalled);
    }

    [Fact]
    public async Task StartAsync_DelegatesToHostNeutralService()
    {
        var service = new StubRefreshService();
        var handler = new DataflowRefreshHandler(service);

        var result = await handler.StartAsync(
            "workspace-id",
            "dataflow-id",
            "Sales refresh",
            ExecuteOptions.ApplyChangesIfNeeded);

        Assert.True(result.IsSuccess);
        Assert.True(service.WasCalled);
        Assert.Equal("workspace-id", service.WorkspaceId);
        Assert.Equal("dataflow-id", service.DataflowId);
        Assert.Equal("Sales refresh", service.DisplayName);
        Assert.Equal(ExecuteOptions.ApplyChangesIfNeeded, service.ExecuteOption);
        Assert.Equal("InProgress", result.Value!.Status);
    }

    private sealed class StubRefreshService : IDataflowRefreshService
    {
        public bool WasCalled { get; private set; }
        public string? WorkspaceId { get; private set; }
        public string? DataflowId { get; private set; }
        public string? DisplayName { get; private set; }
        public string? ExecuteOption { get; private set; }

        public Task<DataflowRefreshResult> StartRefreshAsync(
            string workspaceId,
            string dataflowId,
            string? displayName = null,
            string executeOption = ExecuteOptions.SkipApplyChanges,
            List<ItemJobParameter>? parameters = null)
        {
            WasCalled = true;
            WorkspaceId = workspaceId;
            DataflowId = dataflowId;
            DisplayName = displayName;
            ExecuteOption = executeOption;

            return Task.FromResult(new DataflowRefreshResult
            {
                IsComplete = false,
                Status = "InProgress"
            });
        }

        public Task<DataflowRefreshResult> StartRefreshAsync(
            McpSession session,
            string workspaceId,
            string dataflowId,
            string? displayName = null,
            string executeOption = ExecuteOptions.SkipApplyChanges,
            List<ItemJobParameter>? parameters = null)
            => throw new NotSupportedException();

        public Task<DataflowRefreshResult> GetStatusAsync(DataflowRefreshContext context)
            => throw new NotSupportedException();
    }
}
