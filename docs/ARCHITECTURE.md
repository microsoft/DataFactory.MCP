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
│  │ ┌─────────────┐ │  │ ┌─────────────┐ │  │ ┌─────────────────────────┐ │ │
│  │ │ConnectionTool│ │  │ │ConnectionSvc│ │  │ │ IFabricConnectionService │ │ │
│  │ └─────────────┘ │  │ └─────────────┘ │  │ └─────────────────────────┘ │ │
│  │ ┌─────────────┐ │  │ ┌─────────────┐ │  │ ┌─────────────────────────┐ │ │
│  │ │WorkspaceTool│ │  │ │WorkspaceSvc │ │  │ │ IFabricWorkspaceService │ │ │
│  │ └─────────────┘ │  │ └─────────────┘ │  │ └─────────────────────────┘ │ │
│  │ ┌─────────────┐ │  │ ┌─────────────┐ │  │ ┌─────────────────────────┐ │ │
│  │ │ DataflowTool│ │  │ │DataflowSvc  │ │  │ │ IFabricDataflowService  │ │ │
│  │ └─────────────┘ │  │ └─────────────┘ │  │ └─────────────────────────┘ │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────────────────┘ │
│                          │                          │                      │
│                          ▼                          ▼                      │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────────────┐ │
│  │     Models      │  │   Extensions    │  │         Utilities          │ │
│  │                 │  │                 │  │                             │ │
│  │ • Gateway       │  │ • Gateway       │  │ • Logging                   │ │
│  │ • Connection    │  │   Extensions    │  │ • Configuration             │ │
│  │ • Workspace     │  │ • Connection    │  │ • Error Handling            │ │
│  │ • Dataflow      │  │   Extensions    │  │ • Validation                │ │
│  │ • Auth Result   │  │ • Workspace     │  │ • HTTP Client Factory       │ │
│  │ • Azure Config  │  │   Extensions    │  │ • JSON Serialization        │ │
│  └─────────────────┘  │ • Dataflow      │  └─────────────────────────────┘ │
│                       │   Extensions    │                                  │
│                       │ • JSON          │                                  │
│                       │   Converters    │                                  │
│                       └─────────────────┘                                  │
└─────────────────────────┬───────────────────────────────────────────────────┘
                          │ HTTPS / Microsoft Graph API & Fabric API
                          ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                          Microsoft Azure                                   │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────────────┐ │
│  │   Azure AD      │  │ Microsoft Graph │  │   Microsoft Fabric          │ │
│  │ Authentication  │  │      API        │  │  (Workspaces & Dataflows)   │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────────────────┘ │
│                       │  • Gateways     │  │  • Data Factory            │ │
│                       │  • Connections  │  │    Gateways                │ │
│                       │  • Workspaces   │  │                             │ │
│                       └─────────────────┘  └─────────────────────────────┘ │
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
    .WithTools<GatewayTool>()
    .WithTools<ConnectionsTool>()
    .WithTools<WorkspacesTool>()
    .WithTools<DataflowTool>();

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
- `CreateVNetGatewayAsync()`: Create a new virtual network gateway

#### ConnectionsTool
- `ListConnectionsAsync()`: List accessible connections
- `GetConnectionAsync()`: Get connection details by ID

#### WorkspacesTool
- `ListWorkspacesAsync()`: List accessible workspaces

#### DataflowTool
- `ListDataflowsAsync()`: List dataflows in a workspace
- `CreateDataflowAsync()`: Create a new dataflow in a workspace

#### CapacityTool
- `ListCapacitiesAsync()`: List accessible capacities

#### AzureResourceDiscoveryTool
- `GetAzureSubscriptionsAsync()`: List Azure subscriptions
- `GetAzureResourceGroupsAsync()`: List resource groups in a subscription
- `GetAzureVirtualNetworksAsync()`: List virtual networks in a subscription
- `GetAzureSubnetsAsync()`: List subnets in a virtual network

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

#### FabricConnectionService
Implements `IFabricConnectionService` and handles:
- Connection data retrieval
- Microsoft Graph API integration
- Connection type classification
- Pagination support

Key Methods:
```csharp
public async Task<ConnectionResponse> ListConnectionsAsync(string? continuationToken = null)
public async Task<Connection> GetConnectionAsync(string connectionId)
```

#### FabricWorkspaceService
Implements `IFabricWorkspaceService` and handles:
- Workspace data retrieval
- User permission filtering
- Role-based access control
- Workspace metadata management

Key Methods:
```csharp
public async Task<WorkspaceResponse> ListWorkspacesAsync(string? continuationToken = null, string? roles = null, bool? preferWorkspaceSpecificEndpoints = null)
```

#### FabricDataflowService
Implements `IFabricDataflowService` and handles:
- Dataflow data retrieval from Fabric workspaces
- Microsoft Fabric Dataflows API integration
- Workspace-scoped dataflow listing
- Pagination and error handling

Key Methods:
```csharp
public async Task<ListDataflowsResponse> ListDataflowsAsync(string workspaceId, string? continuationToken = null)
```

### 4. Abstractions Layer

**Location**: `Abstractions/`

Defines interfaces and base classes that enable testability and extensibility.

#### Interfaces
- `IAuthenticationService`: Authentication operations contract
- `IFabricGatewayService`: Gateway operations contract
- `IFabricConnectionService`: Connection operations contract
- `IFabricWorkspaceService`: Workspace operations contract
- `IFabricDataflowService`: Dataflow operations contract

#### Base Classes
- `FabricServiceBase`: Common functionality for Fabric services

### 5. Models Layer

**Location**: `Models/`

Data Transfer Objects (DTOs) and configuration models with built-in validation:

#### Authentication Models
- `AuthenticationResult`: Authentication status and user info
- `AzureAdConfiguration`: Azure AD configuration settings

#### Gateway Models
- `Gateway`: Base gateway information
- `OnPremisesGateway`: On-premises gateway specific data
- `OnPremisesGatewayPersonal`: Personal gateway data
- `VirtualNetworkGateway`: Virtual network gateway data
- `CreateVNetGatewayRequest`: Request model with validation attributes
- `VirtualNetworkAzureResource`: Azure resource configuration with validation
- `GatewayResponse`: API response wrapper with pagination

#### Connection Models
- `Connection`: Base connection information
- `ConnectionDetails`: Detailed connection configuration
- `ConnectionResponse`: API response wrapper with pagination

#### Workspace Models
- `Workspace`: Workspace information and metadata
- `WorkspaceResponse`: API response wrapper with pagination

#### Dataflow Models
- `Dataflow`: Dataflow information and properties
- `DataflowProperties`: Dataflow-specific metadata
- `CreateDataflowRequest`: Request model with validation attributes
- `ListDataflowsResponse`: API response wrapper with pagination
- `ItemTag`: Tagging and categorization metadata

#### Capacity Models
- `Capacity`: Capacity information and metadata
- `CapacityResponse`: API response wrapper with pagination

#### Azure Resource Models
- `AzureSubscription`: Azure subscription information
- `AzureResourceGroup`: Resource group information
- `AzureVirtualNetwork`: Virtual network information
- `AzureSubnet`: Subnet information

#### Validation Attributes
Models use Data Annotations for validation:
- `[Required]`: Required fields
- `[StringLength]`: String length constraints
- `[RegularExpression]`: Pattern validation (GUIDs, etc.)
- `[Range]`: Numeric range validation
- Custom attributes for business rules (e.g., `[InactivityValidation]`)

### 6. Validation Architecture

**Location**: `Services/ValidationService.cs`, Model Attributes

The validation system implements a multi-layered approach with model-based validation as the primary mechanism.

#### Validation Service
The `ValidationService` implements `IValidationService` and provides centralized validation capabilities:

```csharp
public interface IValidationService
{
    void ValidateAndThrow<T>(T obj, string parameterName) where T : class;
    IList<ValidationResult> Validate<T>(T obj) where T : class;
    void ValidateRequiredString(string value, string parameterName, int? maxLength = null);
    void ValidateGuid(string value, string parameterName);
}
```

#### Base Service Integration
All Fabric services inherit from `FabricServiceBase`, which includes `ValidationService` as a protected property:

```csharp
public abstract class FabricServiceBase : IDisposable
{
    protected readonly ILogger Logger;
    protected readonly IAuthenticationService AuthService;
    protected readonly IValidationService ValidationService; // Available to all services
    
    protected FabricServiceBase(
        ILogger logger,
        IAuthenticationService authService,
        IValidationService validationService)
    {
        Logger = logger;
        AuthService = authService;
        ValidationService = validationService;
    }
}
```

#### Model-Based Validation
Primary validation is performed using Data Annotations on model properties:

```csharp
public class CreateVNetGatewayRequest
{
    [Required(ErrorMessage = "Display name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Display name must be between 1 and 100 characters")]
    public string DisplayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Capacity ID is required")]
    [RegularExpression(@"^[{(]?[0-9A-Fa-f]{8}[-]?([0-9A-Fa-f]{4}[-]?){3}[0-9A-Fa-f]{12}[)}]?$", 
        ErrorMessage = "Capacity ID must be a valid GUID")]
    public string CapacityId { get; set; } = string.Empty;

    [InactivityValidation] // Custom validation attribute
    public int InactivityMinutesBeforeSleep { get; set; } = 120;
}
```

#### Custom Validation Attributes
For complex business rules, custom validation attributes are implemented:

```csharp
public class InactivityValidationAttribute : ValidationAttribute
{
    private static readonly int[] ValidValues = { 30, 60, 90, 120, 150, 240, 360, 480, 720, 1440 };

    public override bool IsValid(object? value)
    {
        if (value is int intValue)
        {
            return ValidValues.Contains(intValue);
        }
        return false;
    }

    public override string FormatErrorMessage(string name)
    {
        return $"{name} must be one of: {string.Join(", ", ValidValues)}";
    }
}
```

#### Validation Flow
```
┌─────────────────┐
│     Tools       │ ← Handle ArgumentException from validation
├─────────────────┤
│   Services      │ ← Call ValidationService.ValidateAndThrow(model)
├─────────────────┤
│ ValidationService│ ← Uses Data Annotations to validate models
├─────────────────┤
│    Models       │ ← Define validation rules with attributes
└─────────────────┘
```

#### Service Implementation Example
```csharp
public async Task<CreateVNetGatewayResponse> CreateVNetGatewayAsync(CreateVNetGatewayRequest request)
{
    try
    {
        // Single validation call handles all model validation rules
        ValidationService.ValidateAndThrow(request, nameof(request));
        
        // Business logic continues...
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "Error creating VNet gateway");
        throw;
    }
}
```

#### Tool Exception Handling
Tools catch validation exceptions and return user-friendly error messages:

```csharp
try
{
    var response = await _gatewayService.CreateVNetGatewayAsync(request);
    return SerializeResponse(response);
}
catch (ArgumentException ex)
{
    return $"Invalid parameter: {ex.Message}";
}
```

### 7. Extensions Layer

**Location**: `Extensions/`

Extension methods and utility functions:

#### GatewayExtensions
- `ToFormattedInfo()`: Format gateway data for display
- Type-specific formatting methods

#### ConnectionExtensions
- `ToFormattedInfo()`: Format connection data for display

#### WorkspaceExtensions
- `ToFormattedInfo()`: Format workspace data for display

#### DataflowExtensions
- `ToFormattedInfo()`: Format dataflow data for display

#### CapacityExtensions
- `ToFormattedList()`: Format capacity data for display

#### JSON Converters
- `GatewayJsonConverter`: Handle polymorphic gateway deserialization
- `ConnectionJsonConverter`: Handle polymorphic connection deserialization

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
4. FabricGatewayService → Validates request model using ValidationService
5. FabricGatewayService → Microsoft Graph API
6. Microsoft Graph API → Returns gateway data
7. FabricGatewayService → Processes and formats data
8. FabricGatewayService → GatewayTool
9. GatewayTool → AI Assistant (formatted response)
```

### Dataflow Operations Flow

```
1. AI Assistant → DataflowTool
2. DataflowTool → Validates authentication
3. DataflowTool → FabricDataflowService
4. FabricDataflowService → Validates request model using ValidationService
5. FabricDataflowService → Microsoft Fabric API
6. Microsoft Fabric API → Returns dataflow data
7. FabricDataflowService → Processes and formats data
8. FabricDataflowService → DataflowTool
9. DataflowTool → AI Assistant (formatted response)
```

### Validation Flow

```
1. Tool → Creates request model with user parameters
2. Tool → Service.MethodAsync(model)
3. Service → ValidationService.ValidateAndThrow(model)
4. ValidationService → Validates using Data Annotations on model
5. ValidationService → Throws ArgumentException if invalid
6. Tool → Catches ArgumentException
7. Tool → Returns user-friendly error message
```

### Error Flow

```
1. Service encounters error (validation, API, business logic)
2. Service logs error details
3. Service throws appropriate exception (ArgumentException for validation)
4. Tool catches specific exception types
5. Tool transforms to user-friendly message
6. Tool returns formatted error to AI Assistant
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

### Validation Pattern
- **Model-Based Validation**: Primary validation using Data Annotations
- **Centralized Validation Service**: Single service handles all validation logic
- **Fail-Fast Validation**: Validate inputs early in the request pipeline
- **Custom Validation Attributes**: Business-specific validation rules
- **Layered Error Handling**: Services throw exceptions, tools format user messages

### Base Class Pattern
- `FabricServiceBase`: Common functionality for all Fabric services
- Shared dependencies (Logger, AuthService, ValidationService)
- Consistent HTTP client configuration and JSON serialization

### Extension Method Pattern
- Type-specific formatting methods
- Consistent data presentation across tools
- Separation of display logic from business logic
