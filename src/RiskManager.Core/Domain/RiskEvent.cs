namespace RiskManager.Core.Domain;

/// <summary>
/// A potential risk event of a software project (stage 1.2, set RP — 46 items).
/// </summary>
public sealed record RiskEvent(string Code, RiskCategory Category, string Description)
    : ICategorizedItem;
