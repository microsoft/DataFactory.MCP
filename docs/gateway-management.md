# Gateway Management Guide

This guide covers how to use the Microsoft Data Factory MCP Server for managing Azure Data Factory and Microsoft Fabric gateways.

## Overview

The gateway management tools allow you to:
- List all accessible gateways across different types
- Retrieve detailed information about specific gateways
- Work with on-premises, personal mode, and virtual network gateways

## Available Operations

### List Gateways

Retrieve a list of all gateways you have access to.

#### Usage
```
list_gateways
```

#### With Pagination
```
list_gateways(continuationToken: "next-page-token")
```

#### Response Format
```json
{
  "totalCount": 5,
  "continuationToken": "eyJza2lwIjoyMCwidGFrZSI6MjB9",
  "hasMoreResults": true,
  "gateways": [
    {
      "id": "12345678-1234-1234-1234-123456789012",
      "name": "My Data Gateway",
      "type": "OnPremisesGateway",
      "status": "Online",
      "version": "3000.123.4",
      "location": "East US",
      "description": "Production data gateway for sales data"
    }
  ]
}
```

### Get Gateway Details

Retrieve detailed information about a specific gateway.

#### Usage
```
get_gateway(gatewayId: "12345678-1234-1234-1234-123456789012")
```

#### Response Format
```json
{
  "id": "12345678-1234-1234-1234-123456789012",
  "name": "My Data Gateway",
  "type": "OnPremisesGateway",
  "status": "Online",
  "version": "3000.123.4",
  "location": "East US",
  "description": "Production data gateway for sales data",
  "contactInformation": "admin@company.com",
  "machineNames": ["GATEWAY-SERVER-01"],
  "gatewayInstallId": "87654321-4321-4321-4321-210987654321",
  "loadBalancing": {
    "enabled": true,
    "members": [
      {
        "memberId": "member-1",
        "status": "Online",
        "version": "3000.123.4"
      }
    ]
  },
  "publicKey": {
    "exponent": "AQAB",
    "modulus": "base64-encoded-modulus"
  }
}
```

## Usage Examples

### Basic Gateway Operations
```
# List all available gateways
> show me all my data factory gateways

# Get specific gateway details by ID
> get details for gateway with ID 12345678-1234-1234-1234-123456789012

# Get specific gateway details by name
> get details for gateway with name test-gateway
```
