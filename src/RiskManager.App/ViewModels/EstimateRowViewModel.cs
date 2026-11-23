using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using RiskManager.Core.Calculations;
using RiskManager.Core.Domain;

namespace RiskManager.App.ViewModels;

/// <summary>
/// One row of table 5.1: a risk event, the estimates of ten experts, the
/// possible loss lr and the results calculated from them (stages 2.1-2.3).
/// </summary>
public partial class EstimateRowViewModel : ObservableValidator
{
    /// <summary>The number of experts in table 5.1.</summary>
    public const int ExpertCount = 10;

    public EstimateRowViewModel(RiskEvent riskEvent)
    {
        ArgumentNullException.ThrowIfNull(riskEvent);
        Event = riskEvent;

        Estimates = Enumerable.Range(0, ExpertCount)
            .Select(_ => new ExpertEstimateViewModel())
            .ToList();

        foreach (ExpertEstimateViewModel estimate in Estimates)
        {
            estimate.PropertyChanged += OnEstimatePropertyChanged;
        }
    }

    public RiskEvent Event { get; }

    public string Code => Event.Code;

    public string Description => Event.Description;

    public RiskCategory Category => Event.Category;

    /// <summary>Exactly <see cref="ExpertCount"/> cells, one per expert.</summary>
    public IReadOnlyList<ExpertEstimateViewModel> Estimates { get; }

    /// <summary>lr — the possible loss in [0; 1]; null means "not entered yet".</summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(0.0, 1.0, ErrorMessage = "Збитки мають бути в межах від 0 до 1")]
    public partial double? Loss { get; set; }

    /// <summary>
    /// The only stored result; null while the row is incomplete or has errors.
    /// Everything below is derived from it, so er, vr and the level can never
    /// get out of sync with each other.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsComplete))]
    [NotifyPropertyChangedFor(nameof(Probability))]
    [NotifyPropertyChangedFor(nameof(Value))]
    [NotifyPropertyChangedFor(nameof(ProbabilityLevel))]
    public partial RiskAssessment? Assessment { get; private set; }

    public bool IsComplete => Assessment is not null;

    /// <summary>er — the average of the experts' estimates (formula 5.5).</summary>
    public double? Probability => Assessment?.Probability;

    /// <summary>vr = er * lr (formula 5.8).</summary>
    public double? Value => Assessment?.Value;

    public ProbabilityLevel? ProbabilityLevel => Assessment?.ProbabilityLevel;

    private void OnEstimatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ExpertEstimateViewModel.Value))
        {
            Recalculate();
        }
    }

    // A hook of the generated Loss setter. The setter validates the value
    // before calling it, so HasErrors is already up to date here.
    partial void OnLossChanged(double? value) => Recalculate();

    private void Recalculate()
    {
        Assessment = TryAssess();
    }

    private RiskAssessment? TryAssess()
    {
        // Core throws on values outside [0; 1], so errors are checked first.
        if (HasErrors || Loss is not double loss || Estimates.Any(e => e.HasErrors || e.Value is null))
        {
            return null;
        }

        List<double> estimates = Estimates.Select(e => e.Value!.Value).ToList();

        return new RiskAssessment(Event, Analysis.EventProbability(estimates), loss);
    }
}
