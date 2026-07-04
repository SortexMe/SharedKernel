using SharedKernel.Common.DTOs;
using System;
using System.Collections.Generic;
using System.Net;

namespace SharedKernel.Common.Exceptions;

/// <summary>
/// Represents a domain-specific exception carrying validation error details.
/// </summary>
/// <remarks>
/// This exception wraps one or more <see cref="DTOValidationError"/> instances
/// to provide rich error information in domain logic.
/// </remarks>
public class DomainException : Exception
{
    // Backing list of validation errors.
    private readonly List<DTOValidationError> errors = [];

    /// <summary>
    /// Gets the read-only collection of validation errors associated with this exception.
    /// </summary>
    public IReadOnlyCollection<DTOValidationError> Errors => errors;

    // Private constructors enforce use of factory methods for controlled instantiation.

    private DomainException(string errorMessage) : base(errorMessage)
    {
        errors.Add(DTOValidationError.CreateInternalError(errorMessage));
    }

    private DomainException(string errorMessage, string errorCode) : base(errorMessage)
    {
        errors.Add(DTOValidationError.CreateSimpleError(errorMessage, errorCode));
    }

    private DomainException(string errorMessage, string errorCode, string propertyName) : base(errorMessage)
    {
        errors.Add(DTOValidationError.CreateDetailedError(errorMessage, errorCode, propertyName));
    }

    private DomainException(IEnumerable<DTOValidationError> errors)
    {
        this.errors.AddRange(errors);
    }

    /// <summary>
    /// Creates a domain exception representing an internal error with a message.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    public static DomainException CreateInternalException(string errorMessage)
    {
        return new DomainException(errorMessage);
    }

    /// <summary>
    /// Creates a domain exception with a simple error message and error code.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="errorCode">The error code.</param>
    public static DomainException CreateSimpleException(string errorMessage, string errorCode)
    {
        return new DomainException(errorMessage, errorCode);
    }

    /// <summary>
    /// Creates a domain exception with a detailed error including property name.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="errorCode">The error code.</param>
    /// <param name="propertyName">The related property name.</param>
    public static DomainException CreateDetailedException(string errorMessage, string errorCode, string propertyName)
    {
        return new DomainException(errorMessage, errorCode, propertyName);
    }

    /// <summary>
    /// Creates a domain exception containing multiple validation errors.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    public static DomainException CreateWithErrors(IEnumerable<DTOValidationError> errors)
    {
        return new DomainException(errors);
    }

    /// <summary>
    /// Implicitly converts a <see cref="DomainException"/> into a <see cref="BaseResponseDTO"/>,
    /// setting the status code to 400 Bad Request.
    /// </summary>
    /// <param name="exception">The domain exception.</param>
    public static implicit operator BaseResponseDTO(DomainException exception)
    {
        var response = BaseResponseDTO.WithErrors(exception.errors.ToArray());
        response.StatusCode = (int)HttpStatusCode.BadRequest;
        return response;
    }
}

// Development Notes:
// - Factory methods provide controlled creation patterns for various error detail levels.
// - The Errors collection exposes the validation errors that caused this domain exception.
// - The implicit operator allows easy conversion to API response DTOs with HTTP 400 status.