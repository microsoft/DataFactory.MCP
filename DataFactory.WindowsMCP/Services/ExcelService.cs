using DataFactory.MCP.Abstractions;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.WindowsMCP.Abstractions;
using DataFactory.WindowsMCP.Abstractions.Interfaces;
using DataFactory.WindowsMCP.Models.Dataflow.Excel;
using Microsoft.Extensions.Logging;
using Microsoft.Office.Interop.Excel;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using Excel = Microsoft.Office.Interop.Excel;

namespace DataFactory.WindowsMCP.Services;

/// <summary>
/// Service for interacting with Microsoft Excel API
/// </summary>
public class ExcelService : FabricServiceBase, IExcelService
{
    private const string ArrowContentType = "application/octet-stream";

    public ExcelService(
        IHttpClientFactory httpClientFactory,
        ILogger<ExcelService> logger,
        IValidationService validationService)
        : base(httpClientFactory, logger, validationService)
    {
    }

    public async Task<AddQueryTableResponse> AddQueryTableAsync(
        string mashupQuery,
        string jsonData)
    {
        Excel.Application? excelApp = null;
        Excel.Workbook? workbook = null;
        Excel.Worksheet? worksheet = null;
        
        try
        {
            Logger.LogInformation("Adding query table to Excel workbook");

            // Run Excel automation on a separate thread to avoid COM threading issues
            await Task.Run(() =>
            {
                // Step 1: Find or create Excel application instance
                try
                {
                    excelApp = (Excel.Application)GetActiveObject("Excel.Application", true);
                    Logger.LogInformation("Found active Excel application");
                    if (excelApp == null)
                    {
                        throw new InvalidOperationException("Failed to get active Excel application instance");
                    }
                }
                catch (COMException)
                {
                    excelApp = new Excel.Application();
                    Logger.LogInformation("Created new Excel application instance");
                }

                excelApp.Visible = true;

                // Step 2: Get active workbook or create a new one
                if (excelApp.ActiveWorkbook != null)
                {
                    workbook = excelApp.ActiveWorkbook;
                    Logger.LogInformation("Using active workbook");
                }
                else
                {
                    workbook = excelApp.Workbooks.Add();
                    Logger.LogInformation("Created new workbook");
                }

                // Step 3: Create a new worksheet
                worksheet = (Excel.Worksheet)workbook.Worksheets.Add();
                worksheet.Name = $"QueryTable_{DateTime.Now:yyyyMMddHHmmss}";
                Logger.LogInformation($"Created new worksheet: {worksheet.Name}");

                // Step 4: Create the Power Query first
                var queryName = $"Query_{DateTime.Now:yyyyMMddHHmmss}";
                var queryConnection = workbook.Queries.Add(
                    Name: queryName,
                    Formula: mashupQuery);
                
                Logger.LogInformation($"Created Power Query: {queryName}");

                // Step 5: Create a ListObject with the query connection at cell B2
                var startCell = (Excel.Range)worksheet.Cells[2, 2]; // B2
                
                // Create QueryTable connection using the existing query
                var connectionString = $"OLEDB;Provider=Microsoft.Mashup.OleDb.1;Data Source=$Workbook$;Location={queryName};Extended Properties=\"\"";
                Excel.ListObject listObject = worksheet.ListObjects.AddEx(
                    SourceType: Excel.XlListObjectSourceType.xlSrcExternal,
                    Source: connectionString,
                    LinkSource: Missing.Value,
                    XlListObjectHasHeaders: Excel.XlYesNoGuess.xlYes,
                    Destination: startCell);

                Excel.QueryTable queryTable = listObject.QueryTable;
                queryTable.CommandType = Excel.XlCmdType.xlCmdSql;
                queryTable.CommandText = string.Format("SELECT * FROM [{0}]", queryName);
                queryTable.BackgroundQuery = true;
                queryTable.AdjustColumnWidth = true;
                
                Logger.LogInformation("Created ListObject with Power Query");

                // Step 5: Populate with JSON data if provided
                if (!string.IsNullOrWhiteSpace(jsonData))
                {
                    PopulateListObjectWithJsonData(worksheet, listObject, jsonData, startCell);
                    Logger.LogInformation("Populated ListObject with JSON data");
                }
                else
                {
                    // Refresh the query to populate data
                    queryTable.Refresh(BackgroundQuery: false);
                    Logger.LogInformation("Refreshed query table");
                }
            });

            Logger.LogInformation("Successfully added query table to Excel workbook");

            return new AddQueryTableResponse
            {
                Success = true,
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error adding query table to Excel workbook");
            return new AddQueryTableResponse
            {
                Success = false,
                Error = ex.Message,
            };
        }
        finally
        {
            // Clean up COM objects
            if (worksheet != null) Marshal.ReleaseComObject(worksheet);
            if (workbook != null) Marshal.ReleaseComObject(workbook);
            if (excelApp != null) Marshal.ReleaseComObject(excelApp);
        }
    }

    private void PopulateListObjectWithJsonData(
        Excel.Worksheet worksheet,
        Excel.ListObject listObject,
        string jsonData,
        Excel.Range startCell)
    {
        try
        {
            // Parse JSON data
            var jsonDocument = JsonDocument.Parse(jsonData);
            
            if (jsonDocument.RootElement.ValueKind == JsonValueKind.Array)
            {
                var rows = jsonDocument.RootElement.EnumerateArray().ToList();
                if (rows.Count == 0) return;

                // Get column names from the first object
                var firstRow = rows[0];
                var columns = firstRow.EnumerateObject().Select(p => p.Name).ToList();
                
                // Write headers
                for (int col = 0; col < columns.Count; col++)
                {
                    worksheet.Cells[startCell.Row, startCell.Column + col] = columns[col];
                }

                // Write data rows
                for (int row = 0; row < rows.Count; row++)
                {
                    var jsonRow = rows[row];
                    for (int col = 0; col < columns.Count; col++)
                    {
                        var columnName = columns[col];
                        if (jsonRow.TryGetProperty(columnName, out var value))
                        {
                            object? cellValue = value.ValueKind switch
                            {
                                JsonValueKind.String => value.GetString(),
                                JsonValueKind.Number => value.GetDouble(),
                                JsonValueKind.True => true,
                                JsonValueKind.False => false,
                                JsonValueKind.Null => null,
                                _ => value.ToString()
                            };
                            
                            worksheet.Cells[startCell.Row + row + 1, startCell.Column + col] = cellValue;
                        }
                    }
                }

                // Resize the ListObject to include all data
                var endCell = worksheet.Cells[startCell.Row + rows.Count, startCell.Column + columns.Count - 1];
                listObject.Resize(worksheet.Range[startCell, endCell]);
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to populate ListObject with JSON data, will fall back to query refresh");
            throw;
        }
    }

    public static object GetActiveObject(string progId, bool throwOnError = false)
    {
        if (progId == null)
            throw new ArgumentNullException(nameof(progId));

        var hr = CLSIDFromProgIDEx(progId, out var clsid);
        if (hr < 0)
        {
            if (throwOnError)
                Marshal.ThrowExceptionForHR(hr);
        }

        hr = GetActiveObject(clsid, IntPtr.Zero, out var obj);
        if (hr < 0)
        {
            if (throwOnError)
                Marshal.ThrowExceptionForHR(hr);
        }
        return obj;
    }

    [DllImport("ole32")]
    private static extern int CLSIDFromProgIDEx([MarshalAs(UnmanagedType.LPWStr)] string lpszProgID, out Guid lpclsid);

    [DllImport("oleaut32")]
    private static extern int GetActiveObject([MarshalAs(UnmanagedType.LPStruct)] Guid rclsid, IntPtr pvReserved, [MarshalAs(UnmanagedType.IUnknown)] out object ppunk);
}
