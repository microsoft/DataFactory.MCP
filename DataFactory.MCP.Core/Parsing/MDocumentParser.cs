using System.Text.RegularExpressions;

namespace DataFactory.MCP.Parsing;

/// <summary>
/// Parses M (Power Query) section documents to extract queries.
/// </summary>
public class MDocumentParser
{
    /// <summary>
    /// Parses an M document and extracts all shared queries.
    /// </summary>
    public List<ParsedQuery> ParseQueries(string document)
    {
        var queries = new List<ParsedQuery>();

        // Pattern to match: shared QueryName = ... (we'll extract attributes separately)
        // This handles both simple names and #"quoted names"
        var pattern = @"\bshared\s+(#""[^""]+""|\w+)\s*=\s*";
        var matches = Regex.Matches(document, pattern, RegexOptions.IgnoreCase);

        for (int i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            
            // Extract attribute by looking backwards from 'shared' for balanced brackets
            var attribute = ExtractAttributeBeforePosition(document, match.Index);
            var queryName = match.Groups[1].Value;

            // Remove #"" wrapper if present for the name
            if (queryName.StartsWith("#\"") && queryName.EndsWith("\""))
            {
                queryName = queryName.Substring(2, queryName.Length - 3);
            }

            // Find the end of this query (next shared or end of document)
            var startIndex = match.Index + match.Length;
            int endIndex;

            if (i + 1 < matches.Count)
            {
                // Find the position just before the next attribute or 'shared'
                endIndex = matches[i + 1].Index;
                // Walk back to find the semicolon
                var searchArea = document.Substring(startIndex, endIndex - startIndex);
                var lastSemicolon = searchArea.LastIndexOf(';');
                if (lastSemicolon >= 0)
                {
                    endIndex = startIndex + lastSemicolon + 1;
                }
            }
            else
            {
                endIndex = document.Length;
            }

            var queryCode = document.Substring(startIndex, endIndex - startIndex).Trim();
            // Remove trailing semicolon for the code
            if (queryCode.EndsWith(";"))
            {
                queryCode = queryCode.Substring(0, queryCode.Length - 1).Trim();
            }

            queries.Add(new ParsedQuery
            {
                Name = queryName,
                Code = queryCode,
                Attribute = attribute
            });
        }

        return queries;
    }

    /// <summary>
    /// Extracts a balanced bracket attribute that appears before the given position.
    /// Handles nested brackets like [DataDestinations = {[Definition = [Kind = "Reference", ...]]}]
    /// </summary>
    private static string ExtractAttributeBeforePosition(string document, int sharedPosition)
    {
        // Walk backwards from 'shared' to find whitespace, then look for ']'
        var pos = sharedPosition - 1;
        
        // Skip whitespace before 'shared'
        while (pos >= 0 && char.IsWhiteSpace(document[pos]))
        {
            pos--;
        }
        
        // Check if we have a closing bracket
        if (pos < 0 || document[pos] != ']')
        {
            return "";
        }
        
        // Find the matching opening bracket using bracket counting
        var endPos = pos;
        var bracketCount = 0;
        var braceCount = 0;
        var inString = false;
        
        while (pos >= 0)
        {
            var c = document[pos];
            
            // Handle string literals (skip their content)
            if (c == '"' && (pos == 0 || document[pos - 1] != '\\'))
            {
                inString = !inString;
            }
            
            if (!inString)
            {
                if (c == ']') bracketCount++;
                else if (c == '[') bracketCount--;
                else if (c == '}') braceCount++;
                else if (c == '{') braceCount--;
                
                // Found the matching opening bracket
                if (bracketCount == 0 && braceCount == 0 && c == '[')
                {
                    return document.Substring(pos, endPos - pos + 1).Trim();
                }
            }
            
            pos--;
        }
        
        return "";
    }
}

/// <summary>
/// Represents a parsed query from an M document.
/// </summary>
public class ParsedQuery
{
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string Attribute { get; set; } = "";
}
