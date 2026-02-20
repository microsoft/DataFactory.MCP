using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace DataFactory.MCP.Infrastructure.McpApps;

/// <summary>
/// Utility for loading and bundling MCP Apps UI resources.
/// Combines separate HTML, CSS, and JS files into a single inline HTML document.
/// </summary>
public static partial class McpAppResourceLoader
{
    private static readonly Assembly ResourceAssembly = typeof(McpAppResourceLoader).Assembly;
    private const string ResourceBasePath = "DataFactory.MCP.Core.Resources.McpApps";

    /// <summary>
    /// Loads a pre-bundled MCP App UI resource from the monorepo dist folder.
    /// The monorepo builds all apps into McpApps/dist/{appName}.html
    /// </summary>
    /// <param name="appName">The app name (e.g., "user-input")</param>
    /// <returns>Complete HTML with all dependencies bundled</returns>
    public static string LoadFromMonorepo(string appName)
    {
        // Load from monorepo dist folder: McpApps/dist/{appName}.html
        var resourceName = $"{ResourceBasePath}.dist.{appName}.html";
        using var stream = ResourceAssembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"MCP App resource not found: {resourceName}. Run 'npm run build' in the McpApps folder.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Loads a pre-bundled MCP App UI resource (built with Vite).
    /// For legacy per-app folder structure.
    /// </summary>
    /// <param name="resourceFolder">The resource folder name (e.g., "AddConnection")</param>
    /// <param name="baseName">The base file name without extension (e.g., "add-connection")</param>
    /// <returns>Complete HTML with all dependencies bundled</returns>
    public static string LoadPreBundled(string resourceFolder, string baseName)
    {
        // Load from dist subfolder (Vite output)
        var resourceName = $"{ResourceBasePath}.{resourceFolder}.dist.{baseName}.html";
        using var stream = ResourceAssembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Pre-bundled resource not found: {resourceName}. Run 'npm run build' in the {resourceFolder} folder.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Loads an MCP App UI resource by name, inlining CSS and JS.
    /// </summary>
    /// <param name="resourceFolder">The resource folder name (e.g., "AddConnection")</param>
    /// <param name="baseName">The base file name without extension (e.g., "add-connection")</param>
    /// <returns>Complete HTML with inlined CSS and JS</returns>
    public static string LoadAndBundle(string resourceFolder, string baseName)
    {
        var html = LoadResource(resourceFolder, $"{baseName}.html");
        var css = LoadResourceOrDefault(resourceFolder, $"{baseName}.css");
        var js = LoadResourceOrDefault(resourceFolder, $"{baseName}.js");

        return BundleHtml(html, css, js);
    }

    /// <summary>
    /// Loads a resource file content.
    /// </summary>
    private static string LoadResource(string folder, string fileName)
    {
        var resourceName = $"{ResourceBasePath}.{folder}.{fileName}";
        using var stream = ResourceAssembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource not found: {resourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Loads a resource file content, returning empty string if not found.
    /// </summary>
    private static string LoadResourceOrDefault(string folder, string fileName)
    {
        var resourceName = $"{ResourceBasePath}.{folder}.{fileName}";
        using var stream = ResourceAssembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            return string.Empty;
        }
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Bundles HTML with inline CSS and JS.
    /// Replaces link[rel=stylesheet] and script[src] tags with inline content.
    /// </summary>
    private static string BundleHtml(string html, string css, string js)
    {
        var result = new StringBuilder(html);

        // Replace CSS link with inline style
        if (!string.IsNullOrEmpty(css))
        {
            result = new StringBuilder(CssLinkRegex().Replace(
                result.ToString(),
                $"<style>\n{css}\n</style>"));
        }

        // Replace JS script src with inline script
        if (!string.IsNullOrEmpty(js))
        {
            result = new StringBuilder(JsScriptRegex().Replace(
                result.ToString(),
                $"<script>\n{js}\n</script>"));
        }

        return result.ToString();
    }

    [GeneratedRegex(@"<link\s+rel=[""']stylesheet[""']\s+href=[""'][^""']+[""']\s*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex CssLinkRegex();

    [GeneratedRegex(@"<script\s+src=[""'][^""']+[""']\s*>\s*</script>", RegexOptions.IgnoreCase)]
    private static partial Regex JsScriptRegex();
}
