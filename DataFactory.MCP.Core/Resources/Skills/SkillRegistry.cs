using System.Text.Json;

namespace DataFactory.MCP.Resources.Skills;

/// <summary>
/// Registry of skill files loaded at startup. Provides the skill index
/// and individual file content for the SEP-2640 skill:// resource handler.
/// Skills are loaded from the skills/ directory relative to the application root.
/// </summary>
public static class SkillRegistry
{
    private static readonly Dictionary<string, SkillEntry> _skills = new(StringComparer.OrdinalIgnoreCase);
    private static bool _initialized;

    /// <summary>
    /// A registered skill with its metadata and file contents.
    /// </summary>
    public record SkillEntry(
        string Name,
        string Description,
        Dictionary<string, string> Files // relative path → content
    );

    /// <summary>
    /// Initialize the registry by scanning the skills/ directory.
    /// Call once at startup. Safe to call multiple times (idempotent).
    /// </summary>
    public static void Initialize(string? basePath = null)
    {
        if (_initialized) return;

        var root = basePath ?? AppContext.BaseDirectory;
        var skillsDir = FindSkillsDirectory(root);
        if (skillsDir == null)
        {
            _initialized = true;
            return;
        }

        foreach (var skillDir in Directory.GetDirectories(skillsDir))
        {
            var skillMd = Path.Combine(skillDir, "SKILL.md");
            if (!File.Exists(skillMd)) continue;

            var skillName = Path.GetFileName(skillDir);
            var content = File.ReadAllText(skillMd);
            var description = ExtractDescription(content);

            var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["SKILL.md"] = content
            };

            // Load references/ subdirectory if present
            var refsDir = Path.Combine(skillDir, "references");
            if (Directory.Exists(refsDir))
            {
                foreach (var refFile in Directory.GetFiles(refsDir, "*.md"))
                {
                    var relPath = $"references/{Path.GetFileName(refFile)}";
                    files[relPath] = File.ReadAllText(refFile);
                }
            }

            _skills[skillName] = new SkillEntry(skillName, description, files);
        }

        // Also load common/ files as a pseudo-skill
        var commonDir = Path.Combine(Path.GetDirectoryName(skillsDir)!, "common");
        if (Directory.Exists(commonDir))
        {
            var commonFiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var commonFile in Directory.GetFiles(commonDir, "*.md"))
            {
                commonFiles[Path.GetFileName(commonFile)] = File.ReadAllText(commonFile);
            }
            if (commonFiles.Count > 0)
            {
                _skills["_common"] = new SkillEntry(
                    "_common",
                    "Shared reference documents for Data Factory MCP skills (authentication, connections, patterns).",
                    commonFiles);
            }
        }

        _initialized = true;
    }

    /// <summary>
    /// Returns the skill index as a JSON array of {name, description} objects.
    /// </summary>
    public static string GetIndex()
    {
        var index = _skills.Values
            .Where(s => !s.Name.StartsWith("_")) // exclude pseudo-skills from index
            .Select(s => new { name = s.Name, description = s.Description })
            .OrderBy(s => s.name);
        return JsonSerializer.Serialize(index, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Returns the content of a specific file within a skill.
    /// </summary>
    public static string GetFile(string skillName, string filePath)
    {
        if (!_skills.TryGetValue(skillName, out var skill))
            throw new ArgumentException($"Unknown skill: {skillName}");
        if (!skill.Files.TryGetValue(filePath, out var content))
            throw new ArgumentException($"File not found in skill '{skillName}': {filePath}");
        return content;
    }

    /// <summary>
    /// Returns all registered skill names.
    /// </summary>
    public static IEnumerable<string> GetSkillNames() => _skills.Keys.Where(k => !k.StartsWith("_"));

    /// <summary>
    /// Returns true if a skill with the given name exists.
    /// </summary>
    public static bool HasSkill(string skillName) => _skills.ContainsKey(skillName);

    /// <summary>
    /// Lists all files available in a skill.
    /// </summary>
    public static IEnumerable<string> GetSkillFiles(string skillName)
    {
        if (!_skills.TryGetValue(skillName, out var skill))
            throw new ArgumentException($"Unknown skill: {skillName}");
        return skill.Files.Keys;
    }

    /// <summary>
    /// Extract the description field from YAML frontmatter.
    /// </summary>
    private static string ExtractDescription(string content)
    {
        if (!content.StartsWith("---")) return string.Empty;

        var endIndex = content.IndexOf("---", 3);
        if (endIndex < 0) return string.Empty;

        var frontmatter = content[3..endIndex];
        var descStart = frontmatter.IndexOf("description:", StringComparison.OrdinalIgnoreCase);
        if (descStart < 0) return string.Empty;

        // Handle multi-line YAML description (> folded scalar)
        var afterKey = frontmatter[(descStart + "description:".Length)..];
        var lines = afterKey.Split('\n');

        var descLines = new List<string>();
        var started = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (!started)
            {
                if (trimmed == ">" || trimmed == "|") { started = true; continue; }
                if (!string.IsNullOrEmpty(trimmed)) { descLines.Add(trimmed); break; } // single-line
            }
            else
            {
                if (string.IsNullOrEmpty(trimmed) || (!line.StartsWith("  ") && !line.StartsWith("\t")))
                    break; // end of indented block
                descLines.Add(trimmed);
            }
        }

        return string.Join(" ", descLines);
    }

    /// <summary>
    /// Find the skills/ directory by walking up from the base path.
    /// </summary>
    private static string? FindSkillsDirectory(string basePath)
    {
        var current = basePath;
        for (var i = 0; i < 5; i++) // walk up max 5 levels
        {
            var candidate = Path.Combine(current, "skills");
            if (Directory.Exists(candidate) &&
                Directory.GetFiles(candidate, "SKILL.md", SearchOption.AllDirectories).Length > 0)
                return candidate;

            var parent = Directory.GetParent(current);
            if (parent == null) break;
            current = parent.FullName;
        }
        return null;
    }
}
