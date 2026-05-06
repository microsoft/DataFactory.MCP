using System.Text.RegularExpressions;

namespace DataFactory.WindowsMCP.Services;

/// <summary>
/// Validates M (Power Query) queries to prevent execution of dangerous functions
/// that could access external resources or execute arbitrary code.
/// </summary>
public static class MQueryValidator
{
    /// <summary>
    /// Set of M function names that are blocked due to security risks.
    /// These functions can access external data sources, execute native queries,
    /// or perform other potentially dangerous operations.
    /// </summary>
    private static readonly string[] BlockedFunctions =
    [
        // Web and network access
        "Web.Contents",

        // File system access
        "File.Contents",
        "Folder.Contents",

        // Database connectors
        "OleDb.DataSource",
        "OleDb.Query",
        "Odbc.DataSource",
        "Odbc.Query",
        "Sql.Database",
        "Sql.Databases",
        "Oracle.Database",
        "MySQL.Database",
        "PostgreSQL.Database",
        "Access.Database",

        // Cloud storage
        "AzureStorage.Blobs",
        "AzureStorage.Tables",

        // Hadoop
        "Hdfs.Contents",
        "Hdfs.Files",

        // SharePoint
        "SharePoint.Contents",
        "SharePoint.Files",
        "SharePoint.Tables",

        // Directory and collaboration services
        "ActiveDirectory.Domains",
        "Exchange.Contents",
        "Facebook.Graph",
        "GoogleAnalytics.Accounts",
        "Salesforce.Data",

        // Code execution and native queries
        "Expression.Evaluate",
        "Value.NativeQuery",

        // Dynamic dispatch — can bypass blocklist via reflection
        "#shared",

        // Script execution
        "R.Execute",
        "Python.Execute",

        // Additional data source connectors
        "Web.BrowserContents",
        "Kusto.Contents",
        "AnalysisServices.Database",
        "AnalysisServices.Databases",
        "AdoDotNet.DataSource",
        "AdoDotNet.Query",

        // Binary operations that could be abused
        "Binary.Buffer"
    ];

    /// <summary>
    /// Pattern that matches any blocked function name (case-insensitive).
    /// Uses word boundary matching to avoid false positives.
    /// </summary>
    private static readonly Regex BlockedFunctionPattern = new(
        string.Join("|", BlockedFunctions.Select(f => @"\b" + Regex.Escape(f) + @"\b")),
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Validates an M query to ensure it does not contain dangerous functions.
    /// This check intentionally matches inside string literals and comments for defense-in-depth,
    /// as patterns like <c>Expression.Evaluate("Web.Contents(...)")</c> represent real attack vectors.
    /// </summary>
    /// <param name="mashupQuery">The M query to validate.</param>
    /// <exception cref="ArgumentException">Thrown when a blocked function is detected in the query.</exception>
    public static void Validate(string mashupQuery)
    {
        var matches = BlockedFunctionPattern.Matches(mashupQuery);

        if (matches.Count > 0)
        {
            var blockedNames = string.Join(", ", matches.Select(m => $"'{m.Value}'").Distinct());
            throw new ArgumentException(
                $"The M query contains blocked functions: {blockedNames}. " +
                $"Data source functions and code execution functions are not allowed for security reasons.",
                nameof(mashupQuery));
        }
    }
}
