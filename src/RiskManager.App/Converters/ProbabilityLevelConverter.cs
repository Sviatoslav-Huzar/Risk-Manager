using System.Globalization;
using System.Windows;
using System.Windows.Data;
using RiskManager.Core.Domain;

namespace RiskManager.App.Converters;

/// <summary>
/// Shows a probability level the way the methodology names it: Medium → "середня".
/// </summary>
[ValueConversion(typeof(ProbabilityLevel), typeof(string))]
public sealed class ProbabilityLevelConverter : IValueConverter
{
    public static string GetName(ProbabilityLevel level) => level switch
    {
        ProbabilityLevel.VeryLow => "дуже низька",
        ProbabilityLevel.Low => "низька",
        ProbabilityLevel.Medium => "середня",
        ProbabilityLevel.High => "висока",
        ProbabilityLevel.VeryHigh => "дуже висока",
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, "Unknown probability level."),
    };

    // null is a normal state here (an incomplete row), so it is shown as empty text.
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        null => string.Empty,
        ProbabilityLevel level => GetName(level),
        _ => DependencyProperty.UnsetValue,
    };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("A probability level cannot be converted back.");
}
