using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Extensions;
using DataFactory.WindowsMCP.Abstractions.Interfaces;
using DataFactory.WindowsMCP.Extensions;
using DataFactory.WindowsMCP.Models.Dataflow.Excel;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace DataFactory.WindowsMCP.Tools;

/// <summary>
/// MCP Tool for creating Excel worbooks from data
/// </summary>
[McpServerToolType]
public class ExcelTool
{
    private readonly IExcelService _excelService;
    private readonly IValidationService _validationService;

    public ExcelTool(IExcelService excelService, IValidationService validationService)
    {
        _excelService = excelService ?? throw new ArgumentNullException(nameof(excelService));
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
    }

    [McpServerTool, Description(@"Adds a query table to an Excel workbook based on an M query. Optionally, if data results are provided, they will be included in the table.")]
    public async Task<string> AddQueryTable(
        [Description("The M (Power Query) language query to execute (required). Just the raw M should be provided without a section document.")] string mashupQuery,
        [Description("The data to populate the table in JSON format (optional).")] string data)
    {
        try
        {
            // Validate required parameters using validation service
            _validationService.ValidateRequiredString(mashupQuery, nameof(mashupQuery));

            // TODO: Verify the M query is valid

            // Add the query table to the workbook
            var request = new AddQueryTableRequest
            {
                MashupQuery = mashupQuery,
                JsonData = data
            };

            var response = await _excelService.AddQueryTableAsync(mashupQuery, data);

            // Return formatted response
            var result = new
            {
                Details = response?.Success == true ? "Successfully added query table" : "Failed to add query table"
            };

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
            return ex.ToOperationError("adding query table").ToMcpJson();
        }
    }
}