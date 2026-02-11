# Microsoft Data Factory MCP Server Architecture

This document provides a comprehensive overview of the Microsoft Data Factory MCP Server architecture, design decisions, and implementation details.

## Table of Contents

- [Overview](#overview)
- [High-Level Architecture](#high-level-architecture)
- [Component Details](#component-details)
  - [Application Entry Point](#1-application-entry-point)
  - [MCP Tools Layer](#2-mcp-tools-layer)
  - [Core Services Layer](#3-core-services-layer)
  - [Abstractions Layer](#4-abstractions-layer)
  - [Models Layer](#5-models-layer)
  - [Extensions Layer](#6-extensions-layer)
  - [Infrastructure Layer](#7-infrastructure-layer)
  - [Configuration Layer](#8-configuration-layer)
- [Data Flow](#data-flow)
- [Security Architecture](#security-architecture)
- [Extension Points](#extension-points)
- [Design Patterns](#design-patterns)
- [Performance Considerations](#performance-considerations)
- [Future Enhancements](#future-enhancements)

## Overview

The Microsoft Data Factory MCP Server is a .NET-based application that implements the Model Context Protocol (MCP) to provide AI assistants with comprehensive capabilities to interact with Microsoft Fabric services, including gateways, connections, workspaces, dataflows, and capacities. The server acts as a bridge between AI chat interfaces and Microsoft Fabric APIs.

### Key Design Principles

- **Separation of Concerns**: Clear boundaries between authentication, service management, infrastructure, and MCP protocol handling
- **Dependency Injection**: Loose coupling through interfaces and DI container with proper service lifetimes
- **Async-First**: All I/O operations use async/await patterns
- **Configuration-Driven**: Behavior controlled through configuration, environment variables, and feature flags
- **Extensibility**: Plugin architecture for additional services and tools with feature flag support
- **Security**: Secure authentication through delegating handlers and centralized token management
- **Centralized API Management**: API versions and URLs managed through dedicated configuration classes

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
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                         MCP Tools Layer                              │   │
│  │  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌────────────┐  │   │
│  │  │Authentication│ │   Gateway    │ │ Connections  │ │ Workspaces │  │   │
│  │  │    Tool      │ │    Tool      │ │    Tool      │ │   Tool     │  │   │
│  │  └──────────────┘ └──────────────┘ └──────────────┘ └────────────┘  │   │
│  │  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌────────────┐  │   │
│  │  │  Dataflow    │ │DataflowQuery │ │  Capacity    │ │ AzureRes   │  │   │
│  │  │    Tool      │ │ Tool (flag)  │ │    Tool      │ │ Discovery  │  │   │
│  │  └──────────────┘ └──────────────┘ └──────────────┘ └────────────┘  │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                    │                                        │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                      Core Services Layer                             │   │
│  │  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌────────────┐  │   │
│  │  │Authentication│ │FabricGateway │ │FabricConnect │ │FabricWork- │  │   │
│  │  │   Service    │ │   Service    │ │   Service    │ │spaceService│  │   │
│  │  └──────────────┘ └──────────────┘ └──────────────┘ └────────────┘  │   │
│  │  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌────────────┐  │   │
│  │  │FabricDataflow│ │FabricCapacity│ │ AzureResource│ │Validation  │  │   │
│  │  │   Service    │ │   Service    │ │ Discovery    │ │  Service   │  │   │
│  │  └──────────────┘ └──────────────┘ └──────────────┘ └────────────┘  │   │
│  │  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌────────────┐  │   │
│  │  │ ArrowData    │ │DataTransform │ │DataflowDef   │ │GatewayClus-│  │   │
│  │  │ReaderService │ │   Service    │ │  Processor   │ │terDatasrc  │  │   │
│  │  └──────────────┘ └──────────────┘ └──────────────┘ └────────────┘  │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                    │                                        │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                     Infrastructure Layer                             │   │
│  │  ┌──────────────────────────────┐ ┌─────────────────────────────┐   │   │
│  │  │     HTTP Client Pipeline     │ │       Configuration         │   │   │
│  │  │  ┌─────────────────────────┐ │ │  ┌───────────────────────┐  │   │   │
│  │  │  │FabricAuthHandler       │ │ │  │ ApiVersions           │  │   │   │
│  │  │  └─────────────────────────┘ │ │  │ FeatureFlags          │  │   │   │
│  │  │  ┌─────────────────────────┐ │ │  │ HttpClientNames       │  │   │   │
│  │  │  │AzureRMAuthHandler      │ │ │  │ JsonSerializerOptions │  │   │   │
│  │  │  └─────────────────────────┘ │ │  └───────────────────────┘  │   │   │
│  │  │  ┌─────────────────────────┐ │ └─────────────────────────────┘   │   │
│  │  │  │FabricUrlBuilder        │ │                                    │   │
│  │  │  │TokenValidator          │ │                                    │   │
│  │  │  └─────────────────────────┘ │                                    │   │
│  │  └──────────────────────────────┘                                    │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                    │                                        │
│  ┌──────────────────────────┐  ┌──────────────────────────────────────┐    │
│  │       Abstractions       │  │            Extensions                │    │
│  │  ┌────────────────────┐  │  │  ┌──────────────┐ ┌──────────────┐   │    │
│  │  │ IAuthenticationSvc │  │  │  │Gateway Ext   │ │Connection Ext│   │    │
│  │  │ IFabricGatewaySvc  │  │  │  │Workspace Ext │ │Dataflow Ext  │   │    │
│  │  │ IFabricConnectSvc  │  │  │  │Capacity Ext  │ │ArrowData Ext │   │    │
│  │  │ IFabricWorkspaceSvc│  │  │  │Response Ext  │ │MQuery Ext    │   │    │
│  │  │ IFabricDataflowSvc │  │  │  │Json Ext      │ │HttpResponse  │   │    │
│  │  │ IFabricCapacitySvc │  │  │  └──────────────┘ └──────────────┘   │    │
│  │  │ IValidationService │  │  └──────────────────────────────────────┘    │                                              │
│  │  │ IArrowDataReader   │  │                                              │
│  │  │ IDataTransformSvc  │  │                                              │
│  │  │ IDataflowDefProc   │  │                                              │
│  │  └────────────────────┘  │                                              │
│  └──────────────────────────┘                                              │
└─────────────────────────────┬───────────────────────────────────────────────┘
                              │ HTTPS
                              ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                            External APIs                                   │
│  ┌─────────────────┐  ┌─────────────────────┐  ┌───────────────────────┐   │
│  │    Azure AD     │  │ Microsoft Fabric API │                              │
│  │ Authentication  │  │ api.fabric.microsoft │                              │
│  │   via MSAL      │  │   .com/v1/           │                              │
│  └─────────────────┘  └─────────────────────┘                              │
│                       │  • Gateways           │                              │
│                       │  • Connections        │                              │
│                       │  • Workspaces         │                              │
│                       │  • Dataflows          │                              │
│                       │  • Capacities         │                              │
│                       └─────────────────────┘                               │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                    Power BI API (v2.0)                               │   │
│  │                    api.powerbi.com/v2.0                              │   │
│  │                    • Gateway Cluster Datasources                     │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Component Details

### 1. Application Entry Point

**File**: `Program.cs`

The main entry point configures the application using the .NET Generic Host pattern with named HTTP clients and authentication handlers:

```csharp
var builder = Host.CreateApplicationBuilder(args);

// Configure logging to stderr (stdout reserved for MCP protocol)
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

// Register authentication handlers as transient (DelegatingHandlers must be transient)
builder.Services.AddTransient<FabricAuthenticationHandler>();

// Register named HttpClients with authentication handlers
builder.Services.AddHttpClient(HttpClientNames.FabricApi, client =>
{
    client.BaseAddress = new Uri(ApiVersions.Fabric.V1BaseUrl + "/");
    client.Timeout = TimeSpan.FromSeconds(30);
}).AddHttpMessageHandler<FabricAuthenticationHandler>();

builder.Services.AddHttpClient(HttpClientNames.PowerBiV2Api, client =>
{
    client.BaseAddress = new Uri(ApiVersions.PowerBi.V2BaseUrl + "/");
    client.Timeout = TimeSpan.FromSeconds(30);
}).AddHttpMessageHandler<FabricAuthenticationHandler>();

// Register services as singletons
builder.Services
    .AddSingleton<IValidationService, ValidationService>()
    .AddSingleton<IAuthenticationService, AuthenticationService>()
    .AddSingleton<IArrowDataReaderService, ArrowDataReaderService>()
    .AddSingleton<IGatewayClusterDatasourceService, GatewayClusterDatasourceService>()
    .AddSingleton<IDataTransformationService, DataTransformationService>()
    .AddSingleton<IDataflowDefinitionProcessor, DataflowDefinitionProcessor>()
    .AddSingleton<IFabricGatewayService, FabricGatewayService>()
    .AddSingleton<IFabricConnectionService, FabricConnectionService>()
    .AddSingleton<IFabricWorkspaceService, FabricWorkspaceService>()
    .AddSingleton<IFabricDataflowService, FabricDataflowService>()
    .AddSingleton<IFabricCapacityService, FabricCapacityService>()
    .AddSingleton<FabricDataSourceConnectionFactory>();

// Configure MCP server with tools
var mcpBuilder = builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<AuthenticationTool>()
    .WithTools<GatewayTool>()
    .WithTools<ConnectionsTool>()
    .WithTools<WorkspacesTool>()
    .WithTools<DataflowTool>()
    .WithTools<CapacityTool>();

// Conditionally enable DataflowQueryTool based on feature flag
mcpBuilder.RegisterToolWithFeatureFlag<DataflowQueryTool>(
    builder.Configuration,
    args,
    FeatureFlags.DataflowQuery,
    nameof(DataflowQueryTool),
    logger);

await builder.Build().RunAsync();
```

### 2. MCP Tools Layer

**Location**: `Tools/`

MCP Tools are the public interface that AI assistants interact with. Each tool is decorated with `[McpServerToolType]` and individual methods with `[McpServerTool]`. They handle:
- Parameter validation via `IValidationService`
- Input sanitization
- Error handling with user-friendly responses using extension methods
- Delegation to core services
- JSON serialization of responses

#### AuthenticationTool
- `AuthenticateInteractiveAsync()`: Interactive Azure AD login
- `AuthenticateServicePrincipalAsync()`: Service principal authentication with client secret
- `GetAuthenticationStatus()`: Current auth status and profile
- `GetAccessTokenAsync()`: Retrieve Fabric API access token
- `SignOutAsync()`: Clear authentication

#### GatewayTool
- `ListGatewaysAsync()`: List accessible gateways (on-premises, personal, VNet)
- `GetGatewayAsync()`: Get gateway details by ID
- `CreateVNetGatewayAsync()`: Create a new VNet gateway with Azure resource configuration

#### ConnectionsTool
- `ListConnectionsAsync()`: List all accessible connections
- `GetConnectionAsync()`: Get connection details by ID
- `CreateCloudSqlBasicAsync()`: Create cloud SQL connection with basic auth
- `CreateCloudSqlWorkspaceIdentityAsync()`: Create cloud SQL with workspace identity
- `CreateCloudWebAnonymousAsync()`: Create web connection with anonymous auth
- `CreateCloudWebBasicAsync()`: Create web connection with basic auth
- `CreateVNetSqlBasicAsync()`: Create VNet gateway SQL with basic auth
- `CreateVNetSqlWorkspaceIdentityAsync()`: Create VNet gateway SQL with workspace identity

#### WorkspacesTool
- `ListWorkspacesAsync()`: List accessible workspaces with optional role filtering

#### DataflowTool
- `ListDataflowsAsync()`: List dataflows in a workspace
- `CreateDataflowAsync()`: Create a new dataflow
- `GetDecodedDataflowDefinitionAsync()`: Get decoded dataflow definition (queryMetadata.json, mashup.pq, .platform)
- `AddConnectionToDataflowAsync()`: Add a connection to an existing dataflow
- `AddOrUpdateQueryInDataflowAsync()`: Add or update a query in an existing dataflow

#### DataflowQueryTool (Feature Flag: `--dataflow-query`)
- `ExecuteQueryAsync()`: Execute M (Power Query) queries against dataflows with Apache Arrow response parsing

#### CapacityTool
- `ListCapacitiesAsync()`: List Fabric capacities user has access to

### 3. Core Services Layer

**Location**: `Services/`

Core services implement the business logic and handle external API interactions. All services are registered as singletons and use constructor-injected `IHttpClientFactory` for creating named HTTP clients.

#### AuthenticationService
Implements `IAuthenticationService` and handles:
- Azure AD authentication flows via MSAL
- Token management and in-memory storage
- Token acquisition for Fabric API
- Credential validation
- Multi-tenant support

Key Methods:
```csharp
Task<string> AuthenticateInteractiveAsync()
Task<string> AuthenticateServicePrincipalAsync(string applicationId, string clientSecret, string? tenantId)
string GetAuthenticationStatus()
Task<string> GetAccessTokenAsync(string[]? scopes = null)
Task<string> SignOutAsync()
```

#### FabricGatewayService
Implements `IFabricGatewayService` and handles:
- Microsoft Fabric API calls for gateway operations
- Gateway data retrieval and formatting
- VNet gateway creation
- Pagination and filtering
- Error handling with retry logic

Key Methods:
```csharp
Task<GatewayResponse> ListGatewaysAsync(string? continuationToken = null)
Task<Gateway> GetGatewayAsync(string gatewayId)
Task<VirtualNetworkGateway> CreateVNetGatewayAsync(CreateVNetGatewayRequest request)
```

#### FabricConnectionService
Implements `IFabricConnectionService` and handles:
- Connection data retrieval
- Microsoft Fabric API integration
- Connection creation (cloud, VNet gateway)
- Connection type classification
- Pagination support

Key Methods:
```csharp
Task<ConnectionResponse> ListConnectionsAsync(string? continuationToken = null)
Task<Connection> GetConnectionAsync(string connectionId)
Task<Connection> CreateConnectionAsync(CreateConnectionRequest request)
```

#### FabricWorkspaceService
Implements `IFabricWorkspaceService` and handles:
- Workspace data retrieval
- User permission filtering
- Role-based access control
- Workspace metadata management

Key Methods:
```csharp
Task<WorkspaceResponse> ListWorkspacesAsync(string? roles = null, string? continuationToken = null, bool? preferWorkspaceSpecificEndpoints = null)
```

#### FabricDataflowService
Implements `IFabricDataflowService` and handles:
- Dataflow data retrieval from Fabric workspaces
- Dataflow creation and definition management
- Microsoft Fabric Dataflows API integration
- Query execution with Apache Arrow response handling
- Workspace-scoped dataflow listing
- Pagination and error handling

Key Methods:
```csharp
Task<ListDataflowsResponse> ListDataflowsAsync(string workspaceId, string? continuationToken = null)
Task<CreateDataflowResponse> CreateDataflowAsync(string workspaceId, CreateDataflowRequest request)
Task<DecodedDataflowDefinition> GetDecodedDataflowDefinitionAsync(string workspaceId, string dataflowId)
Task<ExecuteDataflowQueryResponse> ExecuteQueryAsync(string workspaceId, string dataflowId, ExecuteDataflowQueryRequest request)
```

#### FabricCapacityService
Implements `IFabricCapacityService` and handles:
- Capacity listing for administrator/contributor access
- Capacity metadata retrieval

Key Methods:
```csharp
Task<CapacityResponse> ListCapacitiesAsync(string? continuationToken = null)
```

#### ValidationService
Implements `IValidationService` and provides:
- Centralized input validation
- Data annotation validation
- Required string and GUID validation

Key Methods:
```csharp
void ValidateAndThrow<T>(T obj, string parameterName)
IList<ValidationResult> Validate<T>(T obj)
void ValidateRequiredString(string value, string parameterName, int? maxLength = null)
void ValidateGuid(string value, string parameterName)
```

#### ArrowDataReaderService
Implements `IArrowDataReaderService` and handles:
- Apache Arrow stream parsing
- Query result summary generation
- Structured data extraction from Arrow format

Key Methods:
```csharp
Task<QueryResultSummary> ReadArrowStreamAsync(byte[] arrowData)
```

#### DataflowDefinitionProcessor
Implements `IDataflowDefinitionProcessor` and handles:
- Base64 decoding of dataflow definition parts
- QueryMetadata.json, mashup.pq, and .platform extraction
- Connection addition to dataflow definitions
- Query addition/update in dataflow definitions

Key Methods:
```csharp
DecodedDataflowDefinition DecodeDefinition(DataflowDefinition rawDefinition)
DataflowDefinition AddConnectionToDefinition(DataflowDefinition definition, Connection connection, string connectionId, string? clusterId)
DataflowDefinition AddOrUpdateQueryInDefinition(DataflowDefinition definition, string queryName, string mCode)
```

#### DataflowRefreshService
Implements `IDataflowRefreshService` and handles:
- Starting background dataflow refresh operations
- Tracking refresh progress and history
- Integration with `IBackgroundJobMonitor` for monitoring

Key Methods:
```csharp
Task<DataflowRefreshResult> StartRefreshAsync(McpSession session, string workspaceId, string dataflowId, ...)
Task<DataflowRefreshResult> GetStatusAsync(DataflowRefreshContext context)
IReadOnlyList<TrackedTask> GetAllTasks()
TrackedTask? GetTask(string taskId)
```

#### BackgroundJobMonitor
Implements `IBackgroundJobMonitor` and handles:
- Centralized management of background job lifecycle (start, track, poll, notify)
- Single timer-based polling loop for efficiency (3-second intervals)
- Job history tracking (active and completed tasks)
- Integration with notification queue for completion alerts

Key Methods:
```csharp
Task<BackgroundJobResult> StartJobAsync(IBackgroundJob job, McpSession session)
TrackedTask? GetTask(string taskId)
IReadOnlyList<TrackedTask> GetAllTasks()
bool HasActiveJobs { get; }
int ActiveJobCount { get; }
```

#### NotificationQueue
Implements `INotificationQueue` and handles:
- Queuing notifications for sequential delivery
- Spacing notifications (3-second delay) to prevent overlap
- Channel-based async producer/consumer pattern

Key Methods:
```csharp
void Enqueue(QueuedNotification notification)
int PendingCount { get; }
```

#### Platform Notification Providers
Platform-specific implementations of `IPlatformNotificationProvider` (registered in the stdio host only):
- **WindowsToastNotificationProvider**: Launches a WPF toast via PowerShell using embedded XAML/PS1 resources
- **MacOsNotificationProvider**: Shells out to `osascript` to display native macOS alerts
- **LinuxNotificationProvider**: Shells out to `notify-send` (libnotify) for desktop notifications

#### DataTransformationService
Implements `IDataTransformationService` and handles:
- JSON content parsing and transformation
- Data format conversions

#### GatewayClusterDatasourceService (DMTSv2)
Implements `IGatewayClusterDatasourceService` and handles:
- Power BI API v2.0 integration for gateway cluster datasources
- Located in `Services/DMTSv2/`

### 4. Abstractions Layer

**Location**: `Abstractions/`

Defines interfaces and base classes that enable testability and extensibility.

#### Interfaces (`Abstractions/Interfaces/`)
- `IAuthenticationService`: Authentication operations contract
- `IFabricGatewayService`: Gateway operations contract
- `IFabricConnectionService`: Connection operations contract
- `IFabricWorkspaceService`: Workspace operations contract
- `IFabricDataflowService`: Dataflow operations contract
- `IFabricCapacityService`: Capacity operations contract
- `IValidationService`: Input validation contract
- `IArrowDataReaderService`: Apache Arrow data parsing contract
- `IDataTransformationService`: Data transformation contract
- `IDataflowDefinitionProcessor`: Dataflow definition processing contract
- `IBackgroundJobMonitor`: Background job lifecycle management (start, track, poll, notify)
- `IBackgroundJob`: Interface for background jobs (dataflow refresh, etc.)
- `IDataflowRefreshService`: High-level dataflow refresh operations
- `IUserNotificationService`: Cross-platform user notification delivery
- `IPlatformNotificationProvider`: Platform-specific notification provider (Windows/macOS/Linux)
- `INotificationQueue`: Notification queuing and spacing
- `IMcpSessionAccessor`: MCP session access for background operations

#### DMTSv2 Interfaces (`Abstractions/Interfaces/DMTSv2/`)
- `IGatewayClusterDatasourceService`: Power BI gateway cluster datasource operations

#### Factories (`Abstractions/Factories/`)
- Connection creation factory abstractions

#### Base Classes
- `FabricServiceBase`: Common functionality for Fabric services including HTTP client access and logging

### 5. Models Layer

**Location**: `Models/`

Data Transfer Objects (DTOs) and configuration models organized by domain:

#### Authentication Models
- `AuthenticationResult`: Authentication status and user info
- `AzureAdConfiguration`: Azure AD configuration settings with default scopes
- `Messages`: Centralized message strings for consistent error/success messaging

#### Capacity Models (`Models/Capacity/`)
- `Capacity`: Fabric capacity information
- `CapacityResponses`: API response wrappers
- `CapacityState`: Capacity state enumeration

#### Gateway Models (`Models/Gateway/`)
- `Gateway`: Base gateway information
- `OnPremisesGateway`: On-premises gateway specific data
- `OnPremisesGatewayPersonal`: Personal gateway data
- `VirtualNetworkGateway`: Virtual network gateway data
- `CreateVNetGatewayRequest`: VNet gateway creation request
- `VirtualNetworkAzureResource`: Azure resource reference for VNet
- `GatewayResponse`: API response wrapper with pagination

#### Connection Models (`Models/Connection/`)
- `Connection`: Base connection information
- `ConnectionDetails`: Detailed connection configuration
- `CreateConnectionRequest`: Connection creation request
- `ConnectionResponse`: API response wrapper with pagination
- `Factories/FabricDataSourceConnectionFactory`: Factory for creating various connection types

#### Workspace Models (`Models/Workspace/`)
- `Workspace`: Workspace information and metadata
- `WorkspaceResponse`: API response wrapper with pagination

#### Dataflow Models (`Models/Dataflow/`)
- `Dataflow`: Dataflow information and properties
- `DataflowProperties`: Dataflow-specific metadata
- `CreateDataflowRequest` / `CreateDataflowResponse`: Dataflow creation DTOs
- `ListDataflowsResponse`: API response wrapper with pagination
- `ItemTag`: Tagging and categorization metadata

#### Dataflow Definition Models (`Models/Dataflow/Definition/`)
- `DataflowDefinition`: Raw dataflow definition structure
- `DataflowDefinitionPart`: Individual definition part (path + payload)
- `DecodedDataflowDefinition`: Human-readable decoded definition
- `GetDataflowDefinitionHttpResponse`: HTTP response wrapper
- `PayloadType`: Payload type enumeration
- `UpdateDataflowDefinitionRequest` / `UpdateDataflowDefinitionResponse`: Definition update DTOs

#### Dataflow Query Models (`Models/Dataflow/Query/`)
- `ExecuteDataflowQueryRequest`: Query execution request with M code
- `ExecuteDataflowQueryResponse`: Query execution response with Arrow data
- `QueryResultSummary`: Parsed query result summary with structured data

#### Common Models (`Models/Common/`)
- Shared base classes and common DTOs

### 6. Extensions Layer

**Location**: `Extensions/`

Extension methods providing formatting, serialization, and utility functions:

#### GatewayExtensions
- `ToFormattedInfo()`: Format gateway data for MCP response display
- Type-specific formatting for different gateway types

#### ConnectionExtensions
- `ToFormattedInfo()`: Format connection data for display
- `ToCreationSuccessResponse()`: Format successful creation responses

#### WorkspaceExtensions
- `ToFormattedInfo()`: Format workspace data for display

#### DataflowExtensions
- `ToFormattedInfo()`: Format dataflow data for display

#### CapacityExtensions
- `ToFormattedList()`: Format capacity list for display

#### ArrowDataExtensions
- `CreateArrowDataReport()`: Create formatted report from Arrow query results
- `ToQueryExecutionError()`: Format query execution errors

#### ResponseExtensions
- `ToAuthenticationError()`: Convert exceptions to auth error responses
- `ToValidationError()`: Convert validation errors to responses
- `ToHttpError()`: Convert HTTP errors to user-friendly responses
- `ToOperationError()`: Generic operation error formatting
- `ToNotFoundError()`: Format not-found responses

#### MQueryExtensions
- `WrapForDataflowQuery()`: Auto-wrap M expressions in section document format

#### JsonExtensions
- `ToMcpJson()`: Serialize objects to formatted JSON for MCP responses

#### HttpResponseMessageExtensions
- `ReadAsJsonOrDefaultAsync<T>()`: Deserialize HTTP responses with fallback

### 7. Infrastructure Layer

**Location**: `Infrastructure/`

Cross-cutting infrastructure concerns and HTTP pipeline components.

#### HTTP Pipeline (`Infrastructure/Http/`)

##### FabricAuthenticationHandler
A `DelegatingHandler` that automatically adds Bearer token authentication to outgoing HTTP requests:
- Retrieves tokens from `IAuthenticationService`
- Validates tokens before attaching
- Used by Fabric API and Power BI API clients

##### FabricUrlBuilder
Fluent URL builder for consistent API endpoint construction:
- `ForFabricApi()`: Create builder with Fabric API base URL
- `ForPowerBiV2Api()`: Create builder with Power BI API base URL
- `WithPath()`: Add URL-encoded path segments
- `WithLiteralPath()`: Add literal path segments
- `WithQueryParam()`: Add query parameters
- `WithApiVersion()`: Add API version parameter
- `Build()`: Construct final URL string

##### TokenValidator
Utility for validating access tokens before use in requests.

### 8. Configuration Layer

**Location**: `Configuration/`

Centralized configuration constants and settings:

#### ApiVersions
Centralized API version management:
```csharp
public static class ApiVersions
{
    public static class Fabric
    {
        public const string V1 = "v1";
        public const string V1BaseUrl = "https://api.fabric.microsoft.com/v1";
    }

    public static class PowerBi
    {
        public const string V2 = "v2.0";
        public const string V2BaseUrl = "https://api.powerbi.com/v2.0";
    }
}
```

#### HttpClientNames
Named HTTP client constants:
- `FabricApi`: Client for Microsoft Fabric API
- `PowerBiV2Api`: Client for Power BI API v2.0

#### FeatureFlags
Feature flag constants for conditional tool registration:
- `DataflowQuery`: Enable/disable DataflowQueryTool (`--dataflow-query`)

#### FeatureFlagRegistration
Extension methods for registering tools based on feature flags:
```csharp
mcpBuilder.RegisterToolWithFeatureFlag<DataflowQueryTool>(
    configuration, args, FeatureFlags.DataflowQuery, nameof(DataflowQueryTool), logger);
```

#### JsonSerializerOptionsProvider
Centralized JSON serialization options:
- `CaseInsensitive`: Options for case-insensitive deserialization
- `Indented`: Options for formatted JSON output

## Data Flow

### Authentication Flow

```
1. AI Assistant → AuthenticationTool.AuthenticateInteractiveAsync()
2. AuthenticationTool → AuthenticationService
3. AuthenticationService → Azure AD (via MSAL PublicClientApplication)
4. Azure AD → Returns tokens with user claims
5. AuthenticationService → Stores tokens in memory
6. AuthenticationService → Returns success message with user info
7. AuthenticationTool → AI Assistant (formatted response)
```

### Authenticated API Request Flow

```
1. AI Assistant → MCP Tool (e.g., GatewayTool.ListGatewaysAsync())
2. Tool → Service (e.g., FabricGatewayService)
3. Service → HttpClientFactory.CreateClient("FabricApi")
4. HttpClient sends request through pipeline:
   └── FabricAuthenticationHandler intercepts request
       ├── Retrieves token from IAuthenticationService
       ├── TokenValidator validates token
       └── Adds "Authorization: Bearer <token>" header
5. Request → Microsoft Fabric API
6. Fabric API → Returns JSON response
7. Service → Parses response using JsonSerializerOptionsProvider
8. Service → Tool (domain objects)
9. Tool → Formats using extension methods
10. Tool → AI Assistant (JSON via ToMcpJson())
```

### Dataflow Query Execution Flow

```
1. AI Assistant → DataflowQueryTool.ExecuteQueryAsync()
2. Tool → MQueryExtensions.WrapForDataflowQuery() (auto-wrap M code)
3. Tool → FabricDataflowService.ExecuteQueryAsync()
4. Service → POST to Fabric API with ExecuteDataflowQueryRequest
5. Fabric API → Returns Apache Arrow binary data
6. Service → ArrowDataReaderService.ReadArrowStreamAsync()
7. ArrowDataReaderService → Parses Arrow stream, extracts rows/columns
8. Service → Returns QueryResultSummary
9. Tool → ArrowDataExtensions.CreateArrowDataReport()
10. Tool → AI Assistant (formatted table data)
```

### Error Flow

```
1. Service encounters error (HTTP, validation, auth, etc.)
2. Error bubbles up to Tool layer
3. Tool catches specific exception types:
   ├── UnauthorizedAccessException → ToAuthenticationError()
   ├── ArgumentException → ToValidationError()
   ├── HttpRequestException → ToHttpError()
   └── Exception → ToOperationError()
4. Extension method creates user-friendly error object
5. Tool → ToMcpJson() → AI Assistant
```

### Background Job Flow (Dataflow Refresh)

```
1. AI Assistant → DataflowTool.RefreshDataflowBackgroundAsync()
2. Tool → DataflowRefreshService.StartRefreshAsync()
3. Service → Creates DataflowRefreshJob (implements IBackgroundJob)
4. Service → BackgroundJobMonitor.StartJobAsync()
5. Monitor:
   ├── Stores MCP session via IMcpSessionAccessor
   ├── Calls job.StartAsync() → POST to Fabric API
   ├── Tracks task in internal dictionary
   ├── Starts/continues single Timer (3-second interval)
   └── Returns initial result to Tool → AI Assistant
6. Background Monitoring (single timer polls all jobs):
   ├── Timer fires every 3 seconds
   ├── Polls all active jobs in parallel (Task.WhenAll)
   ├── For each completed job:
   │   ├── Updates tracked task status
   │   └── Enqueues notification to NotificationQueue
   └── Stops timer when no active jobs remain
7. Notification Delivery:
   ├── NotificationQueue processor (single background task)
   ├── Shows notifications sequentially with 3-second spacing
   └── Platform-native delivery (Windows toast / macOS alert / Linux notify-send)
```

#### Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                    Background Task System                       │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌─────────────────┐      ┌─────────────────────────────────┐  │
│  │DataflowRefresh  │─────▶│     BackgroundJobMonitor        │  │
│  │    Service      │      │  ┌─────────────────────────┐    │  │
│  └─────────────────┘      │  │ Single Timer (3s poll)  │    │  │
│          │                │  └───────────┬─────────────┘    │  │
│          │                │              │                   │  │
│          ▼                │  ┌───────────▼─────────────┐    │  │
│  ┌─────────────────┐      │  │  Active Jobs Dict       │    │  │
│  │DataflowRefresh  │      │  │  ┌─────┐ ┌─────┐       │    │  │
│  │     Job         │◀────▶│  │  │Job 1│ │Job 2│ ...   │    │  │
│  └─────────────────┘      │  │  └─────┘ └─────┘       │    │  │
│                           │  └───────────┬─────────────┘    │  │
│                           │              │                   │  │
│                           │  ┌───────────▼─────────────┐    │  │
│                           │  │  Task History Dict      │    │  │
│                           │  └─────────────────────────┘    │  │
│                           └───────────────┬─────────────────┘  │
│                                           │                     │
│                           ┌───────────────▼─────────────────┐  │
│                           │       NotificationQueue         │  │
│                           │  ┌─────────────────────────┐    │  │
│                           │  │ Channel<Notification>   │    │  │
│                           │  │ (3s spacing)            │    │  │
│                           │  └───────────┬─────────────┘    │  │
│                           └───────────────┼─────────────────┘  │
│                                           │                     │
│  ┌────────────────────────────────────────┼────────────────┐   │
│  │         IUserNotificationService       │                │   │
│  │  ┌─────────────────────────────────────▼──────────────┐ │   │
│  │  │          SystemToastNotificationService            │ │   │
│  │  │       ┌──────────────────────────────┐              │ │   │
│  │  │       │  Platform Notification        │              │ │   │
│  │  │       │  Providers (Win/Mac/Linux)     │              │ │   │
│  │  │       └──────────────────────────────┘              │ │   │
│  │  └────────────────────────────────────────────────────┘ │   │
│  └─────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

## Security Architecture

### Authentication Security

- **Token Storage**: In-memory storage with automatic expiration via MSAL
- **Credential Protection**: Never log or expose secrets; validation service checks inputs
- **Secure Communication**: HTTPS only for all external API calls
- **Token Refresh**: Automatic token refresh via MSAL when possible
- **Delegating Handlers**: Authentication logic centralized in HTTP pipeline handlers

### HTTP Pipeline Security

- **FabricAuthenticationHandler**: Validates and attaches tokens for Fabric/Power BI APIs
- **TokenValidator**: Ensures tokens are valid before making requests
- **Centralized Base URLs**: API endpoints defined in `ApiVersions` class to prevent URL manipulation

### API Security

- **Input Validation**: All user inputs validated via `IValidationService`
- **Authorization**: Token-based access control with proper scopes
- **Rate Limiting**: Respect API rate limits through HttpClient timeout configuration
- **Error Sanitization**: No sensitive data in error messages; user-friendly error responses

### Configuration Security

- **Environment Variables**: Secrets stored in environment variables
- **No Hardcoded Secrets**: All credentials externally configured
- **Principle of Least Privilege**: Minimal required API permissions
- **Feature Flags**: Experimental features disabled by default

## Extension Points

### Adding New Tools

1. Create tool class with MCP attributes:
```csharp
[McpServerToolType]
public class NewTool
{
    private readonly INewService _service;
    private readonly IValidationService _validationService;

    public NewTool(INewService service, IValidationService validationService)
    {
        _service = service;
        _validationService = validationService;
    }

    [McpServerTool, Description("Description of the tool")]
    public async Task<string> NewOperationAsync(
        [Description("Parameter description")] string parameter)
    {
        try
        {
            _validationService.ValidateRequiredString(parameter, nameof(parameter));
            var result = await _service.DoOperationAsync(parameter);
            return result.ToMcpJson();
        }
        catch (ArgumentException ex)
        {
            return ex.ToValidationError().ToMcpJson();
        }
        catch (Exception ex)
        {
            return ex.ToOperationError("performing operation").ToMcpJson();
        }
    }
}
```

2. Register in `Program.cs`:
```csharp
// Always enabled
mcpBuilder.WithTools<NewTool>();

// Or conditionally with feature flag
mcpBuilder.RegisterToolWithFeatureFlag<NewTool>(
    configuration, args, "new-feature-flag", nameof(NewTool), logger);
```

### Adding New Services

1. Define interface in `Abstractions/Interfaces/`:
```csharp
public interface INewService
{
    Task<NewResult> PerformOperationAsync(string input);
}
```

2. Implement service in `Services/`:
```csharp
public class NewService : INewService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NewService> _logger;

    public NewService(IHttpClientFactory httpClientFactory, ILogger<NewService> logger)
    {
        _httpClient = httpClientFactory.CreateClient(HttpClientNames.FabricApi);
        _logger = logger;
    }

    public async Task<NewResult> PerformOperationAsync(string input)
    {
        var url = FabricUrlBuilder.ForFabricApi()
            .WithLiteralPath("newEndpoint")
            .WithPath(input)
            .Build();

        var response = await _httpClient.GetAsync(url);
        return await response.ReadAsJsonOrDefaultAsync(new NewResult(), JsonSerializerOptionsProvider.CaseInsensitive);
    }
}
```

3. Register service in `Program.cs`:
```csharp
builder.Services.AddSingleton<INewService, NewService>();
```

### Adding New API Endpoints

1. Add API version to `Configuration/ApiVersions.cs` if needed
2. Create URL using `FabricUrlBuilder`:
```csharp
var url = FabricUrlBuilder.ForFabricApi()
    .WithLiteralPath("workspaces")
    .WithPath(workspaceId)
    .WithLiteralPath("newResource")
    .WithQueryParam("api-version", ApiVersions.Fabric.V1)
    .Build();
```

### Adding New HTTP Clients

1. Add client name to `Configuration/HttpClientNames.cs`:
```csharp
public const string NewApiClient = "NewApiClient";
```

2. Register in `Program.cs` with appropriate handler:
```csharp
builder.Services.AddHttpClient(HttpClientNames.NewApiClient, client =>
{
    client.BaseAddress = new Uri("https://api.example.com/");
    client.Timeout = TimeSpan.FromSeconds(30);
}).AddHttpMessageHandler<FabricAuthenticationHandler>();
```

### Adding Feature Flags

1. Add constant to `Configuration/FeatureFlags.cs`:
```csharp
public const string NewFeature = "new-feature";
```

2. Use in `Program.cs`:
```csharp
mcpBuilder.RegisterToolWithFeatureFlag<NewFeatureTool>(
    configuration, args, FeatureFlags.NewFeature, nameof(NewFeatureTool), logger);
```

3. Enable via command line: `--new-feature`

## Design Patterns

### Dependency Injection
- Constructor injection for all dependencies
- Interface-based design for testability
- Singleton lifetime for services (shared state, HTTP clients)
- Transient lifetime for delegating handlers (required by HttpClient pipeline)

### Delegating Handler Pattern
- Authentication handlers (`FabricAuthenticationHandler`)
- Centralized cross-cutting concerns in HTTP pipeline
- Separation of authentication from business logic

### Factory Pattern
- `FabricDataSourceConnectionFactory` for creating various connection types
- `IHttpClientFactory` for creating named HTTP clients with proper configuration
- Encapsulates complex object creation logic

### Builder Pattern
- `FabricUrlBuilder` for constructing API URLs with fluent interface
- Method chaining for readable URL construction
- Handles encoding and query parameter formatting

### Repository Pattern (Implicit)
- Services act as repositories for external data
- Abstracted data access through interfaces
- Consistent data retrieval and formatting

### Extension Method Pattern
- Response formatting (`ToFormattedInfo()`, `ToMcpJson()`)
- Error handling (`ToAuthenticationError()`, `ToValidationError()`)
- Adds functionality without modifying core classes

### Options Pattern
- `AzureAdConfiguration` for Azure AD settings
- Configuration binding from appsettings.json or environment

### Strategy Pattern (via Feature Flags)
- `FeatureFlagRegistration` for conditional tool registration
- Runtime selection of available features
## Performance Considerations

### HTTP Client Management
- **Named HTTP Clients**: Use `IHttpClientFactory` with named clients to avoid socket exhaustion
- **Connection Pooling**: HTTP clients are configured with proper lifetimes for connection reuse
- **Timeout Configuration**: 30-second default timeouts prevent hanging requests
- **Base Address Caching**: Base URLs set once per client, not per request

### Token Management
- **In-Memory Caching**: MSAL caches tokens automatically
- **Lazy Token Acquisition**: Tokens acquired only when needed via delegating handlers
- **Automatic Refresh**: MSAL handles token refresh before expiration

### Data Processing
- **Streaming Arrow Parsing**: `ArrowDataReaderService` processes Arrow streams incrementally
- **Efficient JSON Serialization**: Centralized `JsonSerializerOptionsProvider` with pre-configured options
- **Pagination Support**: All list operations support continuation tokens to handle large datasets

### Service Lifetime
- **Singleton Services**: Core services registered as singletons to share state and HTTP clients
- **Transient Handlers**: Delegating handlers registered as transient per HttpClientFactory requirements

### Memory Management
- **Dispose Patterns**: Proper disposal of streams and readers in Arrow processing
- **Using Statements**: All disposable resources properly scoped

## Future Enhancements

### Planned Features
- **Caching Layer**: Add response caching for frequently accessed data
- **Retry Policies**: Implement Polly-based retry policies for transient failures
- **Batch Operations**: Support for batch operations on dataflows and connections
- **Webhook Integration**: Real-time notifications for resource changes
- **Additional Data Sources**: Expand connection factory to support more data source types

### Infrastructure Improvements
- **Health Checks**: Add health check endpoints for monitoring
- **Metrics/Telemetry**: OpenTelemetry integration for observability
- **Rate Limiting**: Proactive rate limit handling with backoff strategies
- **Circuit Breaker**: Prevent cascade failures with circuit breaker pattern

### Tool Enhancements
- ~~**Dataflow Refresh**: Trigger and monitor dataflow refresh operations~~ ✅ Implemented
- **Schema Discovery**: Introspect dataflow query schemas
- **Incremental Refresh**: Support for incremental data refresh policies
- **Lineage Tracking**: Data lineage and dependency visualization

### Testing Improvements
- **Integration Test Suite**: Comprehensive integration tests with Fabric API
- **Mock Service Layer**: Improved testability with mock implementations
- **Performance Benchmarks**: Automated performance regression testing