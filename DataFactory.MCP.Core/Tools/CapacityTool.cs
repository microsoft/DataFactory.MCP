using ModelContextProtocol.Server;
using System.ComponentModel;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Extensions;

namespace DataFactory.MCP.Tools;

[McpServerToolType]
public class CapacityTool
{
    private readonly IFabricCapacityService _capacityService;

    public CapacityTool(IFabricCapacityService capacityService)
    {
        _capacityService = capacityService;
    }

    [McpServerTool, Description(@"Lists all capacities the user has permission for (either administrator or contributor)")]
    public async Task<string> ListCapacitiesAsync(
        [Description("A token for retrieving the next page of results (optional)")] string? continuationToken = null)
    {
        try
        {
            var response = await _capacityService.ListCapacitiesAsync(continuationToken);

            if (!response.Value.Any())
            {
                return "No capacities found. You may not have access to any Microsoft Fabric capacities, or they may not be provisioned yet.";
            }

            var result = new
            {
                TotalCount = response.Value.Count,
                ContinuationToken = response.ContinuationToken,
                HasMoreResults = !string.IsNullOrEmpty(response.ContinuationToken),
                FormattedResults = response.Value.ToFormattedList()
            };

            return result.ToMcpJson();
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
            return ex.ToOperationError("listing capacities").ToMcpJson();
        }
    }
}