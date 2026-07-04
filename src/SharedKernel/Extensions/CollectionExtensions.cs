using SharedKernel.Common.DTOs;
using SharedKernel.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedKernel.Extensions;

/// <summary>
/// Extension methods for collections.
/// </summary>
public static class CollectionExtensions
{
    /// <summary>
    /// Throws a <see cref="DomainException"/> if the collection of <see cref="DTOValidationError"/> is not null and contains any errors.
    /// </summary>
    /// <typeparam name="T">Type of collection implementing <see cref="IEnumerable{DTOValidationError}"/>.</typeparam>
    /// <param name="errors">The collection of validation errors.</param>
    /// <exception cref="DomainException">Thrown when the collection contains one or more errors.</exception>
    public static void ThrowDomainException<T>(this T? errors) where T : IEnumerable<DTOValidationError>
    {
        // Development notes:
        // - This extension method allows easy validation of collections of DTOValidationError.
        // - If the collection has any errors, it throws a DomainException containing all of them.
        // - Useful for enforcing business rules and validation in domain logic.
        if (errors?.Any() == true)
            throw DomainException.CreateWithErrors(errors);
    }
}