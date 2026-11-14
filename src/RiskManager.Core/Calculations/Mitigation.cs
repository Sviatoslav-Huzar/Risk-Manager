using RiskManager.Core.Domain;

namespace RiskManager.Core.Calculations;

/// <summary>
/// A candidate measure for one risk with the expected probability epr and loss elr
/// after the measure is applied (stage 4.1, formulas 5.14-5.15).
/// </summary>
public sealed record MeasureOption
{
    public MeasureOption(Measure measure, double probability, double loss)
    {
        ArgumentNullException.ThrowIfNull(measure);
        Guard.InUnitInterval(probability, nameof(probability));
        Guard.InUnitInterval(loss, nameof(loss));

        Measure = measure;
        Probability = probability;
        Loss = loss;
    }

    public Measure Measure { get; }

    /// <summary>epr — probability after the measure.</summary>
    public double Probability { get; }

    /// <summary>elr — loss after the measure.</summary>
    public double Loss { get; }

    /// <summary>evr = epr * elr — risk value after the measure (formula 5.16).</summary>
    public double Value => Probability * Loss;
}

/// <summary>A risk together with the measure chosen for it.</summary>
public sealed record MitigationResult(RiskAssessment Risk, MeasureOption Chosen)
{
    /// <summary>vr - evr. Negative if the measure makes things worse.</summary>
    public double Reduction => Risk.Value - Chosen.Value;

    /// <summary>Reduction relative to the original value, in [-inf; 1]; 0 when vr = 0.</summary>
    public double ReductionRatio => Risk.Value > 0 ? Reduction / Risk.Value : 0;
}

/// <summary>
/// Stages 3-4: choosing a measure for each risk and re-evaluating the risk.
/// </summary>
public static class Mitigation
{
    /// <summary>
    /// Picks the option with the smallest risk value after the measure.
    /// On a tie the option listed first wins.
    /// </summary>
    public static MitigationResult ChooseBest(
        RiskAssessment risk,
        IReadOnlyCollection<MeasureOption> options)
    {
        ArgumentNullException.ThrowIfNull(risk);
        ArgumentNullException.ThrowIfNull(options);

        if (options.Count == 0)
        {
            throw new ArgumentException("At least one measure option is required.", nameof(options));
        }

        MeasureOption best = options.MinBy(o => Guard.Normalize(o.Value))!;
        return new MitigationResult(risk, best);
    }
}
