using System.Globalization;

namespace Opto;

public static class OptoCulture
{
    /// <summary>
    /// Русский UI, но десятичный разделитель — точка.
    /// </summary>
    public static CultureInfo Current { get; } = Create();

    private static CultureInfo Create()
    {
        var culture = (CultureInfo)CultureInfo.GetCultureInfo("ru-RU").Clone();
        culture.NumberFormat.NumberDecimalSeparator = ".";
        culture.NumberFormat.NumberGroupSeparator = " ";
        culture.NumberFormat.CurrencyDecimalSeparator = ".";
        culture.NumberFormat.CurrencyGroupSeparator = " ";
        culture.NumberFormat.PercentDecimalSeparator = ".";
        culture.NumberFormat.PercentGroupSeparator = " ";
        return CultureInfo.ReadOnly(culture);
    }
}
