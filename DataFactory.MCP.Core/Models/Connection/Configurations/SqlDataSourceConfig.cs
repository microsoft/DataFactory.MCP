using DataFactory.MCP.Models.Connection.Interfaces;
using DataFactory.MCP.Models.Connection.Create.Parameters;

namespace DataFactory.MCP.Models.Connection.Configurations;

/// <summary>
/// SQL Server data source configuration
/// </summary>
public class SqlDataSourceConfig : IDataSourceConfig
{
    public string Type => "SQL";
    public string CreationMethod => "SQL";
    public string ServerName { get; }
    public string DatabaseName { get; }

    public SqlDataSourceConfig(string serverName, string databaseName)
    {
        ServerName = serverName;
        DatabaseName = databaseName;
    }

    public List<ConnectionDetailsParameter> GetParameters()
    {
        return new List<ConnectionDetailsParameter>
        {
            new ConnectionDetailsTextParameter { Name = "server", Value = ServerName },
            new ConnectionDetailsTextParameter { Name = "database", Value = DatabaseName }
        };
    }

    public static SqlDataSourceConfig Create(string serverName, string databaseName)
    {
        return new SqlDataSourceConfig(serverName, databaseName);
    }
}