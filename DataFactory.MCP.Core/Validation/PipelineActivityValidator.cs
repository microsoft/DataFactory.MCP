using System.Text.Json.Nodes;

namespace DataFactory.MCP.Validation;

/// <summary>
/// Guards dependencies between top-level pipeline activities. This is not activity-schema
/// validation, cycle detection, recursive container validation, or comprehensive graph validation.
/// </summary>
public static class PipelineActivityValidator
{
    private static readonly HashSet<string> ValidDependencyConditions = new(StringComparer.OrdinalIgnoreCase)
    {
        "Succeeded", "Failed", "Skipped", "Completed"
    };

    /// <summary>
    /// Validates dependency rules that do not require the complete pipeline graph.
    /// </summary>
    public static void ValidateActivityDependencies(JsonObject activity, string activityName)
    {
        var dependsOn = GetOptionalArray(activity, "dependsOn", "Activity 'dependsOn'");
        if (dependsOn is null)
            return;

        foreach (var dependency in dependsOn)
        {
            var dependencyObject = GetOptionalObject(dependency, "Activity dependency");
            if (dependencyObject is null)
                continue;

            var dependencyActivity = GetOptionalString(
                dependencyObject, "activity", "Dependency 'activity'");
            if (string.Equals(dependencyActivity, activityName, StringComparison.Ordinal))
                throw new ArgumentException($"Activity '{activityName}' cannot depend on itself");

            ValidateConditions(dependencyObject, null);
        }
    }

    /// <summary>
    /// Validates references and conditions between top-level activities only.
    /// </summary>
    public static void ValidateActivityGraph(JsonArray activities)
    {
        var activityNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var activity in activities)
        {
            var activityObject = GetOptionalObject(activity, "Pipeline activity");
            if (activityObject is null)
                continue;

            var activityName = GetOptionalString(activityObject, "name", "Activity 'name'");
            if (activityName is not null)
                activityNames.Add(activityName);
        }

        foreach (var activity in activities)
        {
            var activityObject = GetOptionalObject(activity, "Pipeline activity");
            if (activityObject is null)
                continue;

            var activityName = GetOptionalString(activityObject, "name", "Activity 'name'") ?? "";
            var dependsOn = GetOptionalArray(activityObject, "dependsOn", "Activity 'dependsOn'");
            if (dependsOn is null)
                continue;

            foreach (var dependency in dependsOn)
            {
                var dependencyObject = GetOptionalObject(dependency, "Activity dependency");
                if (dependencyObject is null)
                    continue;

                var dependencyActivity = GetOptionalString(
                    dependencyObject, "activity", "Dependency 'activity'");
                if (string.Equals(dependencyActivity, activityName, StringComparison.Ordinal))
                    throw new ArgumentException($"Activity '{activityName}' cannot depend on itself");

                if (dependencyActivity is not null && !activityNames.Contains(dependencyActivity))
                {
                    throw new ArgumentException(
                        $"Activity '{activityName}' depends on '{dependencyActivity}' which does not exist in the pipeline");
                }

                ValidateConditions(dependencyObject, activityName);
            }
        }
    }

    /// <summary>
    /// Rejects removal when another top-level activity directly references the selected name.
    /// </summary>
    public static void ValidateRemoval(JsonArray activities, string activityName)
    {
        var dependents = new List<string>();
        foreach (var activity in activities)
        {
            var activityObject = GetOptionalObject(activity, "Pipeline activity");
            if (activityObject is null)
                continue;

            var name = GetOptionalString(activityObject, "name", "Activity 'name'");
            if (string.Equals(name, activityName, StringComparison.Ordinal))
                continue;

            var dependsOn = GetOptionalArray(activityObject, "dependsOn", "Activity 'dependsOn'");
            if (dependsOn is null)
                continue;

            foreach (var dependency in dependsOn)
            {
                var dependencyObject = GetOptionalObject(dependency, "Activity dependency");
                if (dependencyObject is null)
                    continue;

                var dependencyActivity = GetOptionalString(
                    dependencyObject, "activity", "Dependency 'activity'");
                if (!string.Equals(dependencyActivity, activityName, StringComparison.Ordinal))
                    continue;

                dependents.Add(name ?? "(unnamed)");
                break;
            }
        }

        if (dependents.Count > 0)
        {
            throw new ArgumentException(
                $"Cannot remove activity '{activityName}' because it is referenced by: {string.Join(", ", dependents)}");
        }
    }

    private static void ValidateConditions(JsonObject dependency, string? activityName)
    {
        var conditions = GetOptionalArray(
            dependency, "dependencyConditions", "Dependency 'dependencyConditions'");
        if (conditions is null)
            return;

        foreach (var condition in conditions)
        {
            if (condition is null)
                continue;

            if (condition is not JsonValue value || !value.TryGetValue<string>(out var conditionValue))
                throw new InvalidOperationException("Dependency condition must be a string or null");

            if (ValidDependencyConditions.Contains(conditionValue))
                continue;

            var activityContext = activityName is null ? "" : $" in activity '{activityName}'";
            throw new ArgumentException(
                $"Invalid dependency condition '{conditionValue}'{activityContext}. Valid values: Succeeded, Failed, Skipped, Completed");
        }
    }

    private static JsonArray? GetOptionalArray(JsonObject owner, string propertyName, string description)
    {
        var node = owner[propertyName];
        if (node is null)
            return null;

        return node as JsonArray
            ?? throw new InvalidOperationException($"{description} must be an array or null");
    }

    private static JsonObject? GetOptionalObject(JsonNode? node, string description)
    {
        if (node is null)
            return null;

        return node as JsonObject
            ?? throw new InvalidOperationException($"{description} must be an object or null");
    }

    private static string? GetOptionalString(
        JsonObject owner, string propertyName, string description)
    {
        var node = owner[propertyName];
        if (node is null)
            return null;

        return node is JsonValue value && value.TryGetValue<string>(out var result)
            ? result
            : throw new InvalidOperationException($"{description} must be a string or null");
    }
}
