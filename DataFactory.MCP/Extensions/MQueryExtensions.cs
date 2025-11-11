namespace DataFactory.MCP.Extensions;

/// <summary>
/// Extension methods for M Query processing
/// </summary>
public static class MQueryExtensions
{
    /// <summary>
    /// Wraps a raw M query in the proper section format if it's not already wrapped
    /// </summary>
    /// <param name="query">The M query to wrap</param>
    /// <param name="queryName">The name of the query</param>
    /// <returns>The properly formatted section document</returns>
    public static string WrapForDataflowQuery(this string query, string queryName)
    {
        if (string.IsNullOrWhiteSpace(query))
            return query;

        // Check if the query already starts with "section" (case-insensitive)
        var trimmedQuery = query.Trim();
        if (trimmedQuery.StartsWith("section ", StringComparison.OrdinalIgnoreCase))
        {
            // Already in section format, return as-is
            return query;
        }

        // Auto-wrap the raw M query in section format
        return $@"section Section1;

shared {queryName} = {query.TrimEnd()};";
    }
}