using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RiskManager.Core.Calculations;
using RiskManager.Core.Domain;

namespace RiskManager.App.ViewModels;

/// <summary>
/// The "Monitoring" tab (stage 4): evaluates the candidate measures of every
/// risk, chooses the best ones and sets the priorities again by the rules of
/// stage 2.4.
/// </summary>
public partial class MonitoringViewModel : ObservableObject
{
    private readonly MeasuresViewModel _measures;
    private readonly Random _random;

    // Keeps the numbers of risks that are temporarily absent.
    private readonly Dictionary<RiskEvent, RiskMonitoringViewModel> _rowsByEvent = [];

    // The row the user clicked, as opposed to the one selected automatically.
    private RiskMonitoringViewModel? _userChoice;
    private bool _isUpdatingRows;

    /// <param name="measures">The "Measures" tab.</param>
    /// <param name="random">Tests pass a seeded generator to get repeatable numbers.</param>
    public MonitoringViewModel(MeasuresViewModel measures, Random? random = null)
    {
        ArgumentNullException.ThrowIfNull(measures);
        _measures = measures;
        _random = random ?? Random.Shared;
        _measures.PropertyChanged += OnMeasuresPropertyChanged;

        UpdateRows();
    }

    /// <summary>The risks that have at least one measure, the greatest first.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<RiskMonitoringViewModel> Rows { get; private set; }

    /// <summary>The risk whose measures are being evaluated.</summary>
    [ObservableProperty]
    public partial RiskMonitoringViewModel? SelectedRow { get; set; }

    /// <summary>
    /// The results of the completely evaluated risks and their new priorities:
    /// min, max and mpr are calculated from evr this time.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CompleteCount))]
    [NotifyPropertyChangedFor(nameof(TotalBefore))]
    [NotifyPropertyChangedFor(nameof(TotalAfter))]
    [NotifyPropertyChangedFor(nameof(TotalReductionRatio))]
    public partial PrioritizationResult<MitigationResult> Results { get; private set; }

    /// <summary>How many analysed risks have no result: no measures or no numbers.</summary>
    [ObservableProperty]
    public partial int MissingCount { get; private set; }

    public int CompleteCount => Results.Items.Count;

    /// <summary>The sum of vr of the evaluated risks, i.e. comparable with <see cref="TotalAfter"/>.</summary>
    public double TotalBefore => Results.Items.Sum(item => item.Item.Risk.Value);

    /// <summary>The sum of evr of the evaluated risks.</summary>
    public double TotalAfter => Results.Items.Sum(item => item.Value);

    public double TotalReductionRatio => TotalBefore > 0 ? (TotalBefore - TotalAfter) / TotalBefore : 0;

    /// <summary>Fills the empty cells of all candidates with demonstration data.</summary>
    [RelayCommand(CanExecute = nameof(CanFillRandom))]
    private void FillRandom()
    {
        foreach (RiskMonitoringViewModel row in Rows)
        {
            row.FillEmpty(_random);
        }
    }

    private bool CanFillRandom() => Rows.Any(row => !row.IsComplete);

    private void OnMeasuresPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MeasuresViewModel.PlannedRows))
        {
            UpdateRows();
        }
    }

    [MemberNotNull(nameof(Rows), nameof(Results))]
    private void UpdateRows()
    {
        // Until the rows are complete, their notifications are ignored; the
        // selection that the table changes by itself is ignored as well.
        _isUpdatingRows = true;

        Rows = _measures.PlannedRows.Select(GetRow).ToList();

        SelectedRow = _userChoice is not null && Rows.Contains(_userChoice)
            ? _userChoice
            : Rows.FirstOrDefault();

        _isUpdatingRows = false;

        Recalculate();
    }

    private RiskMonitoringViewModel GetRow(RiskMeasuresViewModel source)
    {
        RiskEvent riskEvent = source.Ranked.Risk.Event;

        if (_rowsByEvent.TryGetValue(riskEvent, out RiskMonitoringViewModel? row))
        {
            row.Update(source.Ranked, source.SelectedMeasures);
        }
        else
        {
            row = new RiskMonitoringViewModel(source.Ranked, source.SelectedMeasures);
            row.PropertyChanged += OnRowPropertyChanged;
            _rowsByEvent.Add(riskEvent, row);
        }

        return row;
    }

    partial void OnSelectedRowChanged(RiskMonitoringViewModel? value)
    {
        if (!_isUpdatingRows)
        {
            _userChoice = value;
        }
    }

    private void OnRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(RiskMonitoringViewModel.Result) && !_isUpdatingRows)
        {
            Recalculate();
        }
    }

    [MemberNotNull(nameof(Results))]
    private void Recalculate()
    {
        List<MitigationResult> results = Rows
            .Select(row => row.Result)
            .OfType<MitigationResult>()
            .ToList();

        PrioritizationResult<MitigationResult> prioritized =
            Prioritization.Prioritize(results, result => result.Chosen.Value);

        Dictionary<RiskEvent, PriorityLevel> priorities = prioritized.Items
            .ToDictionary(item => item.Item.Risk.Event, item => item.Priority);

        foreach (RiskMonitoringViewModel row in Rows)
        {
            row.PriorityAfter = priorities.TryGetValue(row.Ranked.Risk.Event, out PriorityLevel priority)
                ? priority
                : null;
        }

        // Set last: whoever reacts to Results must see the rows up to date.
        MissingCount = _measures.Rows.Count - results.Count;
        Results = prioritized;

        FillRandomCommand.NotifyCanExecuteChanged();
    }
}
