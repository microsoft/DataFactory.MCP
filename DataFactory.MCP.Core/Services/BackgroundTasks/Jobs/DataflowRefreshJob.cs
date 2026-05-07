using System.Net.Http.Json;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Configuration;
using DataFactory.MCP.Infrastructure.Http;
using DataFactory.MCP.Models.Dataflow.BackgroundTask;
using Microsoft.Extensions.Logging;

namespace DataFactory.MCP.Services.BackgroundTasks.Jobs;

/// <summary>
/// Background job for refreshing a dataflow.
/// Single Responsibility: only handles dataflow refresh API interactions.
/// </summary>
public class DataflowRefreshJob : IBackgroundJob
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DataflowRefreshJob> _logger;
    private readonly string _workspaceId;
    private readonly string _dataflowId;
    private readonly string _executeOption;
    private readonly List<ItemJobParameter>? _parameters;

    private string? _jobInstanceId;
    private DateTime _startedAt;

    public string JobId { get; }
    public string JobType => "Dataflow Refresh";
    public string DisplayName { get; }

    public DataflowRefreshJob(
        IHttpClientFactory httpClientFactory,
        ILogger<DataflowRefreshJob> logger,
        string workspaceId,
        string dataflowId,
        string? displayName = null,
        string executeOption = ExecuteOptions.SkipApplyChanges,
        List<ItemJobParameter>? parameters = null)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _workspaceId = workspaceId;
        _dataflowId = dataflowId;
        _executeOption = executeOption;
        _parameters = parameters;

        JobId = Guid.NewGuid().ToString();
        DisplayName = displayName ?? $"Dataflow {dataflowId[..Math.Min(8, dataflowId.Length)]}...";
    }

    public async Task<BackgroundJobResult> StartAsync()
    {
        _startedAt = DateTime.UtcNow;

        try
        {
            var httpClient = _httpClientFactory.CreateClient(HttpClientNames.FabricApi);

            var endpoint = $"workspaces/{_workspaceId}/dataflows/{_dataflowId}/jobs/Execute/instances";
            var url = FabricUrlBuilder.ForFabricApi().WithLiteralPath(endpoint).Build();

            var request = new RunOnDemandExecuteRequest();
            if (!string.IsNullOrEmpty(_executeOption) || _parameters?.Count > 0)
            {
                request.ExecutionData = new DataflowExecutionPayload
                {
                    ExecuteOption = _executeOption,
                    Parameters = _parameters
                };
            }

            var jsonContent = System.Text.Json.JsonSerializer.Serialize(request,
                JsonSerializerOptionsProvider.FabricApi);
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            _logger.LogInformation("Starting dataflow refresh: POST {Url}", url);

            var response = await httpClient.PostAsync(url, content);

            if (response.StatusCode != System.Net.HttpStatusCode.Accepted)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return new BackgroundJobResult
                {
                    IsComplete = true,
                    IsSuccess = false,
                    Status = "Failed",
                    ErrorMessage = $"Failed to start refresh. Status: {response.StatusCode}, Response: {errorContent}",
                    StartedAt = _startedAt,
                    CompletedAt = DateTime.UtcNow
                };
            }

            // Extract job instance ID from Location header
            var location = response.Headers.Location?.ToString();
            _jobInstanceId = ExtractJobInstanceId(location);

            _logger.LogInformation("Dataflow refresh started. JobInstanceId={JobInstanceId}", _jobInstanceId);

            return new BackgroundJobResult
            {
                IsComplete = false,
                IsSuccess = false,
                Status = "InProgress",
                StartedAt = _startedAt,
                Context = new DataflowRefreshContext
                {
                    WorkspaceId = _workspaceId,
                    DataflowId = _dataflowId,
                    JobInstanceId = _jobInstanceId,
                    DisplayName = DisplayName,
                    StartedAtUtc = _startedAt
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start dataflow refresh for {DataflowId}", _dataflowId);

            return new BackgroundJobResult
            {
                IsComplete = true,
                IsSuccess = false,
                Status = "Failed",
                ErrorMessage = ex.Message,
                StartedAt = _startedAt,
                CompletedAt = DateTime.UtcNow
            };
        }
    }

    public async Task<BackgroundJobResult> CheckStatusAsync()
    {
        if (string.IsNullOrEmpty(_jobInstanceId))
        {
            return new BackgroundJobResult
            {
                IsComplete = true,
                IsSuccess = false,
                Status = "Failed",
                ErrorMessage = "No job instance ID available",
                StartedAt = _startedAt,
                CompletedAt = DateTime.UtcNow
            };
        }

        var httpClient = _httpClientFactory.CreateClient(HttpClientNames.FabricApi);

        var endpoint = $"workspaces/{_workspaceId}/items/{_dataflowId}/jobs/instances/{_jobInstanceId}";
        var url = FabricUrlBuilder.ForFabricApi().WithLiteralPath(endpoint).Build();

        _logger.LogDebug("Polling job status: GET {Url}", url);

        var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var jobInstance = await response.Content.ReadFromJsonAsync<ItemJobInstance>(
            JsonSerializerOptionsProvider.FabricApi);

        if (jobInstance == null)
        {
            throw new InvalidOperationException("Failed to deserialize job instance response");
        }

        var isComplete = jobInstance.Status is "Completed" or "Failed" or "Cancelled" or "Deduped";
        var isSuccess = jobInstance.Status == "Completed";
        var completedAt = isComplete ? (jobInstance.EndTimeUtc ?? DateTime.UtcNow) : (DateTime?)null;

        return new BackgroundJobResult
        {
            IsComplete = isComplete,
            IsSuccess = isSuccess,
            Status = jobInstance.Status ?? "Unknown",
            ErrorMessage = jobInstance.FailureReason?.Message,
            StartedAt = _startedAt,
            CompletedAt = completedAt,
            DurationFormatted = completedAt.HasValue ? FormatDuration(completedAt.Value - _startedAt) : null,
            Context = new DataflowRefreshContext
            {
                WorkspaceId = _workspaceId,
                DataflowId = _dataflowId,
                JobInstanceId = _jobInstanceId,
                DisplayName = DisplayName,
                StartedAtUtc = _startedAt
            }
        };
    }

    private static string ExtractJobInstanceId(string? location)
    {
        if (string.IsNullOrEmpty(location))
            throw new InvalidOperationException("No Location header in response");

        var segments = location.Split('/');
        var instancesIndex = Array.IndexOf(segments, "instances");
        if (instancesIndex >= 0 && instancesIndex < segments.Length - 1)
        {
            return segments[instancesIndex + 1];
        }

        throw new InvalidOperationException($"Could not parse job instance ID from Location: {location}");
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalSeconds < 60)
            return duration.Seconds == 1 ? "1 second" : $"{duration.Seconds} seconds";

        if (duration.TotalMinutes < 60)
        {
            var mins = (int)duration.TotalMinutes;
            var secs = duration.Seconds;
            var minPart = mins == 1 ? "1 minute" : $"{mins} minutes";
            if (secs == 0) return minPart;
            var secPart = secs == 1 ? "1 second" : $"{secs} seconds";
            return $"{minPart} {secPart}";
        }

        var hours = (int)duration.TotalHours;
        var minutes = duration.Minutes;
        var hourPart = hours == 1 ? "1 hour" : $"{hours} hours";
        if (minutes == 0) return hourPart;
        var minutePart = minutes == 1 ? "1 minute" : $"{minutes} minutes";
        return $"{hourPart} {minutePart}";
    }
}
