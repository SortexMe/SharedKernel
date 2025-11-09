using System;

namespace SharedKernel.Options;

/// <summary>
/// Represents configuration options for a cache entry's expiration policy.
/// </summary>
public class CacheEntryOptions
{
    /// <summary>
    /// Gets or sets an absolute expiration date and time for the cache entry.
    /// When set, the entry will expire and be removed from the cache at this exact point in time.
    /// </summary>
    public DateTimeOffset? AbsoluteExpiration { get; set; }

    /// <summary>
    /// Gets or sets an expiration time, relative to the current time,
    /// indicating how long the cache entry should live regardless of access frequency.
    /// </summary>
    public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }

    /// <summary>
    /// Gets or sets the sliding expiration interval for the cache entry.
    /// If the entry is not accessed within the specified time, it will expire and be removed.
    /// Note that this setting does not extend the lifetime of the entry beyond the absolute expiration (if specified).
    /// </summary>
    public TimeSpan? SlidingExpiration { get; set; }
}
