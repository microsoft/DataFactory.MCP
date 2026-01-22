using ModelContextProtocol.Server;
using System.ComponentModel;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Extensions;
using DataFactory.MCP.Validation;
using DataFactory.MCP.Parsing;

namespace DataFactory.MCP.Tools;

/// <summary>
/// MCP Tool for validating and saving M (Power Query) section documents for Fabric Dataflows.
/// Uses dedicated services for validation and parsing.
/// </summary>
[McpServerToolType]
public class MDocumentTool
{
    private readonly IFabricDataflowService _dataflowService;
    private readonly IValidationService _validationService;
    private readonly Validation.MDocumentValidator _validator;
    private readonly Parsing.MDocumentParser _parser;

    public MDocumentTool(IFabricDataflowService dataflowService, IValidationService validationService)
    {
        _dataflowService = dataflowService ?? throw new ArgumentNullException(nameof(dataflowService));
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _validator = new Validation.MDocumentValidator();
        _parser = new Parsing.MDocumentParser();
    }

    [McpServerTool, Description(@"Validates and saves an M section document to a dataflow.

This tool:
1. Validates the M document syntax and structure
2. Extracts individual queries from the document
3. Saves each query to the specified dataflow

The M document should be a complete section document with all queries needed for the data flow.
If validation fails, it returns detailed error information to help fix the document.")]
    public async Task<string> ValidateAndSaveMDocumentAsync(
        [Description("The workspace ID containing the target dataflow (required)")] string workspaceId,
        [Description("The dataflow ID to save the document to (required)")] string dataflowId,
        [Description("The complete M section document to validate and save (required). Should start with 'section Section1;' and contain all shared queries.")] string mDocument,
        [Description("If true, only validates without saving (optional, defaults to false)")] bool validateOnly = false)
    {
        try
        {
            _validationService.ValidateRequiredString(workspaceId, nameof(workspaceId));
            _validationService.ValidateRequiredString(dataflowId, nameof(dataflowId));
            _validationService.ValidateRequiredString(mDocument, nameof(mDocument));

            // Step 1: Validate the document structure using the validator service
            var validationResult = _validator.Validate(mDocument);
            if (!validationResult.IsValid)
            {
                return new
                {
                    Success = false,
                    Stage = "Validation",
                    Errors = validationResult.Errors,
                    Warnings = validationResult.Warnings,
                    Suggestions = validationResult.Suggestions,
                    Document = mDocument
                }.ToMcpJson();
            }

            // Step 2: Parse queries from the document using the parser service
            var queries = _parser.ParseQueries(mDocument);
            if (queries.Count == 0)
            {
                return new
                {
                    Success = false,
                    Stage = "Parsing",
                    Errors = new[] { "No valid queries found in the document" },
                    Suggestions = new[] { "Ensure queries are declared with 'shared QueryName = ...' syntax" }
                }.ToMcpJson();
            }

            // If validate only, return success with parsed info
            if (validateOnly)
            {
                return new
                {
                    Success = true,
                    Stage = "ValidationComplete",
                    Message = "Document is valid and ready to save",
                    DetectedPattern = validationResult.IsGen2 ? "Gen2 FastCopy" : "Gen1 Pipeline",
                    ParsedQueries = queries.Select(q => new { q.Name, CodeLength = q.Code.Length, HasAttribute = !string.IsNullOrEmpty(q.Attribute) }),
                    QueryCount = queries.Count,
                    Warnings = validationResult.Warnings,
                    Suggestions = validationResult.Suggestions
                }.ToMcpJson();
            }

            // Step 3: Sync the entire M document to the dataflow
            // This replaces the entire mashup.pq and syncs queryMetadata.json to match.
            // This is a declarative approach: the provided document IS the desired state.
            var result = await _dataflowService.SyncMashupDocumentAsync(
                workspaceId,
                dataflowId,
                mDocument,
                queries.Select(q => (q.Name, q.Code, (string?)q.Attribute)).ToList());

            if (!result.Success)
            {
                return new
                {
                    Success = false,
                    Stage = "Save",
                    WorkspaceId = workspaceId,
                    DataflowId = dataflowId,
                    DetectedPattern = validationResult.IsGen2 ? "Gen2 FastCopy" : "Gen1 Pipeline",
                    TotalQueries = queries.Count,
                    ErrorMessage = result.ErrorMessage,
                    Message = "Failed to save queries to dataflow"
                }.ToMcpJson();
            }

            return new
            {
                Success = true,
                Stage = "SaveComplete",
                WorkspaceId = workspaceId,
                DataflowId = dataflowId,
                DetectedPattern = validationResult.IsGen2 ? "Gen2 FastCopy" : "Gen1 Pipeline",
                TotalQueries = queries.Count,
                SavedQueries = queries.Count,
                Message = $"Successfully saved all {queries.Count} queries to dataflow"
            }.ToMcpJson();
        }
        catch (ArgumentException ex)
        {
            return ex.ToValidationError().ToMcpJson();
        }
        catch (UnauthorizedAccessException ex)
        {
            return ex.ToAuthenticationError().ToMcpJson();
        }
        catch (HttpRequestException ex)
        {
            return ex.ToHttpError().ToMcpJson();
        }
        catch (Exception ex)
        {
            return ex.ToOperationError("validating and saving M document").ToMcpJson();
        }
    }
}
