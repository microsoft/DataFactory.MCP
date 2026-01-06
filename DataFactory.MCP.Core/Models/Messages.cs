namespace DataFactory.MCP.Models;

/// <summary>
/// Common messages used throughout the application
/// </summary>
public static class Messages
{
    #region Authentication Messages
    /// <summary>
    /// Authentication error message when no valid token is available
    /// </summary>
    public const string AuthenticationRequired = "Valid authentication token is required. Please authenticate first.";

    /// <summary>
    /// Authentication error message for invalid token format
    /// </summary>
    public const string InvalidTokenFormat = "Invalid access token format.";

    /// <summary>
    /// Authentication error message when no authentication is found
    /// </summary>
    public const string NoAuthenticationFound = "No valid authentication found. Please authenticate first.";

    /// <summary>
    /// Message when user is not authenticated
    /// </summary>
    public const string NotAuthenticated = "Not authenticated. Please authenticate using interactive login or service principal.";

    /// <summary>
    /// Message when no active authentication session is found
    /// </summary>
    public const string NoActiveAuthenticationSession = "No active authentication session found.";

    /// <summary>
    /// Message when public client application is not initialized
    /// </summary>
    public const string PublicClientNotInitialized = "Public client application not initialized. Check Azure AD configuration.";

    /// <summary>
    /// Message when access token is not available
    /// </summary>
    public const string TokenNotAvailable = "Token not available";

    /// <summary>
    /// Message when access token has expired
    /// </summary>
    public const string AccessTokenExpired = "Access token has expired. Please re-authenticate.";

    /// <summary>
    /// Message when access token has expired and cannot be refreshed silently
    /// </summary>
    public const string AccessTokenExpiredCannotRefresh = "Access token has expired and cannot be refreshed silently. Please re-authenticate.";
    #endregion

    #region Success Messages
    /// <summary>
    /// Success message template for interactive authentication
    /// </summary>
    public const string InteractiveAuthenticationSuccessTemplate = "Interactive authentication completed successfully. User: {0}";

    /// <summary>
    /// Success message template for service principal authentication
    /// </summary>
    public const string ServicePrincipalAuthenticationSuccessTemplate = "Service principal authentication completed successfully for application: {0}";

    /// <summary>
    /// Success message template for signing out
    /// </summary>
    public const string SignOutSuccessTemplate = "Successfully signed out user: {0}";
    #endregion

    #region Error Message Templates
    /// <summary>
    /// Generic authentication error message template
    /// </summary>
    public const string AuthenticationErrorTemplate = "Authentication error: {0}";

    /// <summary>
    /// Authentication failed message template
    /// </summary>
    public const string AuthenticationFailedTemplate = "Authentication failed: {0}";

    /// <summary>
    /// Service principal authentication failed message template
    /// </summary>
    public const string ServicePrincipalAuthenticationFailedTemplate = "Service principal authentication failed: {0}";

    /// <summary>
    /// Sign out error message template
    /// </summary>
    public const string SignOutErrorTemplate = "Sign out error: {0}";

    /// <summary>
    /// Error retrieving authentication status message template
    /// </summary>
    public const string ErrorRetrievingAuthStatusTemplate = "Error retrieving authentication status: {0}";

    /// <summary>
    /// Error retrieving access token message template
    /// </summary>
    public const string ErrorRetrievingAccessTokenTemplate = "Error retrieving access token: {0}";

    /// <summary>
    /// API request failed message template
    /// </summary>
    public const string ApiRequestFailedTemplate = "API request failed: {0}";
    #endregion

    #region Validation Messages
    public static string InvalidParameterEmpty(string parameterName) => $"{parameterName} is required and cannot be empty";
    #endregion

    #region Not Found Messages
    /// <summary>
    /// Message when no connections are found
    /// </summary>
    public const string NoConnectionsFound = "No connections found. Response has an empty list.";

    /// <summary>
    /// Message when no gateways are found
    /// </summary>
    public const string NoGatewaysFound = "No gateways found. Make sure you have the required permissions (Gateway.Read.All or Gateway.ReadWrite.All).";

    /// <summary>
    /// Message when no workspaces are found
    /// </summary>
    public const string NoWorkspacesFound = "No workspaces found. Make sure you have the required permissions (Workspace.Read.All or Workspace.ReadWrite.All).";
    #endregion

    #region Service Messages
    /// <summary>
    /// Message when service provider is not initialized
    /// </summary>
    public const string ServiceProviderNotInitialized = "Service provider not initialized";

    /// <summary>
    /// Message when authentication service is not available
    /// </summary>
    public const string AuthServiceNotAvailable = "Error: Authentication service not available. Please ensure the server is properly initialized.";
    #endregion

    #region Log Messages
    /// <summary>
    /// Log message when starting interactive authentication
    /// </summary>
    public const string StartingInteractiveAuthentication = "Starting interactive authentication...";

    /// <summary>
    /// Log message when Azure AD client applications are initialized successfully
    /// </summary>
    public const string AzureAdClientInitializedSuccessfully = "Azure AD client applications initialized successfully";

    /// <summary>
    /// Log message when signing out current user
    /// </summary>
    public const string SigningOutCurrentUser = "Signing out current user...";
    #endregion
}