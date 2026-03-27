using System.Globalization;

namespace ApliqxPos.Helpers;

/// <summary>
/// Helper class for formatting currency values in Iraqi Dinar (IQD) and USD.
/// </summary>
public static class CurrencyHelper
{
    public const string IQD = "IQD";
    public const string USD = "USD";
    
    public const string IQDSymbol = "د.ع";
    public const string USDSymbol = "$";

    private static readonly CultureInfo ArabicCulture = new("ar-IQ");
    private static readonly CultureInfo EnglishCulture = new("en-US");

    /// <summary>
    /// Formats a value as Iraqi Dinar (IQD) without decimal places.
    /// Example: 25000 -> "25,000 د.ع"
    /// </summary>
    public static string FormatIQD(decimal value, bool showSymbol = true)
    {
        var formatted = value.ToString("#,##0", ArabicCulture);
        return showSymbol ? $"{formatted} {IQDSymbol}" : formatted;
    }

    /// <summary>
    /// Formats a value as US Dollar (USD) with optional decimals.
    /// Example: 25.50 -> "$25.50"
    /// </summary>
    public static string FormatUSD(decimal value, bool showSymbol = true, bool showDecimals = true)
    {
        var format = showDecimals ? "#,##0.00" : "#,##0";
        var formatted = value.ToString(format, EnglishCulture);
        return showSymbol ? $"{USDSymbol}{formatted}" : formatted;
    }

    /// <summary>
    /// Formats a value based on the currency code.
    /// </summary>
    public static string Format(decimal value, string currencyCode, bool showSymbol = true)
    {
        return currencyCode.ToUpperInvariant() switch
        {
            USD => FormatUSD(value, showSymbol),
            IQD => FormatIQD(value, showSymbol),
            _ => FormatIQD(value, showSymbol) // Default to IQD
        };
    }

    /// <summary>
    /// Gets the currency symbol for a given currency code.
    /// </summary>
    public static string GetSymbol(string currencyCode)
    {
        return currencyCode.ToUpperInvariant() switch
        {
            USD => USDSymbol,
            IQD => IQDSymbol,
            _ => IQDSymbol
        };
    }

    /// <summary>
    /// Parses a formatted currency string back to decimal.
    /// </summary>
    public static bool TryParse(string input, out decimal value)
    {
        // Remove currency symbols and common separators
        var cleaned = input
            .Replace(IQDSymbol, "")
            .Replace(USDSymbol, "")
            .Replace(",", "")
            .Replace(" ", "")
            .Trim();

        return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
    }

    /// <summary>
    /// Converts from USD to IQD using a given exchange rate.
    /// </summary>
    public static decimal ConvertToIQD(decimal usdAmount, decimal exchangeRate = 1460m)
    {
        return usdAmount * exchangeRate;
    }

    /// <summary>
    /// Converts from IQD to USD using a given exchange rate.
    /// </summary>
    public static decimal ConvertToUSD(decimal iqdAmount, decimal exchangeRate = 1460m)
    {
        return exchangeRate > 0 ? iqdAmount / exchangeRate : 0;
    }
}
