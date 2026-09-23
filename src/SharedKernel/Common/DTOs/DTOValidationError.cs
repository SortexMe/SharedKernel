using System.Text.Json.Serialization;

namespace SharedKernel.Common.DTOs;

/// <summary>
/// Represents a validation error in a DTO with optional error code and property name context.
/// </summary>
/// <remarks>
/// Instances are created through the factory methods; the private constructor is also the
/// <see cref="JsonConstructorAttribute">JSON constructor</see> so the type round-trips through System.Text.Json.
/// </remarks>
public record DTOValidationError
{
    [JsonConstructor]
    private DTOValidationError(string? errorMessage, string? errorCode, string? propertyName)
    {
        ErrorMessage = errorMessage;
        ErrorCode = errorCode;
        PropertyName = propertyName;
    }

    /// <summary>
    /// Creates a basic internal error with just an error message.
    /// </summary>
    /// <param name="errorMessage">The error message describing the error.</param>
    public static DTOValidationError CreateInternalError(string errorMessage) => new(errorMessage, null, null);

    /// <summary>
    /// Creates a simple error with an error message and an error code.
    /// </summary>
    /// <param name="errorMessage">The error message describing the error.</param>
    /// <param name="errorCode">The code representing the type of error.</param>
    public static DTOValidationError CreateSimpleError(string errorMessage, string errorCode) => new(errorMessage, errorCode, null);

    /// <summary>
    /// Creates a detailed error including message, code, and the property name that caused the error.
    /// </summary>
    /// <param name="errorMessage">The error message describing the error.</param>
    /// <param name="errorCode">The code representing the type of error.</param>
    /// <param name="propertyName">The name of the property related to the error.</param>
    public static DTOValidationError CreateDetailedError(string errorMessage, string errorCode, string propertyName) => new(errorMessage, errorCode, propertyName);

    /// <summary>
    /// Implicitly converts a single DTOValidationError into an array containing that error.
    /// </summary>
    /// <param name="error">The error to convert.</param>
    public static implicit operator DTOValidationError[](DTOValidationError error) => [error];

    /// <summary>
    /// Gets the error message describing the validation failure.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Gets the name of the property related to the validation error, if applicable.
    /// </summary>
    public string? PropertyName { get; }

    /// <summary>
    /// Gets the error code that categorizes the error, if applicable.
    /// </summary>
    public string? ErrorCode { get; }
}
