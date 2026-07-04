using SharedKernel.Entities.Base;

namespace SharedKernel.Entities;

/// <summary>
/// Represents a country with various cultural and identification details.
/// </summary>
public class Country : EntityBase
{
    /// <summary>
    /// Gets or sets the common name of the country in English.
    /// </summary>
    public string CountryName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the native name of the country in the local language.
    /// </summary>
    public string NativeName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the ISO code of the country (e.g., "US", "DE").
    /// </summary>
    public string CountryISO { get; set; } = null!;

    /// <summary>
    /// Gets or sets the common symbol or currency symbol used by the country.
    /// </summary>
    public string Symbol { get; set; } = null!;

    /// <summary>
    /// Gets or sets the native symbol or currency symbol in the local language.
    /// </summary>
    public string NativeSymbol { get; set; } = null!;

    /// <summary>
    /// The currency name in English, based on the ISO 4217 standard
    /// (e.g. "Saudi Riyal", "US Dollar").
    /// This value is used for display purposes in English-based UIs
    /// and backend integrations.
    /// </summary>
    public string CurrencyDisplayName { get; set; } = null!;

    /// <summary>
    /// The currency name in the native language of the issuing country,
    /// based on Unicode CLDR data (e.g. "ريال سعودي", "دولار أمريكي").
    /// This value is intended for localized and multilingual user interfaces.
    /// </summary>
    public string CurrencyNativeName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the number of decimal places used in the country's currency.
    /// </summary>
    public int DecimalPlaces { get; set; }

    /// <summary>
    /// Gets or sets the international dialing code for the country (e.g., "+1", "+49").
    /// </summary>
    public string DialCode { get; set; } = null!;

    // Development Notes:
    // - Inherits EntityBase for Id and equality comparison.
    // - Stores essential country information relevant for localization, formatting, and telephony.
    // - Used in applications requiring cultural or regional settings.
}
