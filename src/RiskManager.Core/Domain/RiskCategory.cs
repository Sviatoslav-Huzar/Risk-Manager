namespace RiskManager.Core.Domain;

/// <summary>
/// The four risk groups of the SEI-based model: technical, cost, schedule
/// and project management processes.
/// </summary>
public enum RiskCategory
{
    Technical,
    Cost,
    Schedule,
    Management,
}
