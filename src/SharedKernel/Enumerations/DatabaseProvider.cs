namespace SharedKernel.Enumerations;

/// <summary>
/// Specifies the supported database providers for tenant connections.
/// </summary>
public enum DatabaseProvider
{
    /// <summary>
    /// PostgreSQL database provider.
    /// </summary>
    PostgreSQL = 0,

    /// <summary>
    /// Microsoft SQL Server database provider.
    /// </summary>
    SqlServer = 1,

    /// <summary>
    /// MySQL database provider.
    /// </summary>
    MySQL = 2,
}
