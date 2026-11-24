using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RiskManager.App.Converters;

/// <summary>
/// Converts an optional number to text and back. An empty text means "no value",
/// and both "0,5" and "0.5" are accepted regardless of the culture.
/// </summary>
[ValueConversion(typeof(double?), typeof(string))]
public sealed class NullableNumberConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        null => string.Empty,
        double number => number.ToString(culture),
        _ => DependencyProperty.UnsetValue,
    };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string text = (value as string ?? string.Empty).Trim();

        if (text.Length == 0)
        {
            return null;
        }

        // A thousands separator makes no sense for numbers in [0; 1], so a comma
        // can only be a decimal separator here.
        const NumberStyles styles = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

        // UnsetValue means "cannot be converted": the binding reports a
        // validation error and leaves the view model untouched.
        // A text with hundreds of digits is parsed as infinity, which is not a
        // number either (and cannot be saved to a file).
        return double.TryParse(text.Replace(',', '.'), styles, CultureInfo.InvariantCulture, out double number)
            && double.IsFinite(number)
            ? number
            : DependencyProperty.UnsetValue;
    }
}
