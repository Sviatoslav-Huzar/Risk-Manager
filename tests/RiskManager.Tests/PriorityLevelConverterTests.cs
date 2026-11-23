using System.Globalization;
using System.Windows;
using RiskManager.App.Converters;
using RiskManager.Core.Domain;

namespace RiskManager.Tests;

public class PriorityLevelConverterTests
{
    private readonly PriorityLevelConverter _converter = new();

    [Theory]
    [InlineData(PriorityLevel.Low, "низький")]
    [InlineData(PriorityLevel.Medium, "середній")]
    [InlineData(PriorityLevel.High, "високий")]
    public void Convert_Level_ReturnsMethodologyName(PriorityLevel level, string expected)
    {
        Assert.Equal(expected, Convert(level));
    }

    [Fact]
    public void EveryLevel_HasName()
    {
        Assert.All(Enum.GetValues<PriorityLevel>(), level => Assert.NotEmpty(PriorityLevelConverter.GetName(level)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("High")]
    public void Convert_NotALevel_ReturnsUnsetValue(object? value)
    {
        Assert.Same(DependencyProperty.UnsetValue, Convert(value));
    }

    [Fact]
    public void ConvertBack_IsNotSupported()
    {
        Assert.Throws<NotSupportedException>(() =>
            _converter.ConvertBack("високий", typeof(PriorityLevel), null, CultureInfo.InvariantCulture));
    }

    private object? Convert(object? value) =>
        _converter.Convert(value, typeof(string), null, CultureInfo.InvariantCulture);
}
