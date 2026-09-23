using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json.Serialization;

namespace SharedKernel.Common.DTOs;

/// <summary>
/// Represents a standard response DTO indicating the outcome of an operation.
/// Includes support for error reporting, status, and traceability.
/// </summary>
/// <remarks>
/// Instances are created through the factory methods. The type round-trips through System.Text.Json,
/// so a service consuming another service's response can deserialize it directly.
/// </remarks>
public record BaseResponseDTO
{
    internal const string SuccessMessage = "Your request has been successfully processed";
    internal const string ErrorPrefix = "An error occurred while processing your request:";

    [JsonConstructor]
    private BaseResponseDTO()
    {
    }

    /// <summary>
    /// Creates a success response with a default success message and HTTP 200.
    /// </summary>
    public static BaseResponseDTO WithSuccess() => new()
    {
        Message = SuccessMessage,
        StatusCode = (int)HttpStatusCode.OK,
    };

    /// <summary>
    /// Creates an error response containing a single validation error.
    /// </summary>
    /// <param name="error">The validation error to include.</param>
    public static BaseResponseDTO WithError(DTOValidationError error) => WithErrors([error]);

    /// <summary>
    /// Creates an error response containing multiple validation errors.
    /// </summary>
    /// <param name="errors">The array of validation errors.</param>
    public static BaseResponseDTO WithErrors(DTOValidationError[] errors) => new()
    {
        Errors = errors ?? throw new ArgumentNullException(nameof(errors)),
        Message = FormatErrorMessage(errors),
        StatusCode = (int)HttpStatusCode.BadRequest,
    };

    /// <summary>
    /// Indicates whether the response represents a successful operation.
    /// </summary>
    public bool IsSuccess => Errors.Count == 0;

    /// <summary>
    /// Gets or sets the trace identifier related to this response.
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// Gets or sets the HTTP status code associated with the response.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the descriptive message for this response.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets the collection of validation errors.
    /// </summary>
    [JsonInclude]
    public IReadOnlyCollection<DTOValidationError> Errors { get; private set; } = [];

    internal static string FormatErrorMessage(IReadOnlyCollection<DTOValidationError> errors)
    {
        if (errors.Count == 1)
            return $"{ErrorPrefix}{Environment.NewLine}{errors.First().ErrorMessage}";

        var lines = errors
            .Where(m => !string.IsNullOrWhiteSpace(m.ErrorMessage))
            .Select((m, index) => $"{index + 1}. {m.ErrorMessage}");

        return $"{ErrorPrefix}{Environment.NewLine}{string.Join(Environment.NewLine, lines)}";
    }
}

/// <summary>
/// Generic response DTO that encapsulates a data payload alongside operation result info.
/// </summary>
/// <typeparam name="T">The type of the data returned in the response.</typeparam>
public record BaseResponseDTO<T>
{
    [JsonConstructor]
    private BaseResponseDTO()
    {
    }

    /// <summary>
    /// Creates a success response wrapping the specified data, with HTTP 200.
    /// </summary>
    /// <param name="data">The data to return.</param>
    public static BaseResponseDTO<T> WithSuccess(T data) => new()
    {
        Data = data,
        Message = BaseResponseDTO.SuccessMessage,
        StatusCode = (int)HttpStatusCode.OK,
    };

    /// <summary>
    /// Creates an error response wrapping the data and a single validation error.
    /// </summary>
    /// <param name="data">The data to return.</param>
    /// <param name="error">The validation error to include.</param>
    public static BaseResponseDTO<T> WithError(T data, DTOValidationError error) => WithErrors(data, [error]);

    /// <summary>
    /// Creates an error response wrapping the data and multiple validation errors.
    /// </summary>
    /// <param name="data">The data to return.</param>
    /// <param name="errors">The array of validation errors.</param>
    public static BaseResponseDTO<T> WithErrors(T data, DTOValidationError[] errors) => new()
    {
        Data = data,
        Errors = errors ?? throw new ArgumentNullException(nameof(errors)),
        Message = BaseResponseDTO.FormatErrorMessage(errors),
        StatusCode = (int)HttpStatusCode.BadRequest,
    };

    /// <summary>
    /// Indicates whether the response represents a successful operation.
    /// </summary>
    public bool IsSuccess => Errors.Count == 0;

    /// <summary>
    /// Gets or sets the trace identifier related to this response.
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// Gets or sets the HTTP status code associated with the response.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the descriptive message for this response.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets the data returned in the response. May be default when the response carries only errors.
    /// </summary>
    [JsonInclude]
    public T? Data { get; private set; }

    /// <summary>
    /// Gets the collection of validation errors.
    /// </summary>
    [JsonInclude]
    public IReadOnlyCollection<DTOValidationError> Errors { get; private set; } = [];
}
