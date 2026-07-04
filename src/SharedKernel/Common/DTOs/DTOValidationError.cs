namespace SharedKernel.Common.DTOs;

/// <summary>
/// Represents a validation error in a DTO with optional error code and property name context.
/// </summary>
public record DTOValidationError
{
    // Private constructors enforce creation via factory methods.
    private DTOValidationError(string errorMessage)
    {
        ErrorMessage = errorMessage;
    }

    private DTOValidationError(string errorMessage, string errorCode) : this(errorMessage)
    {
        ErrorCode = errorCode;
    }

    private DTOValidationError(string errorMessage, string errorCode, string propertyName) : this(errorMessage, errorCode)
    {
        PropertyName = propertyName;
    }

    /// <summary>
    /// Creates a basic internal error with just an error message.
    /// </summary>
    /// <param name="errorMessage">The error message describing the error.</param>
    public static DTOValidationError CreateInternalError(string errorMessage)
    {
        var error = new DTOValidationError(errorMessage);
        return error;
    }

    /// <summary>
    /// Creates a simple error with an error message and an error code.
    /// </summary>
    /// <param name="errorMessage">The error message describing the error.</param>
    /// <param name="errorCode">The code representing the type of error.</param>
    public static DTOValidationError CreateSimpleError(string errorMessage, string errorCode)
    {
        var error = new DTOValidationError(errorMessage, errorCode);
        return error;
    }

    /// <summary>
    /// Creates a detailed error including message, code, and the property name that caused the error.
    /// </summary>
    /// <param name="errorMessage">The error message describing the error.</param>
    /// <param name="errorCode">The code representing the type of error.</param>
    /// <param name="propertyName">The name of the property related to the error.</param>
    public static DTOValidationError CreateDetailedError(string errorMessage, string errorCode, string propertyName)
    {
        var error = new DTOValidationError(errorMessage, errorCode, propertyName);
        return error;
    }

    /// <summary>
    /// Implicitly converts a single DTOValidationError into an array containing that error.
    /// </summary>
    /// <param name="error">The error to convert.</param>
    public static implicit operator DTOValidationError[](DTOValidationError error)
    {
        // Development Note:
        // Provides convenience for passing single errors to methods expecting arrays.
        return [error];
    }

    /// <summary>
    /// Gets the error message describing the validation failure.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets the name of the property related to the validation error, if applicable.
    /// </summary>
    public string? PropertyName { get; private set; }

    /// <summary>
    /// Gets the error code that categorizes the error, if applicable.
    /// </summary>
    public string? ErrorCode { get; private set; }
}
