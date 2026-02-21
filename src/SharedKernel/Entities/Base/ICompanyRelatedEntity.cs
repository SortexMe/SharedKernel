using System;
using System.ComponentModel.DataAnnotations;

namespace SharedKernel.Entities.Base;

/// <summary>
/// Marker interface for entities that are associated with a specific company.
/// </summary>
/// <remarks>
/// Entities implementing this interface should have their <see cref="CompanyId"/> property
/// automatically set when saving changes, if it does not already have a value.
/// This supports multi-company or multi-tenant scenarios where data is scoped per company.
/// </remarks>
public interface ICompanyRelatedEntity
{
    /// <summary>
    /// Gets or sets the company identifier to which the entity belongs.
    /// </summary>
    // Development Note:
    // This property will be automatically populated during persistence operations
    // if it is null or empty to enforce company data isolation.
    public Guid CompanyId { get; set; }
}
