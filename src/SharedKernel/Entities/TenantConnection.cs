using SharedKernel.Entities.Base;
using SharedKernel.Enumerations;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace SharedKernel.Entities;

/// <summary>
/// Represents a tenant's database connection information.
/// </summary>
public sealed class TenantConnection : EntityBase
{
    /// <summary>
    /// Gets or sets the connection string used to connect to the tenant's database.
    /// </summary>
    public string ConnectionString { get; set; } = null!;

    /// <summary>
    /// Gets or sets the database provider type for this tenant connection.
    /// Defaults to PostgreSQL.
    /// </summary>
    public DatabaseProvider DatabaseProvider { get; set; } = DatabaseProvider.PostgreSQL;

    /// <summary>
    /// Gets or sets a flag indicating whether this connection is private,
    /// meaning it is dedicated to one and only one tenant assigned to it.
    /// </summary>
    public bool IsPrivate { get; set; }

    /// <summary>
    /// Gets or sets a flag indicating whether this tenant has full access or capabilities.
    /// </summary>
    public bool IsFull { get; set; }

    /// <summary>
    /// Gets or sets the UTC date and time when this tenant connection was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets a flag indicating whether this connection refers to a newly created database.
    /// This property is not mapped to the database.
    /// </summary>
    [NotMapped]
    public bool IsNewDatabase { get; set; }
}
