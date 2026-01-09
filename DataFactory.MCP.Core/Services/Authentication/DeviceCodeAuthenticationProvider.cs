using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Configuration;
using DataFactory.MCP.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;

namespace DataFactory.MCP.Services.Authentication;

/// <summary>
/// Handles device code authentication flow
/// </summary>
public class DeviceCodeAuthenticationProvider : IAuthenticationProvider
{
    private readonly ILogger<DeviceCodeAuthenticationProvider> _logger;
    private readonly IPublicClientApplication _publicClientApp;
    private Task<Microsoft.Identity.Client.AuthenticationResult>? _pendingDeviceAuth;
    private string? _pendingDeviceInstructions;
    private DateTime? _deviceAuthStartTime;
    private readonly TimeSpan _deviceAuthTimeout = TimeSpan.FromMinutes(15); // Match Azure AD default

    public DeviceCodeAuthenticationProvider(ILogger<DeviceCodeAuthenticationProvider> logger)
    {
        _logger = logger;
        _publicClientApp = CreatePublicClientApp();
    }

    public string ProviderType => "DeviceCode";

    public bool CanHandle(AuthenticationRequest request)
    {
        return request is StartDeviceCodeAuthenticationRequest or CheckDeviceCodeAuthenticationRequest;
    }

    public async Task<Abstractions.Interfaces.AuthenticationResult> AuthenticateAsync(AuthenticationRequest request)
    {
        return request switch
        {
            StartDeviceCodeAuthenticationRequest => await StartDeviceCodeAuthAsync(request),
            CheckDeviceCodeAuthenticationRequest => await CheckDeviceCodeStatusAsync(request),
            _ => Abstractions.Interfaces.AuthenticationResult.Failure("Invalid request type for Device Code authentication")
        };
    }

    private async Task<Abstractions.Interfaces.AuthenticationResult> StartDeviceCodeAuthAsync(AuthenticationRequest request)
    {
        try
        {
            if (_pendingDeviceAuth != null && !_pendingDeviceAuth.IsCompleted)
            {
                var timeRemaining = _deviceAuthStartTime.HasValue
                    ? _deviceAuthTimeout - (DateTime.UtcNow - _deviceAuthStartTime.Value)
                    : TimeSpan.Zero;

                if (timeRemaining > TimeSpan.Zero)
                {
                    var message = $"Device authentication already in progress.\n\n{_pendingDeviceInstructions}\n\n⏱️ Time remaining: {timeRemaining.Minutes} minutes";
                    return Abstractions.Interfaces.AuthenticationResult.Failure(message);
                }
                else
                {
                    // Timeout reached, clean up
                    _logger.LogWarning("Device authentication timed out, cleaning up");
                    CleanupPendingAuth();
                }
            }

            _logger.LogInformation("Starting device code authentication");
            _deviceAuthStartTime = DateTime.UtcNow;

            string deviceInstructions = string.Empty;
            var taskCompletionSource = new TaskCompletionSource<string>();

            // Start the device code flow with timeout
            var cancellationTokenSource = new CancellationTokenSource(_deviceAuthTimeout);
            var scopes = request.Scopes ?? AzureAdConfiguration.PowerBIScopes;

            // Start the device code flow but don't await it
            _pendingDeviceAuth = _publicClientApp
                .AcquireTokenWithDeviceCode(scopes, callback =>
                {
                    deviceInstructions = $@"🔐 **Device Code Authentication Started**

**Step 1:** Open your web browser and go to:
👉 {callback.VerificationUrl}

**Step 2:** Enter this device code:
📋 **{callback.UserCode}**

**Step 3:** Complete the sign-in process

⏳ **Use 'check_device_auth_status' tool to check completion status**

You have {callback.ExpiresOn.Subtract(DateTimeOffset.Now).Minutes} minutes to complete this.";

                    _pendingDeviceInstructions = deviceInstructions;
                    _logger.LogInformation("Device code: {UserCode} | URL: {VerificationUrl}",
                        callback.UserCode, callback.VerificationUrl);

                    // Signal that instructions are ready
                    taskCompletionSource.SetResult(deviceInstructions);
                    return Task.FromResult(0);
                })
                .ExecuteAsync(cancellationTokenSource.Token);

            // Wait for the callback to provide the instructions
            var instructions = await taskCompletionSource.Task;
            return Abstractions.Interfaces.AuthenticationResult.Failure(instructions); // Not a failure, but using Failure to return instructions without auth details
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Device code authentication timed out");
            CleanupPendingAuth();
            return Abstractions.Interfaces.AuthenticationResult.Failure("⏰ Device code authentication timed out. Please try again with 'start_device_code_auth'.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start device code authentication");
            CleanupPendingAuth();
            return Abstractions.Interfaces.AuthenticationResult.Failure(string.Format(Messages.AuthenticationErrorTemplate, ex.Message));
        }
    }

    private async Task<Abstractions.Interfaces.AuthenticationResult> CheckDeviceCodeStatusAsync(AuthenticationRequest request)
    {
        try
        {
            if (_pendingDeviceAuth == null)
            {
                return Abstractions.Interfaces.AuthenticationResult.Failure("No device authentication in progress. Use 'start_device_code_auth' first.");
            }

            if (!_pendingDeviceAuth.IsCompleted)
            {
                var timeRemaining = _deviceAuthStartTime.HasValue
                    ? _deviceAuthTimeout - (DateTime.UtcNow - _deviceAuthStartTime.Value)
                    : TimeSpan.Zero;

                var statusMessage = $"⏳ Device authentication still pending...\n\n{_pendingDeviceInstructions}\n\n⏱️ Time remaining: {Math.Max(0, timeRemaining.Minutes)} minutes";
                return Abstractions.Interfaces.AuthenticationResult.Failure(statusMessage);
            }

            // Authentication completed, get the result
            var result = await _pendingDeviceAuth;

            var authDetails = McpAuthenticationResult.Success(
                result.AccessToken,
                result.Account.Username,
                result.TenantId,
                result.ExpiresOn.DateTime,
                string.Join(", ", result.Scopes)
            );

            _logger.LogInformation("Device code authentication completed successfully for user: {Username}", result.Account.Username);

            // Clear pending auth
            CleanupPendingAuth();

            var message = $@"✅ **Authentication Successful!**
Signed in as: {result.Account.Username}
Tenant: {result.TenantId}";

            return Abstractions.Interfaces.AuthenticationResult.Success(message, authDetails);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Device code authentication was cancelled or timed out");
            CleanupPendingAuth();
            return Abstractions.Interfaces.AuthenticationResult.Failure("⏰ Device code authentication timed out or was cancelled. Please start a new authentication.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Device code authentication failed");
            CleanupPendingAuth();
            return Abstractions.Interfaces.AuthenticationResult.Failure(string.Format(Messages.AuthenticationFailedTemplate, ex.Message));
        }
    }

    private void CleanupPendingAuth()
    {
        _pendingDeviceAuth = null;
        _pendingDeviceInstructions = null;
        _deviceAuthStartTime = null;
    }

    private static IPublicClientApplication CreatePublicClientApp()
    {
        return PublicClientApplicationBuilder
            .Create(AzureAdConfiguration.ClientId)
            .WithAuthority(new Uri(AzureAdConfiguration.Authority))
            .WithRedirectUri(AzureAdConfiguration.RedirectUri)
            .Build();
    }
}