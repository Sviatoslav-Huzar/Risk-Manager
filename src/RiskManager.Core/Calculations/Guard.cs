namespace RiskManager.Core.Calculations;

/// <summary>
/// Shared argument checks and numeric helpers for the calculations.
/// </summary>
internal static class Guard
{
    /// <summary>
    /// Values are compared after rounding to this many decimal places,
    /// so binary floating-point noise (0.1 + 0.2 = 0.30000000000000004)
    /// cannot push a value over an interval boundary.
    /// </summary>
    public const int ComparisonDecimals = 9;

    /// <summary>Throws if the value is NaN or outside [0; 1].</summary>
    public static void InUnitInterval(double value, string paramName)
    {
        if (double.IsNaN(value) || value < 0 || value > 1)
        {
            throw new ArgumentOutOfRangeException(paramName, value, "Value must be within [0; 1].");
        }
    }

    public static double Normalize(double value) => Math.Round(value, ComparisonDecimals);
}
