using System;
using System.Collections.Generic;
using System.Linq;

namespace SharedKernel.Common.DTOs;

/// <summary>
/// Represents a standard response DTO indicating the outcome of an operation.
/// Includes support for error reporting, status, and traceability.
/// </summary>
public record BaseResponseDTO
{
    // Backing list for validation errors.
    private readonly List<DTOValidationError> errors = new();

    // Private constructor to enforce use of factory methods.
    private BaseResponseDTO()
    {
    }

    private BaseResponseDTO(DTOValidationError[] errors)
    {
        this.errors.AddRange(errors);
    }

    /// <summary>
    /// Creates a success response with a default success message.
    /// </summary>
    public static BaseResponseDTO WithSuccess()
    {
        var response = new BaseResponseDTO();
        response.Message = "Your request has been successfully processed";
        return response;
    }

    /// <summary>
    /// Creates an error response containing a single validation error.
    /// </summary>
    /// <param name="error">The validation error to include.</param>
    public static BaseResponseDTO WithError(DTOValidationError error)
    {
        var response = new BaseResponseDTO([error]);
        response.Message = $"An error occurred while processing your request:{Environment.NewLine}{error.ErrorMessage}";
        return response;
    }

    /// <summary>
    /// Creates an error response containing multiple validation errors.
    /// </summary>
    /// <param name="errors">The array of validation errors.</param>
    public static BaseResponseDTO WithErrors(DTOValidationError[] errors)
    {
        var response = new BaseResponseDTO(errors);
        response.Message = $"An error occurred while processing your request:{Environment.NewLine}{string.Join(Environment.NewLine, errors.Where(m => !string.IsNullOrWhiteSpace(m.ErrorMessage)).Select((m, index) => $"{index + 1}. {m.ErrorMessage}"))}";
        return response;
    }

    /// <summary>
    /// Indicates whether the response represents a successful operation.
    /// </summary>
    public bool IsSuccess => Errors == null || Errors.Count == 0;

    /// <summary>
    /// Gets the trace identifier related to this response.
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// Gets the HTTP status code associated with the response.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Gets the descriptive message for this response.
    /// </summary>
    public string Message { get; set; } = null!;

    /// <summary>
    /// Gets the collection of validation errors.
    /// </summary>
    public IReadOnlyCollection<DTOValidationError> Errors => errors;
}

/// <summary>
/// Generic response DTO that encapsulates a data payload alongside operation result info.
/// </summary>
/// <typeparam name="T">The type of the data returned in the response.</typeparam>
public record BaseResponseDTO<T>
{
    private readonly List<DTOValidationError> errors = new();

    private BaseResponseDTO(T data)
    {
        Data = data;
    }

    private BaseResponseDTO(T data, DTOValidationError[] errors) : this(data)
    {
        this.errors.AddRange(errors);
    }

    /// <summary>
    /// Creates a success response wrapping the specified data.
    /// </summary>
    /// <param name="data">The data to return.</param>
    public static BaseResponseDTO<T> WithSuccess(T data)
    {
        var response = new BaseResponseDTO<T>(data);
        response.Message = "Your request has been successfully processed";
        return response;
    }

    /// <summary>
    /// Creates an error response wrapping the data and a single validation error.
    /// </summary>
    /// <param name="data">The data to return.</param>
    /// <param name="error">The validation error to include.</param>
    public static BaseResponseDTO<T> WithError(T data, DTOValidationError error)
    {
        var response = new BaseResponseDTO<T>(data, [error]);
        response.Message = $"An error occurred while processing your request:{Environment.NewLine}{error.ErrorMessage}";
        return response;
    }

    /// <summary>
    /// Creates an error response wrapping the data and multiple validation errors.
    /// </summary>
    /// <param name="data">The data to return.</param>
    /// <param name="errors">The array of validation errors.</param>
    public static BaseResponseDTO<T> WithErrors(T data, DTOValidationError[] errors)
    {
        var response = new BaseResponseDTO<T>(data, errors);
        response.Message = $"An error occurred while processing your request:{Environment.NewLine}{string.Join(Environment.NewLine, errors.Where(m => !string.IsNullOrWhiteSpace(m.ErrorMessage)).Select((m, index) => $"{index + 1}. {m.ErrorMessage}"))}";
        return response;
    }

    /// <summary>
    /// Indicates whether the response represents a successful operation.
    /// </summary>
    public bool IsSuccess => Errors == null || Errors.Count == 0;

    /// <summary>
    /// Gets the trace identifier related to this response.
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// Gets the HTTP status code associated with the response.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Gets the descriptive message for this response.
    /// </summary>
    public string Message { get; set; } = null!;

    /// <summary>
    /// Gets the data returned in the response.
    /// </summary>
    public T Data { get; private init; }

    /// <summary>
    /// Gets the collection of validation errors.
    /// </summary>
    public IReadOnlyCollection<DTOValidationError> Errors => errors;
}
