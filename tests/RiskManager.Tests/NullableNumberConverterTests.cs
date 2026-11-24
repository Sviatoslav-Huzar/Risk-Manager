using System.Globalization;
using System.Windows;
using RiskManager.App.Converters;

namespace RiskManager.Tests;

public class NullableNumberConverterTests
{
    private static readonly CultureInfo Ukrainian = CultureInfo.GetCultureInfo("uk-UA");

    private readonly NullableNumberConverter _converter = new();

    [Theory]
    [InlineData(null, "")]
    [InlineData(0.0, "0")]
    [InlineData(0.5, "0,5")]
    [InlineData(0.125, "0,125")]
    public void Convert_ShowsNumberInUkrainianFormat(double? value, string expected)
    {
        Assert.Equal(expected, _converter.Convert(value, typeof(string), null, Ukrainian));
    }

    [Fact]
    public void Convert_NotANumber_ReturnsUnsetValue()
    {
        Assert.Same(DependencyProperty.UnsetValue, _converter.Convert("text", typeof(string), null, Ukrainian));
    }

    [Theory]
    [InlineData("0,5", 0.5)]
    [InlineData("0.5", 0.5)]
    [InlineData(" 0,25 ", 0.25)]
    [InlineData("1", 1.0)]
    [InlineData("-0,1", -0.1)] // out of range, but that is for the view model to decide
    public void ConvertBack_Number(string text, double expected)
    {
        Assert.Equal(expected, ConvertBack(text));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ConvertBack_EmptyText_ReturnsNull(string? text)
    {
        Assert.Null(ConvertBack(text));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("0,5x")]
    [InlineData("1.000,5")]
    public void ConvertBack_NotANumber_ReturnsUnsetValue(string text)
    {
        Assert.Same(DependencyProperty.UnsetValue, ConvertBack(text));
    }

    [Fact]
    public void ConvertBack_HugeNumber_ReturnsUnsetValue()
    {
        // 400 digits do not fit into a double and become infinity.
        Assert.Same(DependencyProperty.UnsetValue, ConvertBack(new string('9', 400)));
    }

    private object? ConvertBack(string? text) =>
        _converter.ConvertBack(text, typeof(double?), null, Ukrainian);
}
