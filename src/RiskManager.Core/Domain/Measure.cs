namespace RiskManager.Core.Domain;

/// <summary>
/// A measure that reduces or eliminates a risk (stage 3.1, set EV — 19 items).
/// </summary>
public sealed record Measure(string Code, string Description) : ICatalogItem;
