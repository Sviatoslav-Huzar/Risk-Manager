using System.Globalization;
using System.Windows;
using RiskManager.App.Converters;
using RiskManager.Core.Domain;

namespace RiskManager.Tests;

public class ProbabilityLevelConverterTests
{
    private readonly ProbabilityLevelConverter _converter = new();

    [Theory]
    [InlineData(ProbabilityLevel.VeryLow, "дуже низька")]
    [InlineData(ProbabilityLevel.Low, "низька")]
    [InlineData(ProbabilityLevel.Medium, "середня")]
    [InlineData(ProbabilityLevel.High, "висока")]
    [InlineData(ProbabilityLevel.VeryHigh, "дуже висока")]
    public void Convert_Level_ReturnsMethodologyName(ProbabilityLevel level, string expected)
    {
        Assert.Equal(expected, Convert(level));
    }

    [Fact]
    public void EveryLevel_HasDistinctName()
    {
        ProbabilityLevel[] levels = Enum.GetValues<ProbabilityLevel>();

        Assert.Equal(levels.Length, levels.Select(ProbabilityLevelConverter.GetName).Distinct().Count());
    }

    [Fact]
    public void Convert_Null_ReturnsEmptyText()
    {
        Assert.Equal(string.Empty, Convert(null));
    }

    [Fact]
    public void Convert_NotALevel_ReturnsUnsetValue()
    {
        Assert.Same(DependencyProperty.UnsetValue, Convert("Medium"));
    }

    [Fact]
    public void ConvertBack_IsNotSupported()
    {
        Assert.Throws<NotSupportedException>(() =>
            _converter.ConvertBack("середня", typeof(ProbabilityLevel), null, CultureInfo.InvariantCulture));
    }

    private object? Convert(object? value) =>
        _converter.Convert(value, typeof(string), null, CultureInfo.InvariantCulture);
}
