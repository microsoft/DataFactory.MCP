# Connection Management Guide

This guide covers how to use the Microsoft Data Factory MCP Server for managing Azure Data Factory and Microsoft Fabric connections.

## Overview

The connection management tools allow you to:
- List all accessible connections across different types
- Retrieve detailed information about specific connections
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