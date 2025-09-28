using System;
using System.Globalization;
using System.Windows.Data;

namespace SmartSleep.App.Converters;

public class TimeSpanToStringConverter : IValueConverter
{
    private static readonly string[] ParseFormats =
    {
        "hh\\:mm",
        "h\\:mm",
        "HHmm",
        "Hmm"
    };

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TimeSpan timeSpan)
        {
            return timeSpan.ToString("hh\\:mm");
        }

        return string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string input)
        {
            return System.Windows.Data.Binding.DoNothing;
        }

        var trimmed = input.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return System.Windows.Data.Binding.DoNothing;
        }

        if (TimeSpan.TryParseExact(trimmed, ParseFormats, culture, out var result) ||
            TimeSpan.TryParseExact(trimmed, ParseFormats, CultureInfo.InvariantCulture, out result))
        {
            return result;
        }

        return System.Windows.Data.Binding.DoNothing;
    }
}
