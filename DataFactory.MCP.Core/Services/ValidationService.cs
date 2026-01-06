using System.ComponentModel.DataAnnotations;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Models;

namespace DataFactory.MCP.Services;

/// <summary>
/// Implementation of centralized validation service
/// </summary>
public class ValidationService : IValidationService
{
    public void ValidateAndThrow<T>(T obj, string parameterName) where T : class
    {
        if (obj == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(obj);

        if (!Validator.TryValidateObject(obj, validationContext, validationResults, validateAllProperties: true))
        {
            var errors = string.Join("; ", validationResults.Select(r => r.ErrorMessage));
            throw new ArgumentException($"Validation failed: {errors}", parameterName);
        }
    }

    public IList<ValidationResult> Validate<T>(T obj) where T : class
    {
        if (obj == null)
        {
            return new List<ValidationResult> { new ValidationResult("Object cannot be null") };
        }

        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(obj);
        Validator.TryValidateObject(obj, validationContext, validationResults, validateAllProperties: true);

        return validationResults;
    }

    public void ValidateRequiredString(string value, string parameterName, int? maxLength = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(Messages.InvalidParameterEmpty(parameterName), parameterName);
        }

        if (maxLength.HasValue && value.Length > maxLength.Value)
        {
            throw new ArgumentException($"{parameterName} cannot exceed {maxLength.Value} characters", parameterName);
        }
    }

    public void ValidateGuid(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{parameterName} is required and cannot be empty", parameterName);
        }

        if (!Guid.TryParse(value, out _))
        {
            throw new ArgumentException($"{parameterName} must be a valid GUID", parameterName);
        }
    }
}