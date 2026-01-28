using System.Net.Http.Json;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Configuration;
using DataFactory.MCP.Infrastructure.Http;
using DataFactory.MCP.Models.Dataflow.BackgroundTask;
using DataFactory.MCP.Services.BackgroundTasks.Jobs;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;

namespace DataFactory.MCP.Services.BackgroundTasks;

/// <summary>
/// High-level service for dataflow refresh operations.
/// Composes IBackgroundJobMonitor with DataflowRefreshJob.
/// </summary>
public class DataflowRefreshService : IDataflowRefreshService
{
    private readonly IBackgroundJobMonitor _jobMonitor;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoggerFactory _loggerFactory;

    public DataflowRefreshService(
        IBackgroundJobMonitor jobMonitor,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory)
    {
        _jobMonitor = jobMonitor;
        _httpClientFactory = httpClientFactory;
        _loggerFactory = loggerFactory;
    }

    public async Task<DataflowRefreshResult> StartRefreshAsync(
        McpSession session,
        string workspaceId,
        string dataflowId,
        string? displayName = null,
        string executeOption = ExecuteOptions.SkipApplyChanges,
        List<ItemJobParameter>? parameters = null)
    {
        // Create the job
        var job = new DataflowRefreshJob(
            _httpClientFactory,
            _loggerFactory.CreateLogger<DataflowRefreshJob>(),
            workspaceId,
            dataflowId,
            displayName,
            executeOption,
            parameters);

        // Start and monitor it
        var result = await _jobMonitor.StartJobAsync(job, session);

        // Map to DataflowRefreshResult for backward compatibility
        return new DataflowRefreshResult
        {
            IsComplete = result.IsComplete,
            Status = result.Status,
            ErrorMessage = result.ErrorMessage,
            Context = result.Context as DataflowRefreshContext,
            EndTimeUtc = result.CompletedAt
        };
    }

    public async Task<DataflowRefreshResult> GetStatusAsync(DataflowRefreshContext context)
    {
        var httpClient = _httpClientFactory.CreateClient(HttpClientNames.FabricApi);

        var endpoint = $"workspaces/{context.WorkspaceId}/items/{context.DataflowId}/jobs/instances/{context.JobInstanceId}";
        var url = FabricUrlBuilder.ForFabricApi().WithLiteralPath(endpoint).Build();

        var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var jobInstance = await response.Content.ReadFromJsonAsync<ItemJobInstance>(
            JsonSerializerOptionsProvider.FabricApi);

        if (jobInstance == null)
        {
            throw new InvalidOperationException("Failed to deserialize job instance response");
        }

        var isComplete = jobInstance.Status is "Completed" or "Failed" or "Cancelled" or "Deduped";

        return new DataflowRefreshResult
        {
            IsComplete = isComplete,
            Status = jobInstance.Status ?? "Unknown",
            Context = context,
            EndTimeUtc = jobInstance.EndTimeUtc,
            FailureReason = jobInstance.FailureReason?.Message
        };
    }

    public IReadOnlyList<TrackedTask> GetAllTasks() => _jobMonitor.GetAllTasks();

    public TrackedTask? GetTask(string taskId) => _jobMonitor.GetTask(taskId);
}
