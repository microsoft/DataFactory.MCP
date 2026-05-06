using System.Text.RegularExpressions;

namespace Fabric.Mcp.Tools.DataFactory.Validation;

/// <summary>
/// Validates M (Power Query) section documents for syntax and structure.
/// </summary>
public class MDocumentValidator
{
    /// <summary>
    /// Validates an M document and returns the result.
    /// </summary>
    public MDocumentValidationResult Validate(string document)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        var suggestions = new List<string>();

        var trimmed = document.Trim();

        // Check for Gen2 FastCopy pattern
        var isGen2 = trimmed.Contains("[StagingDefinition") && trimmed.Contains("FastCopy");

        // Check section declaration (may have attribute before it in Gen2)
        if (!trimmed.Contains("section "))
        {
            errors.Add("Document must contain a section declaration (e.g., 'section Section1;')");
        }
        else if (!Regex.IsMatch(trimmed, @"section\s+\w+\s*;"))
        {
            errors.Add("Section declaration must end with semicolon (e.g., 'section Section1;')");
        }

        // Check for shared queries
        if (!Regex.IsMatch(document, @"\bshared\s+", RegexOptions.IgnoreCase))
        {
            errors.Add("Document must contain at least one 'shared' query declaration");
        }

        // Check for balanced brackets
        ValidateBracketBalance(document, '(', ')', "parentheses", errors);
        ValidateBracketBalance(document, '{', '}', "braces", errors);
        ValidateBracketBalance(document, '[', ']', "square brackets", errors);

        // Check for let...in structure
        var letCount = Regex.Matches(document, @"\blet\b", RegexOptions.IgnoreCase).Count;
        var inCount = Regex.Matches(document, @"\bin\b", RegexOptions.IgnoreCase).Count;
        if (letCount != inCount)
        {
            warnings.Add($"Mismatched let/in keywords: {letCount} 'let', {inCount} 'in'. This may be intentional for simple expressions.");
        }

        return new MDocumentValidationResult
        {
            IsValid = errors.Count == 0,
            IsGen2 = isGen2,
            Errors = errors.ToArray(),
            Warnings = warnings.ToArray(),
            Suggestions = suggestions.ToArray()
        };
    }

    private static void ValidateBracketBalance(string document, char open, char close, string name, List<string> errors)
    {
        var openCount = document.Count(c => c == open);
        var closeCount = document.Count(c => c == close);
        if (openCount != closeCount)
        {
            errors.Add($"Unbalanced {name}: {openCount} opening, {closeCount} closing");
        }
    }
}

/// <summary>
/// Result of M document validation.
/// </summary>
public class MDocumentValidationResult
{
    public bool IsValid { get; set; }
    public bool IsGen2 { get; set; }
    public string[] Errors { get; set; } = Array.Empty<string>();
    public string[] Warnings { get; set; } = Array.Empty<string>();
    public string[] Suggestions { get; set; } = Array.Empty<string>();
}
