using DataFactory.MCP.Models.CopyJob;
using DataFactory.MCP.Models.CopyJob.Definition;
using DataFactory.MCP.Models.Pipeline;
using DataFactory.MCP.Models.Pipeline.Schedule;

namespace DataFactory.MCP.Abstractions.Interfaces;

/// <summary>
/// Service for interacting with Microsoft Fabric Copy Jobs API
/// </summary>
public interface IFabricCopyJobService
{
    /// <summary>
    /// Lists all copy jobs from the specified workspace
    /// </summary>
    Task<ListCopyJobsResponse> ListCopyJobsAsync(
        string workspaceId,
        string? continuationToken = null);

    /// <summary>
    /// Creates a new copy job in the specified workspace
    /// </summary>
    Task<CreateCopyJobResponse> CreateCopyJobAsync(
        string workspaceId,
        CreateCopyJobRequest request);

    /// <summary>
    /// Gets copy job metadata by ID
    /// </summary>
    Task<CopyJob> GetCopyJobAsync(
        string workspaceId,
        string copyJobId);

    /// <summary>
    /// Gets the definition of a copy job
    /// </summary>
    Task<CopyJobDefinition> GetCopyJobDefinitionAsync(
        string workspaceId,
        string copyJobId);

    /// <summary>
    /// Updates copy job metadata (displayName, description)
    /// </summary>
    Task<CopyJob> UpdateCopyJobAsync(
        string workspaceId,
        string copyJobId,
        UpdateCopyJobRequest request);

    /// <summary>
    /// Updates a copy job definition
    /// </summary>
    Task UpdateCopyJobDefinitionAsync(
        string workspaceId,
        string copyJobId,
        CopyJobDefinition definition);

    /// <summary>
    /// Runs a copy job on demand. Returns the Location header URL for tracking the job instance.
    /// </summary>
    Task<string?> RunCopyJobAsync(
        string workspaceId,
        string copyJobId,
        object? executionData = null);

    /// <summary>
    /// Gets the status of a copy job instance
    /// </summary>
    Task<ItemJobInstance> GetCopyJobInstanceAsync(
        string workspaceId,
        string copyJobId,
        string jobInstanceId);

    /// <summary>
    /// Creates a new schedule for a copy job
    /// </summary>
    Task<ItemSchedule> CreateCopyJobScheduleAsync(
        string workspaceId,
        string copyJobId,
        CreateScheduleRequest request);

    /// <summary>
    /// Lists all schedules for a copy job
    /// </summary>
    Task<ListSchedulesResponse> ListCopyJobSchedulesAsync(
        string workspaceId,
        string copyJobId,
        string? continuationToken = null);
}
