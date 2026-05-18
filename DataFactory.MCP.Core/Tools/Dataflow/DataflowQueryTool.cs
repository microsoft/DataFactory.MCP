using ModelContextProtocol.Server;
using System.ComponentModel;
using DataFactory.MCP.Extensions;
using DataFactory.MCP.Handlers;
using DataFactory.MCP.Handlers.Dataflow;

namespace DataFactory.MCP.Tools.Dataflow;

/// <summary>
/// MCP Tool for executing queries against Microsoft Fabric Dataflows.
/// </summary>
[McpServerToolType]
public class DataflowQueryTool
{
    private readonly DataflowQueryHandler _handler;

    public DataflowQueryTool(DataflowQueryHandler handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    [McpServerTool, Description(@"Executes a query against a dataflow and returns the complete results (all data) in Apache Arrow format. This allows you to run M (Power Query) language queries against data sources connected through the dataflow and get the full dataset.

FORMATTING INSTRUCTION: When displaying results to users, please format the 'table.rows' data as a markdown table using the column names from 'table.columns'. This provides immediate visual representation of the query results.")]
    public async Task<string> ExecuteQueryAsync(
        [Description("The workspace ID containing the dataflow (required)")] string workspaceId,
        [Description("The dataflow ID to execute the query against (required)")] string dataflowId,
        [Description("The name of the query to execute (required)")] string queryName,
        [Description("The M (Power Query) language query to execute. Can be either a raw M expression (which will be auto-wrapped) or a complete section document. Results will be returned as structured data - format the table.rows as a markdown table for user display.")] string customMashupDocument)
    {
        var result = await _handler.ExecuteQueryAsync(workspaceId, dataflowId, queryName, customMashupDocument);

        if (result.IsSuccess)
            return result.Value!.Data!.ToMcpJson();

        return result.ToErrorResponse("executing dataflow query").ToMcpJson();
    }
}
