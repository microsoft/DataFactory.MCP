# Microsoft Data Factory MCP Server Architecture

This document provides a comprehensive overview of the Microsoft Data Factory MCP Server architecture, design decisions, and implementation details.

## Table of Contents

- [Overview](#overview)
- [High-Level Architecture](#high-level-architecture)
- [Component Details](#component-details)
- [Data Flow](#data-flow)
- [Security Architecture](#security-architecture)
- [Extension Points](#extension-points)
- [Design Patterns](#design-patterns)
- [Performance Considerations](#performance-considerations)
- [Future Enhancements](#future-enhancements)

## Overview

The Microsoft Data Factory MCP Server is a .NET-based application that implements the Model Context Protocol (MCP) to provide AI assistants with the capability to interact with Azure Data Factory and Microsoft Fabric gateways. The server acts as a bridge between AI chat interfaces and Microsoft Graph APIs.

### Key Design Principles

- **Separation of Concerns**: Clear boundaries between authentication, gateway management, and MCP protocol handling
- **Dependency Injection**: Loose coupling through interfaces and DI container
- **Async-First**: All I/O operations use async/await patterns
- **Configuration-Driven**: Behavior controlled through configuration and environment variables
- **Extensibility**: Plugin architecture for additional services and tools
- **Security**: Secure authentication and token management

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                             AI Chat Interface                              │
│                          (VS Code, Visual Studio)                          │
└─────────────────────────┬───────────────────────────────────────────────────┘
                          │ MCP Protocol (JSON-RPC over stdio)
                          ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                        DataFactory MCP Server                              │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────────────┐ │
│  │   MCP Tools     │  │   Core Services │  │      Abstractions          │ │
│  │                 │  │                 │  │                             │ │
│  │ ┌─────────────┐ │  │ ┌─────────────┐ │  │ ┌─────────────────────────┐ │ │
│  │ │ AuthTool    │ │  │ │ AuthService │ │  │ │ IAuthenticationService  │ │ │
│  │ └─────────────┘ │  │ └─────────────┘ │  │ └─────────────────────────┘ │ │
│  │ ┌─────────────┐ │  │ ┌─────────────┐ │  │ ┌─────────────────────────┐ │ │
│  │ │ GatewayTool │ │  │ │GatewayService│ │  │ │ IFabricGatewayService   │ │ │
│  │ └─────────────┘ │  │ └─────────────┘ │  │ └─────────────────────────┘ │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────────────────┘ │
│                          │                          │                      │
│                          ▼                          ▼                      │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────────────┐ │
│  │     Models      │  │   Extensions    │  │         Utilities          │ │
│  │                 │  │                 │  │                             │ │
│  │ • Gateway       │  │ • Gateway       │  │ • Logging                   │ │
│  │ • Auth Result   │  │   Extensions    │  │ • Configuration             │ │
│  │ • Azure Config  │  │ • JSON          │  │ • Error Handling            │ │
│  └─────────────────┘  │   Converters    │  └─────────────────────────────┘ │
│                       └─────────────────┘                                  │
└─────────────────────────┬───────────────────────────────────────────────────┘
                          │ HTTPS / Microsoft Graph API
                          ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                          Microsoft Azure                                   │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────────────┐ │
│  │   Azure AD      │  │ Microsoft Graph │  │     Data Factory           │ │
│  │ Authentication  │  │      API        │  │      Gateways              │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Component Details

### 1. Application Entry Point

**File**: `Program.cs`

The main entry point configures the application using the .NET Generic Host pattern:

```csharp
var builder = Host.CreateApplicationBuilder(args);

// Configure logging to stderr (stdout reserved for MCP protocol)
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

// Configure Azure AD settings
builder.Services.Configure<AzureAdConfiguration>(
    builder.Configuration.GetSection(AzureAdConfiguration.SectionName));

// Register services
builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();
builder.Services.AddHttpClient<IFabricGatewayService, FabricGatewayService>();

// Register MCP tools
builder.Services.AddTransient<AuthenticationTool>();
builder.Services.AddTransient<GatewayTool>();

// Configure MCP server
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<AuthenticationTool>()
    .WithTools<GatewayTool>();

await builder.Build().RunAsync();
```

### 2. MCP Tools Layer

**Location**: `Tools/`

MCP Tools are the public interface that AI assistants interact with. They handle:
- Parameter validation
- Input sanitization  
- Error handling and user-friendly responses
- Delegation to core services

#### AuthenticationTool
- `AuthenticateInteractiveAsync()`: Interactive Azure AD login
- `AuthenticateServicePrincipalAsync()`: Service principal authentication
- `GetAuthenticationStatusAsync()`: Current auth status
- `GetAccessTokenAsync()`: Retrieve access token
- `SignOutAsync()`: Clear authentication

#### GatewayTool  
- `ListGatewaysAsync()`: List accessible gateways
- `GetGatewayAsync()`: Get gateway details by ID

### 3. Core Services Layer

**Location**: `Services/`

Core services implement the business logic and handle external API interactions.

#### AuthenticationService
Implements `IAuthenticationService` and handles:
- Azure AD authentication flows
- Token management and storage
- Credential validation
- Multi-tenant support

Key Methods:
```csharp
public async Task<string> AuthenticateInteractiveAsync()
public async Task<string> AuthenticateServicePrincipalAsync(string applicationId, string clientSecret, string? tenantId)
public async Task<AuthenticationResult> GetAuthenticationStatusAsync()
public async Task<string> GetAccessTokenAsync()
public async Task<string> SignOutAsync()
```

#### FabricGatewayService
Implements `IFabricGatewayService` and handles:
- Microsoft Graph API calls
- Gateway data retrieval and formatting
- Pagination and filtering
- Error handling and retry logic

Key Methods:
```csharp
public async Task<GatewayResponse> ListGatewaysAsync(string? continuationToken = null)
public async Task<Gateway> GetGatewayAsync(string gatewayId)
```

### 4. Abstractions Layer

**Location**: `Abstractions/`

Defines interfaces and base classes that enable testability and extensibility.

#### Interfaces
- `IAuthenticationService`: Authentication operations contract
- `IFabricGatewayService`: Gateway operations contract

#### Base Classes
- `FabricServiceBase`: Common functionality for Fabric services

### 5. Models Layer

**Location**: `Models/`

Data Transfer Objects (DTOs) and configuration models:

#### Authentication Models
- `AuthenticationResult`: Authentication status and user info
- `AzureAdConfiguration`: Azure AD configuration settings

#### Gateway Models
- `Gateway`: Base gateway information
- `OnPremisesGateway`: On-premises gateway specific data
- `OnPremisesGatewayPersonal`: Personal gateway data
- `VirtualNetworkGateway`: Virtual network gateway data
- `GatewayResponse`: API response wrapper with pagination

### 6. Extensions Layer

**Location**: `Extensions/`

Extension methods and utility functions:

#### GatewayExtensions
- `ToFormattedInfo()`: Format gateway data for display
- Type-specific formatting methods

#### JSON Converters
- `GatewayJsonConverter`: Handle polymorphic gateway deserialization

## Data Flow

### Authentication Flow

```
1. AI Assistant → AuthenticationTool
2. AuthenticationTool → AuthenticationService
3. AuthenticationService → Azure AD (via MSAL)
4. Azure AD → Returns tokens
5. AuthenticationService → Stores tokens
6. AuthenticationService → AuthenticationTool (success/failure)
7. AuthenticationTool → AI Assistant (formatted response)
```

### Gateway Operations Flow

```
1. AI Assistant → GatewayTool
2. GatewayTool → Validates authentication
3. GatewayTool → FabricGatewayService
4. FabricGatewayService → Microsoft Graph API
5. Microsoft Graph API → Returns gateway data
6. FabricGatewayService → Processes and formats data
7. FabricGatewayService → GatewayTool
8. GatewayTool → AI Assistant (formatted response)
```

### Error Flow

```
1. Service encounters error
2. Service logs error details
3. Service transforms technical error to user-friendly message
4. Tool receives processed error message
5. Tool returns formatted error to AI Assistant
```

## Security Architecture

### Authentication Security

- **Token Storage**: In-memory storage with automatic expiration
- **Credential Protection**: Never log or expose secrets
- **Secure Communication**: HTTPS only for external API calls
- **Token Refresh**: Automatic token refresh when possible

### API Security

- **Input Validation**: All user inputs validated and sanitized
- **Authorization**: Token-based access control
- **Rate Limiting**: Respect Microsoft Graph API rate limits
- **Error Sanitization**: No sensitive data in error messages

### Configuration Security

- **Environment Variables**: Secrets stored in environment variables
- **No Hardcoded Secrets**: All credentials externally configured
- **Principle of Least Privilege**: Minimal required permissions

## Extension Points

### Adding New Tools

1. Create tool class implementing MCP tool attributes:
```csharp
[McpServerToolType]
public class NewTool
{
    [McpServerTool, Description("Description of the tool")]
    public async Task<string> NewOperationAsync(string parameter)
    {
        // Implementation
    }
}
```

2. Register in `Program.cs`:
```csharp
builder.Services.AddTransient<NewTool>();
builder.Services.WithTools<NewTool>();
```

### Adding New Services

1. Define interface:
```csharp
public interface INewService
{
    Task<string> PerformOperationAsync();
}
```

2. Implement service:
```csharp
public class NewService : INewService
{
    public async Task<string> PerformOperationAsync()
    {
        // Implementation
    }
}
```

3. Register service:
```csharp
builder.Services.AddTransient<INewService, NewService>();
```

## Design Patterns

### Dependency Injection
- Constructor injection for all dependencies
- Interface-based design for testability
- Scoped lifetimes for services, transient for tools

### Repository Pattern (Implicit)
- Services act as repositories for external data
- Abstracted data access through interfaces
