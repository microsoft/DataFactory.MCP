using ModelContextProtocol;

namespace DataFactory.MCP.Abstractions.Interfaces;

/// <summary>
/// Executes and monitors background jobs.
/// Single Responsibility: orchestrates job execution and notifications.
/// </summary>
public interface IBackgroundJobRunner
{
    /// <summary>
    /// Starts a background job and monitors it until completion.
    /// Sends notifications when the job completes.
    /// </summary>
    /// <param name="job">The job to execute</param>
    /// <param name="session">MCP session for storing in accessor</param>
    /// <returns>The initial job result</returns>
    Task<BackgroundJobResult> RunAsync(IBackgroundJob job, McpSession session);
}
