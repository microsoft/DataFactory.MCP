using ModelContextProtocol.Server;
using System.ComponentModel;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Extensions;
using DataFactory.MCP.Models.Dataflow.Query;

namespace DataFactory.MCP.Tools;

/// <summary>
/// MCP Tool for executing queries against Microsoft Fabric Dataflows
/// </summary>
[McpServerToolType]
public class DataflowQueryTool
{
    private readonly IFabricDataflowService _dataflowService;
    private readonly IValidationService _validationService;

    public DataflowQueryTool(IFabricDataflowService dataflowService, IValidationService validationService)
    {
        _dataflowService = dataflowService ?? throw new ArgumentNullException(nameof(dataflowService));
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
    }

    [McpServerTool, Description(@"Executes a query against a dataflow and returns the complete results (all data) in Apache Arrow format. This allows you to run M (Power Query) language queries against data sources connected through the dataflow and get the full dataset.

FORMATTING INSTRUCTION: When displaying results to users, please format the 'table.rows' data as a markdown table using the column names from 'table.columns'. This provides immediate visual representation of the query results.")]
    public async Task<string> ExecuteQueryAsync(
        [Description("The workspace ID containing the dataflow (required)")] string workspaceId,
        [Description("The dataflow ID to execute the query against (required)")] string dataflowId,
        [Description("The name of the query to execute (required)")] string queryName,
        [Description("The M (Power Query) language query to execute. Can be either a raw M expression (which will be auto-wrapped) or a complete section document. Results will be returned as structured data - format the table.rows as a markdown table for user display.")] string customMashupDocument)
    {
        try
        {
            // Validate required parameters using validation service
            _validationService.ValidateRequiredString(workspaceId, nameof(workspaceId));
            _validationService.ValidateRequiredString(dataflowId, nameof(dataflowId));
            _validationService.ValidateRequiredString(queryName, nameof(queryName));
            _validationService.ValidateRequiredString(customMashupDocument, nameof(customMashupDocument));

            // Auto-wrap the query if it's not already in section format
            var wrappedQuery = customMashupDocument.WrapForDataflowQuery(queryName);

            // Execute the query
            var request = new ExecuteDataflowQueryRequest
            {
                QueryName = queryName,
                CustomMashupDocument = wrappedQuery
            };

            var response = await _dataflowService.ExecuteQueryAsync(workspaceId, dataflowId, request);

            // Return formatted response
            var result = response.Success
                ? response.CreateArrowDataReport()
                : response.ToQueryExecutionError(workspaceId, dataflowId, queryName);

            return result.ToMcpJson();
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
            return ex.ToOperationError("executing dataflow query").ToMcpJson();
        }
    }


}