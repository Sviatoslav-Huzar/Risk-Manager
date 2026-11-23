using System.Globalization;
using System.Windows;
using System.Windows.Data;
using RiskManager.Core.Domain;

namespace RiskManager.App.Converters;

/// <summary>
/// Shows the symbol a category share has in formulas (5.2)-(5.3):
/// Technical → "tc", Cost → "cc", Schedule → "pc", Management → "mc".
/// </summary>
[ValueConversion(typeof(RiskCategory), typeof(string))]
public sealed class CategorySymbolConverter : IValueConverter
{
    public static string GetSymbol(RiskCategory category) => category switch
    {
        RiskCategory.Technical => "tc",
        RiskCategory.Cost => "cc",
        RiskCategory.Schedule => "pc",
        RiskCategory.Management => "mc",
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown risk category."),
    };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is RiskCategory category ? GetSymbol(category) : DependencyProperty.UnsetValue;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("A category symbol cannot be converted back.");
}
