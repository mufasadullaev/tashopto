using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace Opto.Converters;

public sealed class SafeDecimalConverter : IValueConverter
{
    public static readonly SafeDecimalConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is decimal d)
            return d.ToString("0.######", OptoCulture.Current);

        return "0";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var text = (value as string)?.Trim().Replace(',', '.') ?? "";

        // Пока поле пустое при наборе — не трогаем модель.
        if (string.IsNullOrEmpty(text) || text == ".")
            return BindingOperations.DoNothing;

        return decimal.TryParse(
            text,
            NumberStyles.AllowDecimalPoint,
            OptoCulture.Current,
            out var number)
            ? number
            : BindingOperations.DoNothing;
    }
}

public sealed class SafeIntConverter : IValueConverter
{
    public static readonly SafeIntConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int i)
            return i.ToString(OptoCulture.Current);

        return "0";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var text = (value as string)?.Trim() ?? "";
        if (string.IsNullOrEmpty(text))
            return BindingOperations.DoNothing;

        return int.TryParse(text, NumberStyles.Integer, OptoCulture.Current, out var number)
            ? number
            : BindingOperations.DoNothing;
    }
}
