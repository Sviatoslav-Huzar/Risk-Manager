using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using RiskManager.Core.Calculations;
using RiskManager.Core.Data;
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

    // The numbers of a project file wait here until their measure is marked.
    private readonly Dictionary<string, EvaluationData> _saved;

    /// <param name="ranked">The analysed risk.</param>
    /// <param name="measures">The marked measures.</param>
    /// <param name="saved">The numbers of a project file.</param>
    public RiskMonitoringViewModel(
        RankedRisk ranked,
        IReadOnlyList<Measure> measures,
        IReadOnlyCollection<EvaluationData>? saved = null)
    {
        ArgumentNullException.ThrowIfNull(ranked);
        ArgumentNullException.ThrowIfNull(measures);

        _saved = (saved ?? []).ToDictionary(evaluation => evaluation.Measure);
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

    /// <summary>The numbers of all measures, also of those that are not marked now.</summary>
    internal IReadOnlyList<EvaluationData> GetAllData()
    {
        IEnumerable<EvaluationData> entered = _evaluationsByMeasure.Values
            .Select(evaluation => new EvaluationData(evaluation.Code, evaluation.Probability, evaluation.Loss));

        // The entered numbers come first: they replace the saved ones.
        return entered
            .Concat(_saved.Values)
            .DistinctBy(evaluation => evaluation.Measure)
            .ToList();
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

            if (_saved.TryGetValue(measure.Code, out EvaluationData? data))
            {
                evaluation.Restore(data);
            }

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
