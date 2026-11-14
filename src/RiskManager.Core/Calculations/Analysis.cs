using RiskManager.Core.Domain;

namespace RiskManager.Core.Calculations;

/// <summary>
/// Stage 2.1: probabilities of risk events and their classification.
/// </summary>
public static class Analysis
{
    /// <summary>
    /// er_i = average of the experts' estimates for one risk event (formula 5.5).
    /// </summary>
    /// <param name="expertEstimates">Estimates in [0; 1]; the methodology uses 10 experts.</param>
    public static double EventProbability(IReadOnlyCollection<double> expertEstimates)
    {
        ArgumentNullException.ThrowIfNull(expertEstimates);

        if (expertEstimates.Count == 0)
        {
            throw new ArgumentException("At least one expert estimate is required.", nameof(expertEstimates));
        }

        foreach (double estimate in expertEstimates)
        {
            Guard.InUnitInterval(estimate, nameof(expertEstimates));
        }

        return expertEstimates.Average();
    }

    /// <summary>
    /// Maps a probability to one of five levels. The left boundary of each
    /// interval belongs to it: 0.25 is Medium, not Low.
    /// </summary>
    public static ProbabilityLevel ClassifyProbability(double probability)
    {
        Guard.InUnitInterval(probability, nameof(probability));

        return Guard.Normalize(probability) switch
        {
            < 0.10 => ProbabilityLevel.VeryLow,
            < 0.25 => ProbabilityLevel.Low,
            < 0.50 => ProbabilityLevel.Medium,
            < 0.75 => ProbabilityLevel.High,
            _ => ProbabilityLevel.VeryHigh,
        };
    }

    /// <summary>
    /// Probabilities of the four risk groups (formula 5.6) plus the mean
    /// probability within each group.
    /// </summary>
    public static IReadOnlyList<CategoryProbability> CategoryProbabilities(
        IReadOnlyCollection<RiskAssessment> risks)
    {
        ArgumentNullException.ThrowIfNull(risks);

        double total = risks.Sum(r => r.Probability);

        return Enum.GetValues<RiskCategory>()
            .Select(category =>
            {
                List<double> probabilities = risks
                    .Where(r => r.Event.Category == category)
                    .Select(r => r.Probability)
                    .ToList();

                double sum = probabilities.Sum();
                double share = total > 0 ? sum / total : 0;
                double mean = probabilities.Count > 0 ? probabilities.Average() : 0;

                return new CategoryProbability(category, probabilities.Count, share, mean);
            })
            .ToList();
    }
}

/// <summary>Probability characteristics of one risk group.</summary>
/// <param name="EventCount">Number of analysed events in the group.</param>
/// <param name="Share">
/// Formula (5.6): sum of er in the group / sum of all er. The four shares add up to 1,
/// so they show the structure of the risks, not their absolute probability.
/// </param>
/// <param name="Mean">Average er in the group; this is the value to classify.</param>
public sealed record CategoryProbability(
    RiskCategory Category,
    int EventCount,
    double Share,
    double Mean)
{
    public ProbabilityLevel MeanLevel => Analysis.ClassifyProbability(Mean);
}
