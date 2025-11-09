using SharedKernel.Common.DTOs;
using System;
using System.Net;

namespace SharedKernel.Common.Exceptions;

/// <summary>
/// Exception thrown when a requested record is not found in the data store.
/// </summary>
public class RecordNotFoundException : Exception
{
    /// <summary>
    /// Gets the error message describing the missing record.
    /// </summary>
    public string ErrorMessage { get; private set; }

    // Private constructor to enforce creation via factory method.
    private RecordNotFoundException(string errorMessage) : base(errorMessage)
    {
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Creates a new <see cref="RecordNotFoundException"/> with the specified error message.
    /// </summary>
    /// <param name="errorMessage">The error message describing the missing record.</param>
    public static RecordNotFoundException Create(string errorMessage)
    {
        return new RecordNotFoundException(errorMessage);
    }

    /// <summary>
    /// Implicitly converts a <see cref="RecordNotFoundException"/> to a <see cref="DTOValidationError"/>.
    /// </summary>
    /// <param name="recordNotFoundException">The exception to convert.</param>
    public static implicit operator DTOValidationError(RecordNotFoundException recordNotFoundException)
    {
        return DTOValidationError.CreateSimpleError(recordNotFoundException.ErrorMessage, "RecordNotFound");
    }

    /// <summary>
    /// Implicitly converts a <see cref="RecordNotFoundException"/> to a <see cref="BaseResponseDTO"/>
    /// with an HTTP 404 Not Found status code.
    /// </summary>
    /// <param name="recordNotFoundException">The exception to convert.</param>
    public static implicit operator BaseResponseDTO(RecordNotFoundException recordNotFoundException)
    {
        var response = BaseResponseDTO.WithError(recordNotFoundException);
        response.StatusCode = (int)HttpStatusCode.NotFound;
        return response;
    }
}

// Development Notes:
// - Factory method pattern ensures consistent creation with a meaningful message.
// - Implicit conversion to DTOValidationError aids in validation aggregation.
// - Implicit conversion to BaseResponseDTO streamlines API error response handling with 404 status.