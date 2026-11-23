using System.Globalization;
using System.Windows;
using System.Windows.Data;
using RiskManager.App.Converters;
using RiskManager.Core.Domain;

namespace RiskManager.Tests;

public class CategoryConverterTests
{
    [Theory]
    [InlineData(RiskCategory.Technical, "Технічні")]
    [InlineData(RiskCategory.Cost, "Вартісні")]
    [InlineData(RiskCategory.Schedule, "Планові")]
    [InlineData(RiskCategory.Management, "Управлінські")]
    public void GetName_ReturnsMethodologyName(RiskCategory category, string expected)
    {
        Assert.Equal(expected, CategoryNameConverter.GetName(category));
    }

    [Theory]
    [InlineData(RiskCategory.Technical, "tc")]
    [InlineData(RiskCategory.Cost, "cc")]
    [InlineData(RiskCategory.Schedule, "pc")]
    [InlineData(RiskCategory.Management, "mc")]
    public void GetSymbol_ReturnsFormulaSymbol(RiskCategory category, string expected)
    {
        Assert.Equal(expected, CategorySymbolConverter.GetSymbol(category));
    }

    [Fact]
    public void EveryCategory_HasDistinctNameAndSymbol()
    {
        // Guards against adding a fifth category and forgetting the converters:
        // GetName/GetSymbol would throw for it and this test would fail.
        RiskCategory[] categories = Enum.GetValues<RiskCategory>();

        Assert.Equal(categories.Length,
            categories.Select(CategoryNameConverter.GetName).Distinct().Count());
        Assert.Equal(categories.Length,
            categories.Select(CategorySymbolConverter.GetSymbol).Distinct().Count());
    }

    [Fact]
    public void Convert_Category_ReturnsText()
    {
        // This is exactly how a binding calls the converter.
        Assert.Equal("Вартісні", Convert(new CategoryNameConverter(), RiskCategory.Cost));
        Assert.Equal("cc", Convert(new CategorySymbolConverter(), RiskCategory.Cost));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Cost")]
    [InlineData(1)]
    public void Convert_NotACategory_ReturnsUnsetValue(object? value)
    {
        Assert.Same(DependencyProperty.UnsetValue, Convert(new CategoryNameConverter(), value));
        Assert.Same(DependencyProperty.UnsetValue, Convert(new CategorySymbolConverter(), value));
    }

    [Fact]
    public void ConvertBack_IsNotSupported()
    {
        IValueConverter name = new CategoryNameConverter();
        IValueConverter symbol = new CategorySymbolConverter();

        Assert.Throws<NotSupportedException>(() =>
            name.ConvertBack("Вартісні", typeof(RiskCategory), null, CultureInfo.InvariantCulture));
        Assert.Throws<NotSupportedException>(() =>
            symbol.ConvertBack("cc", typeof(RiskCategory), null, CultureInfo.InvariantCulture));
    }

    private static object? Convert(IValueConverter converter, object? value) =>
        converter.Convert(value, typeof(string), null, CultureInfo.InvariantCulture);
}
