using LiveChartsCore.Kernel;

namespace RiskManager.App.Views;

/// <summary>
/// Number formats of the charts. LiveCharts expects delegates, and XAML can take
/// them from here with x:Static.
/// </summary>
public static class ChartFormats
{
    public static Func<double, string> Percent { get; } = value => value.ToString("P0");

    public static Func<double, string> Integer { get; } = value => value.ToString("N0");

    public static Func<double, string> Decimal { get; } = value => value.ToString("N2");

    public static Func<ChartPoint, string> PercentLabel { get; } = point => point.Coordinate.PrimaryValue.ToString("P1");

    public static Func<ChartPoint, string> IntegerLabel { get; } = point => point.Coordinate.PrimaryValue.ToString("N0");

    public static Func<ChartPoint, string> DecimalLabel { get; } = point => point.Coordinate.PrimaryValue.ToString("N3");
}
