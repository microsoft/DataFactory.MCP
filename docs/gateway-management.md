# Gateway Management Guide

This guide covers how to use the Microsoft Data Factory MCP Server for managing Azure Data Factory and Microsoft Fabric gateways.

## Overview

The gateway management tools allow you to:
- List all accessible gateways across different types
- Retrieve detailed information about specific gateways
- Work with on-premises, personal mode, and virtual network gateways

## Available Operations

### Create VNet Gateway

Create a new Virtual Network (VNet) gateway in Microsoft Fabric.

#### Usage
```
create_virtualnetwork_gateway(
  displayName: "My Production Gateway",
  capacityId: "12345678-1234-1234-1234-123456789012",
  subscriptionId: "87654321-1234-1234-1234-123456789012",
  resourceGroupName: "my-resource-group",
  virtualNetworkName: "my-vnet",
  subnetName: "gateway-subnet",
  region: "East US"
)
```

#### Example Request
```
Please create a VNet gateway named 'Production Data Gateway' with the following settings:
- Use capacity "Trial-20241028T100000Z-ABC123DEF456"
- Connect to Azure VNet production-vnet in resource group data-rg 
- Use subscription "Production Environment"
- Use subnet gateway-subnet
- Set inactivity timeout to 2 hours
- Configure with 1 member gateway
```

#### Response Format
```json
{
  "message": "Successfully created VNet gateway 'Production Data Gateway' with ID '12345678-1234-1234-1234-123456789012'",
  "gateway": {
    "id": "12345678-1234-1234-1234-123456789012",
    "displayName": "Production Data Gateway",
    "type": "VirtualNetwork",
    "connectivityStatus": "",
    "capacityId": "12345678-1234-1234-1234-123456789012"
  },
  "configuration": {
    "subscriptionId": "87654321-1234-1234-1234-123456789012",
    "resourceGroupName": "data-rg",
    "virtualNetworkName": "production-vnet",
    "subnetName": "gateway-subnet",
    "inactivityMinutesBeforeSleep": 120,
    "numberOfMemberGateways": 1
  }
}
```

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

### VNet Gateway Creation
```
# Create a new VNet gateway with natural language
> Please create a VNet gateway named 'Development Gateway' with the following settings:
  - Use capacity "Trial-20241028T100000Z-ABC123DEF456"
  - Connect to Azure VNet dev-network in resource group development-rg 
  - Use subscription "Development Environment"
  - Use subnet data-subnet
  - Set inactivity timeout to 1 hour
  - Configure with 1 member gateway

# Create a VNet gateway for production workloads
> Set up a production VNet gateway called 'Prod Data Gateway' connected to:
  - Capacity: Premium-P1-Production
  - Azure subscription: Production-Sub-001
  - Resource group: prod-data-rg
  - Virtual network: production-vnet
  - Subnet: gateway-subnet
  - Timeout: 4 hours of inactivity
  - Member gateways: 2 for high availability
```

### Basic Gateway Operations
```
# List all available gateways
> show me all my data factory gateways

# Get specific gateway details by ID
> get details for gateway with ID 12345678-1234-1234-1234-123456789012

# Get specific gateway details by name
> get details for gateway with name test-gateway
```
