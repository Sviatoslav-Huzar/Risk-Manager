namespace RiskManager.Core.Domain;

/// <summary>
/// The reference data of the risk management model: all risk sources,
/// risk events and mitigation measures.
/// </summary>
public sealed record RiskCatalog(
    IReadOnlyList<RiskSource> Sources,
    IReadOnlyList<RiskEvent> Events,
    IReadOnlyList<Measure> Measures);
