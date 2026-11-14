using RiskManager.Core.Domain;

namespace RiskManager.Core.Calculations;

/// <summary>
/// One analysed risk: a risk event with its probability er and possible loss lr
/// (stages 2.1-2.3).
/// </summary>
public sealed record RiskAssessment
{
    public RiskAssessment(RiskEvent riskEvent, double probability, double loss)
    {
        ArgumentNullException.ThrowIfNull(riskEvent);
        Guard.InUnitInterval(probability, nameof(probability));
        Guard.InUnitInterval(loss, nameof(loss));

        Event = riskEvent;
        Probability = probability;
        Loss = loss;
    }

    public RiskEvent Event { get; }

    /// <summary>er — probability of the event, in [0; 1].</summary>
    public double Probability { get; }

    /// <summary>lr — possible loss if the event occurs, in [0; 1].</summary>
    public double Loss { get; }

    /// <summary>vr = er * lr — the expected loss, i.e. the risk value (formula 5.8).</summary>
    public double Value => Probability * Loss;

    public ProbabilityLevel ProbabilityLevel => Analysis.ClassifyProbability(Probability);
}
