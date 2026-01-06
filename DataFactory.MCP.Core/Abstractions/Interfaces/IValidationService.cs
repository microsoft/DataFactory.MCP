using System.ComponentModel.DataAnnotations;

namespace DataFactory.MCP.Abstractions.Interfaces;

/// <summary>
/// Centralized validation service for consistent model validation across all Fabric services
/// </summary>
public interface IValidationService
{
    /// <summary>
    /// Validates an object using Data Annotations and throws ArgumentException if invalid
    /// </summary>
    /// <typeparam name="T">Type of object to validate</typeparam>
    /// <param name="obj">Object to validate</param>
    /// <param name="parameterName">Parameter name for exception context</param>
    /// <exception cref="ArgumentNullException">When object is null</exception>
    /// <exception cref="ArgumentException">When validation fails</exception>
    void ValidateAndThrow<T>(T obj, string parameterName) where T : class;

    /// <summary>
    /// Validates an object using Data Annotations and returns validation results
    /// </summary>
    /// <typeparam name="T">Type of object to validate</typeparam>
    /// <param name="obj">Object to validate</param>
    /// <returns>List of validation results (empty if valid)</returns>
    IList<ValidationResult> Validate<T>(T obj) where T : class;

    /// <summary>
    /// Validates a required string parameter
    /// </summary>
    /// <param name="value">String value to validate</param>
    /// <param name="parameterName">Parameter name for exception context</param>
    /// <param name="maxLength">Optional maximum length validation</param>
    /// <exception cref="ArgumentException">When string is invalid</exception>
    void ValidateRequiredString(string value, string parameterName, int? maxLength = null);

    /// <summary>
    /// Validates that a string is a valid GUID format
    /// </summary>
    /// <param name="value">String value to validate as GUID</param>
    /// <param name="parameterName">Parameter name for exception context</param>
    /// <exception cref="ArgumentException">When string is not a valid GUID</exception>
    void ValidateGuid(string value, string parameterName);
}