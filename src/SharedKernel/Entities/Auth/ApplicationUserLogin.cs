using SharedKernel.Entities.Base;
using SharedKernel.Enumerations;
using System;
using System.ComponentModel.DataAnnotations;

namespace SharedKernel.Entities.Auth;

/// <summary>
/// Represents a record of a user's login event including device, client, and network information.
/// </summary>
public class ApplicationUserLogin : DomainEntityBase
{
    /// <summary>
    /// Gets or sets the identifier of the user associated with this login.
    /// </summary>
    public string UserId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the date and time of the login event in UTC.
    /// </summary>
    public DateTimeOffset LoginTime { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the type of login (e.g., standard, social, etc.).
    /// </summary>
    public LoginType LoginType { get; set; }

    /// <summary>
    /// Gets or sets the external provider name if the login is via an external authentication provider.
    /// </summary>
    [MaxLength(100)]
    public string? ProviderName { get; set; }

    // Device & Client Info
    /// <summary>
    /// Gets or sets the brand of the device used during login (e.g., Apple, Samsung).
    /// </summary>
    [MaxLength(100)]
    public string? DeviceBrand { get; set; }      // Apple, Samsung, etc.

    /// <summary>
    /// Gets or sets the model of the device used during login (e.g., iPhone 15, Galaxy S23).
    /// </summary>
    [MaxLength(100)]
    public string? DeviceModel { get; set; }      // iPhone 15, Galaxy S23

    /// <summary>
    /// Gets or sets the operating system name of the device (e.g., iOS 17, Windows 11).
    /// </summary>
    [MaxLength(100)]
    public string? OperatingSystem { get; set; }  // iOS 17, Windows 11

    /// <summary>
    /// Gets or sets the version of the operating system.
    /// </summary>
    [MaxLength(50)]
    public string? OSVersion { get; set; }        // 17.1, 11.0.22621

    /// <summary>
    /// Gets or sets the browser name used during login (e.g., Chrome, Firefox).
    /// </summary>
    [MaxLength(100)]
    public string? Browser { get; set; }          // Chrome, Firefox

    /// <summary>
    /// Gets or sets the browser version.
    /// </summary>
    [MaxLength(50)]
    public string? BrowserVersion { get; set; }   // 123.0.0.1

    /// <summary>
    /// Gets or sets the platform type (e.g., Mobile, Desktop, Tablet, Bot).
    /// </summary>
    [MaxLength(50)]
    public string? Platform { get; set; }         // Mobile, Desktop, Tablet, Bot

    /// <summary>
    /// Gets or sets the raw user agent string.
    /// </summary>
    [MaxLength(512)]
    public string? UserAgent { get; set; }        // Raw UA string

    // Network Info
    /// <summary>
    /// Gets or sets the IP address v4 from which the login was made.
    /// </summary>
    [MaxLength(64)]
    public string? IPAddress { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the login attempt was successful.
    /// </summary>
    public bool IsSuccessful { get; set; } = true;

    /// <summary>
    /// Gets or sets the related application user entity.
    /// </summary>
    public virtual ApplicationUser ApplicationUser { get; set; } = null!;

    // Development Notes:
    // - Extends DomainEntityBase to inherit unique identifier and domain event capabilities.
    // - Captures detailed device, client, and network info to support auditing, security, and analytics.
    // - Optional provider name supports external authentication methods.
    // - IP address and user agent string fields help in tracking login origin and context.
    // - The IsSuccessful flag allows marking failed login attempts for security monitoring.
    // - Designed to be associated with ApplicationUser, enabling navigation from login record to user.
}
