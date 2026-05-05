using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace DataFactory.MCP.Resources.Skills;

/// <summary>
/// MCP Resource handler for SEP-2640 skill:// URI scheme.
/// Exposes Data Factory skills as MCP Resources for progressive discovery:
///   - skill://index.json → list all skills with name + description
///   - skill://{skillName}/SKILL.md → full skill instructions
///   - skill://{skillName}/references/{file}.md → supplementary reference
///   - skill://_common/{file}.md → shared reference documents
/// </summary>
[McpServerResourceType]
public class SkillResourceHandler
{
    /// <summary>
    /// Discovery index — returns JSON array of {name, description} for all registered skills.
    /// Clients read this first to decide which skill to load (~100 tokens per entry).
    /// </summary>
    [McpServerResource(
        UriTemplate = "skill://index.json",
        Name = "Skill Index",
        MimeType = "application/json")]
    [Description("List all available Data Factory skills with name and description for routing decisions")]
    public static ReadResourceResult GetIndex()
    {
        var index = SkillRegistry.GetIndex();
        return new ReadResourceResult
        {
            Contents = [new TextResourceContents
            {
                Uri = "skill://index.json",
                Text = index,
                MimeType = "application/json"
            }]
        };
    }

    /// <summary>
    /// Individual skill file access — returns a specific markdown file from a skill.
    /// Use for loading full skill instructions or supplementary references on demand.
    /// </summary>
    [McpServerResource(
        UriTemplate = "skill://{skillName}/{filePath}",
        Name = "Skill Content",
        MimeType = "text/markdown")]
    [Description("Read a specific skill file by skill name and relative file path")]
    public static ReadResourceResult GetSkillFile(string skillName, string filePath)
    {
        var content = SkillRegistry.GetFile(skillName, filePath);
        return new ReadResourceResult
        {
            Contents = [new TextResourceContents
            {
                Uri = $"skill://{skillName}/{filePath}",
                Text = content,
                MimeType = "text/markdown"
            }]
        };
    }
}
