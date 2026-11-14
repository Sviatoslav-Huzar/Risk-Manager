namespace RiskManager.Core.Domain;

/// <summary>
/// Qualitative level of a risk event probability (stage 2.1).
/// </summary>
public enum ProbabilityLevel
{
    /// <summary>p &lt; 0.10</summary>
    VeryLow,

    /// <summary>0.10 &lt;= p &lt; 0.25</summary>
    Low,

    /// <summary>0.25 &lt;= p &lt; 0.50</summary>
    Medium,

    /// <summary>0.50 &lt;= p &lt; 0.75</summary>
    High,

    /// <summary>p &gt;= 0.75</summary>
    VeryHigh,
}
