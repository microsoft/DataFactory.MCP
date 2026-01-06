using DataFactory.MCP.Models.Connection.Create.Parameters;

namespace DataFactory.MCP.Models.Connection.Interfaces;

/// <summary>
/// Interface for data source configuration
/// </summary>
public interface IDataSourceConfig
{
    string Type { get; }
    string CreationMethod { get; }
    List<ConnectionDetailsParameter> GetParameters();
}