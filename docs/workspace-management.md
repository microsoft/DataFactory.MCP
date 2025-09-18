# Workspace Management Guide

This guide covers how to use the Microsoft Data Factory MCP Server for managing Microsoft Fabric workspaces.

## Overview

The workspace management tools allow you to:
- List all accessible workspaces with filtering by role
- Work with different workspace types (Personal, Shared, Admin)
- Navigate paginated results for large workspace collections

## Available Operations

### List Workspaces

Retrieve a list of all workspaces you have access to with full details.

#### Usage
```
list_workspaces
```

#### With Role Filtering
```
list_workspaces(roles: "Admin,Member")
```

#### With Pagination
```
list_workspaces(continuationToken: "next-page-token")
```

#### With Workspace-Specific Endpoints
```
list_workspaces(preferWorkspaceSpecificEndpoints: true)
```

#### Response Format
```json
{
  "totalCount": 10,
  "continuationToken": "eyJza2lwIjoyMCwidGFrZSI6MjB9",
  "hasMoreResults": true,
  "workspaces": [
    {
      "id": "12345678-1234-1234-1234-123456789012",
      "name": "Sales Analytics Workspace",
      "description": "Workspace for sales team analytics and reporting",
      "type": "PersonalGroup",
      "state": "Active",
      "isReadOnly": false,
      "isOnDedicatedCapacity": true,
      "capacityId": "87654321-4321-4321-4321-210987654321",
      "defaultDatasetStorageFormat": "Small"
    }
  ]
}
```

## Role-Based Filtering

The workspace tools support filtering by user roles within workspaces:

- **Admin**: Full administrative access to the workspace
- **Member**: Can contribute content and manage workspace settings
- **Contributor**: Can contribute content but cannot manage workspace settings  
- **Viewer**: Read-only access to workspace content

### Multiple Role Filtering
```
# Filter by multiple roles (comma-separated)
list_workspaces(roles: "Admin,Member,Contributor")

# Filter by single role
list_workspaces(roles: "Admin")
```

## Workspace Types

The system supports different workspace types:

- **PersonalGroup**: Personal workspace for individual users
- **Group**: Shared workspace for team collaboration
- **AdminWorkspace**: Administrative workspace with elevated privileges

## Usage Examples

### Basic Workspace Operations
```
# List all accessible workspaces
> show me all my fabric workspaces

# List workspaces where I'm an admin
> list workspaces where I have admin role

# List workspaces with pagination
> list my workspaces with continuation token abc123
```

### Advanced Filtering
```
# List workspaces by specific roles
> show me workspaces where I'm an admin or member

# List only active workspaces
> show me all active workspaces
```

### Pagination Scenarios
```
# Handle large workspace collections
> list all my workspaces (this may return a continuation token for more results)

# Continue listing with pagination token
> continue listing workspaces with token eyJza2lwIjoyMCwidGFrZSI6MjB9
```