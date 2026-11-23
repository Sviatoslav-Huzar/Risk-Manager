using System.Globalization;
using System.Windows;
using System.Windows.Data;
using RiskManager.Core.Domain;

namespace RiskManager.App.Converters;

/// <summary>
/// Shows a priority the way the methodology names it: High → "високий".
/// </summary>
[ValueConversion(typeof(PriorityLevel), typeof(string))]
public sealed class PriorityLevelConverter : IValueConverter
{
    public static string GetName(PriorityLevel level) => level switch
    {
        PriorityLevel.Low => "низький",
        PriorityLevel.Medium => "середній",
        PriorityLevel.High => "високий",
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, "Unknown priority level."),
    };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is PriorityLevel level ? GetName(level) : DependencyProperty.UnsetValue;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("A priority level cannot be converted back.");
}
