using DataFactory.MCP.Abstractions.Interfaces;

namespace DataFactory.MCP.Models.Connection.Validators;

/// <summary>
/// Helper class for validating Microsoft Fabric data source connection parameters
/// </summary>
public class FabricConnectionParameterValidator
{
    private readonly IValidationService _validationService;

    public FabricConnectionParameterValidator(IValidationService validationService)
    {
        _validationService = validationService;
    }

    /// <summary>
    /// Validates parameters for SQL connection creation
    /// </summary>
    public void ValidateSqlConnectionParameters(
        string displayName,
        string serverName,
        string databaseName,
        string? username = null,
        string? password = null)
    {
        _validationService.ValidateRequiredString(displayName, nameof(displayName), maxLength: 200);
        _validationService.ValidateRequiredString(serverName, nameof(serverName));
        _validationService.ValidateRequiredString(databaseName, nameof(databaseName));

        if (username != null)
        {
            _validationService.ValidateRequiredString(username, nameof(username));
        }

        if (password != null)
        {
            _validationService.ValidateRequiredString(password, nameof(password));
        }
    }

    /// <summary>
    /// Validates parameters for web connection creation
    /// </summary>
    public void ValidateWebConnectionParameters(
        string displayName,
        string url,
        string? username = null,
        string? password = null)
    {
        _validationService.ValidateRequiredString(displayName, nameof(displayName), maxLength: 200);
        _validationService.ValidateRequiredString(url, nameof(url));

        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            throw new ArgumentException("Invalid URL format", nameof(url));
        }

        if (username != null)
        {
            _validationService.ValidateRequiredString(username, nameof(username));
        }

        if (password != null)
        {
            _validationService.ValidateRequiredString(password, nameof(password));
        }
    }

    /// <summary>
    /// Validates parameters for VNet gateway connection creation
    /// </summary>
    public void ValidateVNetGatewayConnectionParameters(
        string displayName,
        string gatewayId,
        string serverName,
        string databaseName,
        string? username = null,
        string? password = null)
    {
        _validationService.ValidateRequiredString(displayName, nameof(displayName), maxLength: 200);
        _validationService.ValidateGuid(gatewayId, nameof(gatewayId));
        _validationService.ValidateRequiredString(serverName, nameof(serverName));
        _validationService.ValidateRequiredString(databaseName, nameof(databaseName));

        if (username != null)
        {
            _validationService.ValidateRequiredString(username, nameof(username));
        }

        if (password != null)
        {
            _validationService.ValidateRequiredString(password, nameof(password));
        }
    }
}