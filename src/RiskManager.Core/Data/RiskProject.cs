namespace RiskManager.Core.Data;

/// <summary>
/// Everything the user enters in one project — the content of a project file.
/// Calculated results are not stored: they are always calculated again, so a
/// file can never contain results that contradict its data.
/// </summary>
/// <param name="Version">The version of the file format.</param>
/// <param name="Sources">Codes of the checked risk sources (stage 1.1).</param>
/// <param name="Events">Codes of the checked risk events (stage 1.2).</param>
/// <param name="Risks">
/// The numbers of the risk events, including the unchecked ones that have them.
/// </param>
public sealed record RiskProject(
    int Version,
    IReadOnlyList<string> Sources,
    IReadOnlyList<string> Events,
    IReadOnlyList<RiskData> Risks)
{
    public const int CurrentVersion = 1;

    /// <summary>A project without any data.</summary>
    public static RiskProject Empty { get; } = new(CurrentVersion, [], [], []);
}

/// <summary>The data of one risk event.</summary>
/// <param name="Event">The code of the risk event.</param>
/// <param name="Estimates">One value per expert; null means an empty cell.</param>
/// <param name="Loss">lr.</param>
/// <param name="Measures">Codes of the marked measures (stage 3).</param>
/// <param name="Evaluations">epr and elr of the measures (stage 4), also of unmarked ones.</param>
public sealed record RiskData(
    string Event,
    IReadOnlyList<double?> Estimates,
    double? Loss,
    IReadOnlyList<string> Measures,
    IReadOnlyList<EvaluationData> Evaluations);

/// <summary>epr and elr of one measure of one risk.</summary>
public sealed record EvaluationData(string Measure, double? Probability, double? Loss);
