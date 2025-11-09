using SharedKernel.Common.DTOs;
using System;
using System.Net;

namespace SharedKernel.Common.Exceptions;

/// <summary>
/// Exception representing an authorization failure in the domain or application layer.
/// </summary>
public class NotAuthorizedException : Exception
{
    /// <summary>
    /// Gets the error message describing the authorization failure.
    /// </summary>
    public string ErrorMessage { get; private set; }

    // Private constructor to enforce creation via factory method.
    private NotAuthorizedException(string errorMessage) : base(errorMessage)
    {
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Creates a new instance of <see cref="NotAuthorizedException"/> with the specified error message.
    /// </summary>
    /// <param name="errorMessage">The error message describing the authorization failure.</param>
    public static NotAuthorizedException Create(string errorMessage)
    {
        return new NotAuthorizedException(errorMessage);
    }

    /// <summary>
    /// Implicitly converts a <see cref="NotAuthorizedException"/> to a <see cref="DTOValidationError"/>.
    /// </summary>
    /// <param name="notAuthorizedException">The exception to convert.</param>
    public static implicit operator DTOValidationError(NotAuthorizedException notAuthorizedException)
    {
        return DTOValidationError.CreateSimpleError(notAuthorizedException.ErrorMessage, "NotAuthorized");
    }

    /// <summary>
    /// Implicitly converts a <see cref="NotAuthorizedException"/> to a <see cref="BaseResponseDTO"/>
    /// with an HTTP 401 Unauthorized status code.
    /// </summary>
    /// <param name="notAuthorizedException">The exception to convert.</param>
    public static implicit operator BaseResponseDTO(NotAuthorizedException notAuthorizedException)
    {
        var response = BaseResponseDTO.WithError(notAuthorizedException);
        response.StatusCode = (int)HttpStatusCode.Unauthorized;
        return response;
    }
}

// Development Notes:
// - Factory method used to control instantiation and enforce error message requirement.
// - Implicit conversions ease integration with validation and API response layers.
// - Status code 401 (Unauthorized) is set automatically on conversion to response DTO.
