namespace RiskManager.Core.Domain;

/// <summary>
/// Any entry of the reference catalog: it has a short code (e.g. "t1", "ev5")
/// and a human-readable description.
/// </summary>
public interface ICatalogItem
{
    string Code { get; }
    string Description { get; }
}

/// <summary>
/// A catalog entry that belongs to one of the risk categories.
/// Implemented by both risk sources and risk events, so the same
/// calculations can work with either of them.
/// </summary>
public interface ICategorizedItem : ICatalogItem
{
    RiskCategory Category { get; }
}
