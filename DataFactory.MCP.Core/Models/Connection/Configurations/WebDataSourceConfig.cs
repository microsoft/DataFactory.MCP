using DataFactory.MCP.Models.Connection.Interfaces;
using DataFactory.MCP.Models.Connection.Create.Parameters;

namespace DataFactory.MCP.Models.Connection.Configurations;

/// <summary>
/// Web data source configuration
/// </summary>
public class WebDataSourceConfig : IDataSourceConfig
{
    public string Type => "Web";
    public string CreationMethod => "Web";
    public string Url { get; }

    public WebDataSourceConfig(string url)
    {
        Url = url;
    }

    public List<ConnectionDetailsParameter> GetParameters()
    {
        return new List<ConnectionDetailsParameter>
        {
            new ConnectionDetailsTextParameter { Name = "url", Value = Url }
        };
    }

    public static WebDataSourceConfig Create(string url)
    {
        return new WebDataSourceConfig(url);
    }
}