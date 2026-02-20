# Connection Management Guide

This guide covers how to use the Microsoft Data Factory MCP Server for managing Azure Data Factory and Microsoft Fabric connections.

## Overview

The connection management tools allow you to:
- List all accessible connections across different types
- Retrieve detailed information about specific connections
- Create new cloud, on-premises (gateway), and virtual network connections
- Discover supported connection types, parameters, and credential kinds
- Work with on-premises, virtual network, and cloud connections

## Available Operations

### List Connections

Retrieve a list of all connections you have access to.

#### Usage
```
list_connections
```

#### With Pagination
```
list_connections(continuationToken: "next-page-token")
```

#### Response Format
```json
{
  "totalCount": 112,
  "continuationToken": null,
  "hasMoreResults": false,
  "connections": [
    {
      "id": "a0b9fa12-60f5-4f95-85ca-565d34abcea1",
      "displayName": "Example Cloud Data Source",
      "connectivityType": "OnPremisesGateway",
      "connectionDetails": {
        "type": "Web",
        "path": "http://www.microsoft.com/"
      },
      "privacyLevel": "Organizational",
      "credentialDetails": {
        "credentialType": "Anonymous",
        "singleSignOnType": "None",
        "connectionEncryption": "Any",
        "skipTestConnection": false
      },
      "gatewayId": "7d3b5733-732d-4bbe-8d17-db6f6fe5d19c"
    }
  ]
}
```

### Get Connection Details

Retrieve detailed information about a specific connection.

#### Usage
```
get_connection(connectionId: "a0b9fa12-60f5-4f95-85ca-565d34abcea1")
```

#### Response Format
```json
{
  "id": "a0b9fa12-60f5-4f95-85ca-565d34abcea1",
  "displayName": "Example Cloud Data Source",
  "connectivityType": "OnPremisesGateway",
  "connectionDetails": {
    "type": "Web",
    "path": "http://www.microsoft.com/"
  },
  "privacyLevel": "Organizational",
  "credentialDetails": {
    "credentialType": "Anonymous",
    "singleSignOnType": "None",
    "connectionEncryption": "Any",
    "skipTestConnection": false
  },
  "gatewayId": "7d3b5733-732d-4bbe-8d17-db6f6fe5d19c"
}
```

### List Supported Connection Types

Retrieve all available connection types along with their creation methods, parameters, and supported credential kinds. Use this before calling `create_connection` to discover valid values.

#### Usage
```
list_supported_connection_types
```

#### With Gateway Filter
```
list_supported_connection_types(gatewayId: "7d3b5733-732d-4bbe-8d17-db6f6fe5d19c")
```

#### Response Format
```json
{
  "totalCount": 45,
  "connectionTypes": [
    {
      "type": "SQL",
      "creationMethods": [
        {
          "name": "SQL",
          "parameters": [
            { "name": "server", "dataType": "Text", "required": true, "allowedValues": null },
            { "name": "database", "dataType": "Text", "required": false, "allowedValues": null }
          ]
        }
      ],
      "supportedCredentialTypes": ["Basic", "Windows", "OAuth2", "ServicePrincipal"],
      "supportedConnectionEncryptionTypes": ["Encrypted", "NotEncrypted", "Any"],
      "supportsSkipTestConnection": true
    }
  ]
}
```

### Create Connection

Creates a new data source connection. Supports cloud, on-premises (gateway), and virtual network connectivity types.

> **Tip**: Use `list_supported_connection_types` first to discover the correct `connectionType`, `creationMethod`, parameters, and credential types for your data source.
>
> **UI alternative**: Use the `ui://datafactory/create-connection` resource to open the guided Create Connection form inside VS Code chat.

#### Usage — SQL (Basic auth, cloud)
```
create_connection(
  connectionName: "My SQL Connection",
  connectionType: "SQL",
  connectionParameters: '{"server":"myserver.database.windows.net","database":"mydb"}',
  credentialType: "Basic",
  credentials: '{"username":"myuser","password":"mypassword"}',
  connectionEncryption: "Encrypted"
)
```

#### Usage — Azure Blob Storage (Anonymous, cloud)
```
create_connection(
  connectionName: "My Blob Storage",
  connectionType: "AzureBlobs",
  connectionParameters: '{"accountDomain":"blob.core.windows.net","accountName":"mystorage"}',
  credentialType: "Anonymous"
)
```

#### Usage — On-premises gateway (SQL, Windows auth)
```
create_connection(
  connectionName: "On-Prem SQL",
  connectionType: "SQL",
  connectionParameters: '{"server":"myserver","database":"mydb"}',
  credentialType: "Windows",
  credentials: '{"username":"DOMAIN\\user","password":"pass"}',
  connectivityType: "OnPremisesGateway",
  gatewayId: "7d3b5733-732d-4bbe-8d17-db6f6fe5d19c"
)
```

#### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `connectionName` | string | required | Display name for the connection |
| `connectionType` | string | `"SQL"` | Connection type identifier (e.g. `SQL`, `Web`, `AzureBlobs`) |
| `creationMethod` | string | same as `connectionType` | Creation method name from `list_supported_connection_types` |
| `connectionParameters` | JSON string | optional | Name-value pairs for connection details (e.g. server, database) |
| `credentialType` | string | `"Anonymous"` | One of: `Anonymous`, `Basic`, `Windows`, `OAuth2`, `Key`, `SharedAccessSignature`, `ServicePrincipal`, `WorkspaceIdentity`, `KeyPair` |
| `credentials` | JSON string | optional | Credential fields (e.g. `username`, `password`, `key`, `tenantId`, `servicePrincipalClientId`, `servicePrincipalSecret`) |
| `privacyLevel` | string | `"None"` | One of: `None`, `Organizational`, `Public`, `Private` |
| `connectionEncryption` | string | `"NotEncrypted"` | One of: `NotEncrypted`, `Encrypted`, `Any` |
| `skipTestConnection` | bool | `false` | Skip the connection test on creation |
| `connectivityType` | string | `"ShareableCloud"` | One of: `ShareableCloud`, `OnPremisesGateway`, `VirtualNetworkGateway` |
| `gatewayId` | string | optional | Required when `connectivityType` is `OnPremisesGateway` or `VirtualNetworkGateway` |

#### Response Format
```json
{
  "success": true,
  "connection": {
    "id": "b1c2d3e4-0000-0000-0000-000000000000",
    "displayName": "My SQL Connection",
    "connectivityType": "ShareableCloud",
    "connectionDetails": {
      "type": "SQL",
      "path": "myserver.database.windows.net;mydb"
    },
    "privacyLevel": "None",
    "credentialDetails": {
      "credentialType": "Basic",
      "singleSignOnType": "None",
      "connectionEncryption": "Encrypted",
      "skipTestConnection": false
    }
  }
}
```

## UI Resource: Create Connection Form

The server exposes a guided HTML form as an MCP App resource that can be rendered inside VS Code chat.

| Property | Value |
|----------|-------|
| Resource URI | `ui://datafactory/create-connection` |
| MIME type | `text/html;profile=mcp-app` |
| Description | Interactive form for creating a new data source connection |

The form dynamically loads supported connection types, prompts for parameters and credentials based on the selected type, and calls `create_connection` on submit.

## Usage Examples

### Basic Connection Operations
```
# List all available connections
> show me all my data factory connections

# Get specific connection details by ID
> get details for connection with ID a0b9fa12-60f5-4f95-85ca-565d34abcea1

# Get specific connection details by name
> get details for connection with name Example Cloud Data Source
```

### Discovering and Creating Connections
```
# Discover what connection types are available
> what connection types are supported?

# Discover supported types for a specific gateway
> what data sources does gateway 7d3b5733-732d-4bbe-8d17-db6f6fe5d19c support?

# Create a new SQL connection
> create a SQL connection named "Production DB" to server myserver.database.windows.net, database mydb, using Basic auth with username sa

# Create an on-premises connection
> create an on-premises SQL connection named "On-Prem Sales" on gateway 7d3b5733, server SQLSRV01, database SalesDB, Windows auth

# Open the interactive Create Connection form
> open the create connection form
```
