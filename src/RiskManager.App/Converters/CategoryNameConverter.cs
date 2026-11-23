using System.Globalization;
using System.Windows;
using System.Windows.Data;
using RiskManager.Core.Domain;

namespace RiskManager.App.Converters;

/// <summary>
/// Shows a risk category the way the methodology names it: Technical → "Технічні".
/// XAML: <c>{Binding Category, Converter={StaticResource CategoryNameConverter}}</c>.
/// </summary>
[ValueConversion(typeof(RiskCategory), typeof(string))]
public sealed class CategoryNameConverter : IValueConverter
{
    /// <summary>
    /// The mapping itself. It is public and static so that C# code can use it
    /// too (for example, the Excel export in a later iteration).
    /// </summary>
    public static string GetName(RiskCategory category) => category switch
    {
        RiskCategory.Technical => "Технічні",
        RiskCategory.Cost => "Вартісні",
        RiskCategory.Schedule => "Планові",
        RiskCategory.Management => "Управлінські",
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown risk category."),
    };

    // UnsetValue tells the binding "no value": it then shows FallbackValue
    // (or nothing) instead of crashing the window on an unexpected input.
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is RiskCategory category ? GetName(category) : DependencyProperty.UnsetValue;

    // The name is display-only: nobody edits it, so there is no way back.
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("A category name cannot be converted back.");
}
