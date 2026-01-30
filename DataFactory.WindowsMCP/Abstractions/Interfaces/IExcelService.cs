using DataFactory.WindowsMCP.Models.Dataflow.Excel;

namespace DataFactory.WindowsMCP.Abstractions.Interfaces;

/// <summary>
/// Service for interacting with Microsoft Excel API
/// </summary>
public interface IExcelService
{
    /// <summary>
    /// Adds a query table to an Excel workbook
    /// </summary>
    /// <param name="mashupQuery">The M query for the query table</param>
    /// <param name="jsonData">The JSON data to populate the query table</param>
    /// <returns>Add operation result</returns>
    Task<AddQueryTableResponse> AddQueryTableAsync(
        string mashupQuery,
        string jsonData);
}