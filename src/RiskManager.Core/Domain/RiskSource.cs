namespace RiskManager.Core.Domain;

/// <summary>
/// A source of risks identified from the SRS (stage 1.1, set RS — 18 items).
/// </summary>
public sealed record RiskSource(string Code, RiskCategory Category, string Description)
    : ICategorizedItem;
