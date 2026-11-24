using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using RiskManager.Core.Calculations;
using RiskManager.Core.Domain;

namespace RiskManager.App.ViewModels;

/// <summary>
/// One risk of the "Monitoring" tab with its candidate measures and the best of
/// them. The best one is chosen only after all candidates are evaluated:
/// otherwise a measure could win just because the others are not filled in.
/// </summary>
public partial class RiskMonitoringViewModel : ObservableObject
{
    // Keeps the numbers of measures that are temporarily not marked.
    private readonly Dictionary<Measure, MeasureEvaluationViewModel> _evaluationsByMeasure = [];

    public RiskMonitoringViewModel(RankedRisk ranked, IReadOnlyList<Measure> measures)
    {
        ArgumentNullException.ThrowIfNull(ranked);
        ArgumentNullException.ThrowIfNull(measures);

        Ranked = ranked;
        Evaluations = [];
        Update(ranked, measures);
    }

    /// <summary>The risk before the measures.</summary>
    [ObservableProperty]
    public partial RankedRisk Ranked { get; private set; }

    /// <summary>One row per candidate, in catalog order.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<MeasureEvaluationViewModel> Evaluations { get; private set; }

    /// <summary>The best measure and its effect; null while something is not evaluated.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsComplete))]
    [NotifyPropertyChangedFor(nameof(ValueAfter))]
    [NotifyPropertyChangedFor(nameof(ReductionRatio))]
    [NotifyPropertyChangedFor(nameof(BestCode))]
    public partial MitigationResult? Result { get; private set; }

    public bool IsComplete => Result is not null;

    /// <summary>evr of the best measure.</summary>
    public double? ValueAfter => Result?.Chosen.Value;

    /// <summary>The share by which vr became smaller; negative if it grew.</summary>
    public double? ReductionRatio => Result?.ReductionRatio;

    public string BestCode => Result?.Chosen.Measure.Code ?? string.Empty;

    /// <summary>The priority after the measures, set by <see cref="MonitoringViewModel"/>.</summary>
    [ObservableProperty]
    public partial PriorityLevel? PriorityAfter { get; internal set; }

    /// <summary>Takes over the current risk value and the current candidates.</summary>
    internal void Update(RankedRisk ranked, IReadOnlyList<Measure> measures)
    {
        Ranked = ranked;

        // The same candidates keep the same list, so the table is not rebuilt.
        if (!Evaluations.Select(evaluation => evaluation.Measure).SequenceEqual(measures))
        {
            Evaluations = measures.Select(GetEvaluation).ToList();
        }

        Recalculate();
    }

    internal void FillEmpty(Random random)
    {
        foreach (MeasureEvaluationViewModel evaluation in Evaluations)
        {
            evaluation.FillEmpty(Ranked.Risk, random);
        }
    }

    private MeasureEvaluationViewModel GetEvaluation(Measure measure)
    {
        if (!_evaluationsByMeasure.TryGetValue(measure, out MeasureEvaluationViewModel? evaluation))
        {
            evaluation = new MeasureEvaluationViewModel(measure);
            evaluation.PropertyChanged += OnEvaluationPropertyChanged;
            _evaluationsByMeasure.Add(measure, evaluation);
        }

        return evaluation;
    }

    private void OnEvaluationPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MeasureEvaluationViewModel.Option))
        {
            Recalculate();
        }
    }

    private void Recalculate()
    {
        List<MeasureOption> options = Evaluations
            .Select(evaluation => evaluation.Option)
            .OfType<MeasureOption>()
            .ToList();

        Result = options.Count > 0 && options.Count == Evaluations.Count
            ? Mitigation.ChooseBest(Ranked.Risk, options)
            : null;

        foreach (MeasureEvaluationViewModel evaluation in Evaluations)
        {
            evaluation.IsBest = Result is not null && evaluation.Measure == Result.Chosen.Measure;
        }
    }
}
