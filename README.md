# Microsoft Data Factory MCP Server

A Model Context Protocol (MCP) server for Microsoft Data Factory and Azure Fabric Gateway management operations. This server provides tools for authentication and gateway management through a standardized MCP interface.

## Features

- 🔐 **Azure AD Authentication**: Interactive and service principal authentication
- 🌐 **Gateway Management**: List and manage Azure Data Factory gateways
- 🏗️ **Microsoft Fabric Integration**: Support for on-premises, personal, and virtual network gateways
- 📦 **NuGet Distribution**: Available as a NuGet package for easy integration
- 🔧 **MCP Protocol**: Built using the official MCP C# SDK

## Available Tools

- **Authentication**: `authenticate_interactive`, `authenticate_service_principal`, `get_authentication_status`, `get_access_token`, `sign_out`
- **Gateway Management**: `list_gateways`, `get_gateway`

## Quick Start

### Using from NuGet (Recommended)

1. **Configure your IDE**: Create an MCP configuration file in your workspace:

   **VS Code**: Create `.vscode/mcp.json`
   **Visual Studio**: Create `.mcp.json` in solution directory

   ```json
   {
     "servers": {
       "DataFactory.MCP": {
         "type": "stdio",
         "command": "dnx",
         "args": [
           "Microsoft.DataFactory.MCP",
           "--version",
           "0.2.0-beta",
           "--yes"
         ]
       }
     }
   }
   ```

2. **Start using**: The server will be automatically downloaded and available in your IDE's MCP-enabled chat interface.

### Development Setup

To run the server locally during development:

```json
{
  "servers": {
    "DataFactory.MCP": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "path/to/DataFactory.MCP"
      ]
    }
  }
}
```

## Configuration

### Prerequisites

- .NET 10.0 or later
- Azure AD tenant and application registration with appropriate permissions
- Environment variables for authentication (see [Authentication Guide](https://github.com/microsoft/DataFactory.MCP/blob/main/docs/authentication.md) for setup details)


## Usage Examples

See the detailed guides for comprehensive usage instructions:
- **Authentication**: See [Authentication Guide](https://github.com/microsoft/DataFactory.MCP/blob/main/docs/authentication.md)
- **Gateway Management**: See [Gateway Management Guide](https://github.com/microsoft/DataFactory.MCP/blob/main/docs/gateway-management.md)

## Development

### Building the Project

```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Create NuGet package
dotnet pack -c Release
````

### Testing Locally

1. Configure your IDE with the development configuration shown above
2. Run the project: `dotnet run`
3. Test the tools through your MCP-enabled chat interface

## Documentation

For complete documentation, see our **[Documentation Index](https://github.com/microsoft/DataFactory.MCP/blob/main/docs/index.md)**.

### Quick Links
- **[Authentication Guide](https://github.com/microsoft/DataFactory.MCP/blob/main/docs/authentication.md)** - Complete authentication setup and usage
- **[Gateway Management Guide](https://github.com/microsoft/DataFactory.MCP/blob/main/docs/gateway-management.md)** - Gateway operations and examples
- **[Architecture Guide](https://github.com/microsoft/DataFactory.MCP/blob/main/docs/ARCHITECTURE.md)** - Technical architecture and design details

## Contributing

We welcome contributions! To get started:
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

Please follow standard .NET coding conventions and ensure all tests pass before submitting.

### Extension Points

The server is designed for extensibility. For detailed information on extending functionality, see the [Extension Points section](https://github.com/microsoft/DataFactory.MCP/blob/main/docs/ARCHITECTURE.md#extension-points) in our architecture documentation, which covers:

- **Adding New Tools**: Create custom MCP tools for additional operations
- **Adding New Services**: Implement new services following our patterns
- **Service Registration**: Proper dependency injection setup

This modular architecture makes it easy to add support for additional Azure services or custom business logic.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For issues and questions:
- Create an issue in this repository
- Review the [MCP documentation](https://modelcontextprotocol.io/)
