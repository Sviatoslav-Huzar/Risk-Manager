namespace RiskManager.Core.Domain;

/// <summary>
/// Mitigation priority of a risk (stage 2.4): sets LER, MER and HER of the methodology.
/// </summary>
public enum PriorityLevel
{
    /// <summary>Third-order risks (set LER).</summary>
    Low,

    /// <summary>Second-order risks (set MER).</summary>
    Medium,

    /// <summary>First-order risks (set HER).</summary>
    High,
}
