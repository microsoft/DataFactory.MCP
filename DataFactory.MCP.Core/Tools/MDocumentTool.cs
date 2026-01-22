using ModelContextProtocol.Server;
using System.ComponentModel;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Extensions;
using DataFactory.MCP.Services;
using DataFactory.MCP.Validation;
using DataFactory.MCP.Parsing;

namespace DataFactory.MCP.Tools;

/// <summary>
/// MCP Tool for authoring M (Power Query) section documents for Fabric Dataflows.
/// Provides guidance and saving capabilities for the dataflow authoring workflow.
/// Uses external templates and dedicated services for validation/parsing.
/// </summary>
[McpServerToolType]
public class MDocumentTool
{
    private readonly IFabricDataflowService _dataflowService;
    private readonly IValidationService _validationService;
    private readonly MTemplateProvider _templateProvider;
    private readonly Validation.MDocumentValidator _validator;
    private readonly Parsing.MDocumentParser _parser;

    public MDocumentTool(IFabricDataflowService dataflowService, IValidationService validationService)
    {
        _dataflowService = dataflowService ?? throw new ArgumentNullException(nameof(dataflowService));
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _templateProvider = new MTemplateProvider();
        _validator = new Validation.MDocumentValidator();
        _parser = new Parsing.MDocumentParser();
    }

    #region TOOL 1: Authoring Guidance

    [McpServerTool, Description(@"**PRIMARY AUTHORING TOOL** - Use this FIRST when a user wants to create a dataflow. 
Analyzes the user's requirements and provides detailed guidance on how to author the M document.

This tool helps you understand:
- What queries to create based on user's data flow requirements
- The structure and pattern to follow (Gen1 vs Gen2)
- Column mappings and transformations needed
- Connection requirements

After receiving this guidance, you should author the M document step by step, then use 'validate_and_save_m_document' to save it.

Example user requests this tool handles:
- 'Load CSV data from SharePoint to Lakehouse'
- 'Create an ETL pipeline from SQL Server to Fabric'
- 'Transform and aggregate sales data into a Lakehouse table'")]
    public string GetAuthoringGuidance(
        [Description("Natural language description of what the user wants to achieve with the dataflow (e.g., 'Load sales data from SQL Server, filter active records, and save to Lakehouse')")] string userRequirement,
        [Description("The data source type: 'sql', 'lakehouse', 'web', 'csv', 'sharepoint', 'odata', 'warehouse'")] string? sourceType = null,
        [Description("The destination type: 'lakehouse' or 'warehouse' (optional, defaults to 'lakehouse')")] string? destinationType = "lakehouse",
        [Description("Known column names from the source (comma-separated, optional)")] string? sourceColumns = null,
        [Description("Target workspace ID if known (optional)")] string? targetWorkspaceId = null,
        [Description("Target Lakehouse/Warehouse ID if known (optional)")] string? targetLakehouseId = null,
        [Description("Dataflow version: 'gen2' (recommended, FastCopy pattern) or 'gen1' (legacy Pipeline.ExecuteAction pattern). Defaults to 'gen2'.")] string? dataflowVersion = "gen2")
    {
        var detectedSource = _templateProvider.DetectSourceType(userRequirement, sourceType);
        var columns = string.IsNullOrWhiteSpace(sourceColumns)
            ? Array.Empty<string>()
            : sourceColumns.Split(',').Select(c => c.Trim()).ToArray();

        var isGen2 = string.Equals(dataflowVersion, "gen2", StringComparison.OrdinalIgnoreCase) ||
                     string.IsNullOrWhiteSpace(dataflowVersion);
        var tableName = _parser.ExtractTableName(userRequirement) ?? "TargetTable";

        var sourceTemplate = _templateProvider.GetDataSourceTemplate(detectedSource);

        var guidance = new
        {
            Title = "Dataflow Authoring Guidance",
            UserRequirement = userRequirement,
            DataflowVersion = isGen2 ? "Gen2 (FastCopy)" : "Gen1 (Pipeline.ExecuteAction)",

            AnalyzedIntent = new
            {
                DetectedSourceType = detectedSource,
                DetectedDestinationType = destinationType,
                RequiresTransformation = _templateProvider.ContainsTransformationKeywords(userRequirement),
                RequiresFiltering = _templateProvider.ContainsFilteringKeywords(userRequirement),
                RequiresAggregation = _templateProvider.ContainsAggregationKeywords(userRequirement),
                DetectedColumns = columns.Length > 0 ? columns : new[] { "Analyze source to determine columns" }
            },

            RecommendedQueryStructure = GetRecommendedStructure(userRequirement, detectedSource, destinationType, isGen2, tableName),

            AuthoringSteps = isGen2
                ? _templateProvider.GetGen2Steps()
                : _templateProvider.GetGen1Steps(),

            SourceTemplate = sourceTemplate != null
                ? new { Type = detectedSource, Code = sourceTemplate.Template, RequiredParams = sourceTemplate.RequiredParameters }
                : new { Type = detectedSource, Code = "Custom source - define manually", RequiredParams = Array.Empty<string>() },

            DestinationInfo = new
            {
                WorkspaceId = targetWorkspaceId ?? "{workspaceId} - Get from user or list_workspaces",
                LakehouseId = targetLakehouseId ?? "{lakehouseId} - Get from user or workspace contents",
                TableName = tableName
            },

            MDocumentStructure = isGen2
                ? GetGen2MDocumentStructure(tableName)
                : _templateProvider.GetGen1DocumentTemplate(),

            DataTypes = _templateProvider.GetDataTypes(),

            NamingRules = GetNamingRules(isGen2),

            NextSteps = _templateProvider.GetNextSteps(),

            Tips = isGen2
                ? _templateProvider.GetGen2Tips()
                : _templateProvider.GetGen1Tips()
        };

        return guidance.ToMcpJson();
    }

    private object GetRecommendedStructure(string requirement, string sourceType, string? destType, bool isGen2, string tableName)
    {
        var hasTransform = _templateProvider.ContainsTransformationKeywords(requirement);
        var hasFilter = _templateProvider.ContainsFilteringKeywords(requirement);
        var hasAggregation = _templateProvider.ContainsAggregationKeywords(requirement);
        var needsWrite = destType != null;

        if (isGen2 && needsWrite)
        {
            // Gen2 FastCopy pattern - only 2 queries needed
            var queries = new List<object>
            {
                new { Order = 1, Name = tableName, Purpose = $"Load from {sourceType} with [DataDestinations] attribute", Required = true },
                new { Order = 2, Name = $"{tableName}_DataDestination", Purpose = $"Navigate to {destType} target table", Required = true }
            };

            return new
            {
                RecommendedQueries = queries,
                TotalQueries = queries.Count,
                Pattern = _templateProvider.GetGen2Pattern(),
                SectionAttribute = _templateProvider.GetGen2SectionAttribute()
            };
        }

        // Gen1 or read-only pattern
        var gen1Queries = new List<object>
        {
            new { Order = 1, Name = "SourceData", Purpose = $"Load from {sourceType}", Required = true }
        };

        if (hasFilter || hasTransform || hasAggregation)
        {
            gen1Queries.Add(new { Order = 2, Name = "TransformedData", Purpose = "Apply filters/transformations", Required = false });
        }

        if (needsWrite)
        {
            gen1Queries.Add(new { Order = 3, Name = "DefaultModelStorage", Purpose = "Staging for write operations", Required = true });
            gen1Queries.Add(new { Order = 4, Name = "DataDestination", Purpose = $"Define {destType} target table", Required = true });
            gen1Queries.Add(new { Order = 5, Name = "TransformForWrite", Purpose = "Map columns for destination", Required = true });
            gen1Queries.Add(new { Order = 6, Name = "WriteToDestination", Purpose = "Execute data load", Required = true });
        }

        return new
        {
            RecommendedQueries = gen1Queries,
            TotalQueries = gen1Queries.Count,
            Pattern = needsWrite ? _templateProvider.GetGen1Pattern() : "Read-Only Query"
        };
    }

    private string GetGen2MDocumentStructure(string tableName)
    {
        var template = _templateProvider.GetGen2DocumentTemplate();
        return template
            .Replace("{tableName}", tableName)
            .Replace("{workspaceId}", "{workspaceId}")
            .Replace("{lakehouseId}", "{lakehouseId}");
    }

    private object GetNamingRules(bool isGen2)
    {
        var rules = _templateProvider.GetNamingRules();
        return new
        {
            rules.SimpleNames,
            rules.SpecialCharacters,
            rules.ColumnNamesWithSpaces,
            DataDestinationNaming = isGen2
                ? _templateProvider.GetGen2NamingConvention()
                : _templateProvider.GetGen1NamingConvention()
        };
    }

    #endregion

    #region TOOL 2: Validate and Save

    [McpServerTool, Description(@"**SAVE TOOL** - Use this AFTER authoring the M document to validate and save it to a dataflow.

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

    #endregion
}
