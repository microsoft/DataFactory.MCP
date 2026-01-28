using System.Collections.Concurrent;
using DataFactory.MCP.Abstractions.Interfaces;

namespace DataFactory.MCP.Services.BackgroundTasks;

/// <summary>
/// In-memory tracker for background tasks.
/// Thread-safe using ConcurrentDictionary.
/// </summary>
public class BackgroundTaskTracker : IBackgroundTaskTracker
{
    private readonly ConcurrentDictionary<string, TrackedTask> _tasks = new();

    public void Track(TrackedTask task)
    {
        _tasks[task.TaskId] = task;
    }

    public void Update(string taskId, Action<TrackedTask> updateAction)
    {
        if (_tasks.TryGetValue(taskId, out var task))
        {
            updateAction(task);
        }
    }

    public TrackedTask? GetTask(string taskId)
    {
        return _tasks.TryGetValue(taskId, out var task) ? task : null;
    }

    public IReadOnlyList<TrackedTask> GetAllTasks()
    {
        return _tasks.Values.ToList().AsReadOnly();
    }
}
