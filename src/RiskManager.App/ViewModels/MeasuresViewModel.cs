using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using RiskManager.Core.Domain;

namespace RiskManager.App.ViewModels;

/// <summary>
/// The "Measures" tab (stage 3): for every analysed risk the user marks the
/// measures that can reduce or eliminate it. The methodology expects one most
/// suitable measure per risk; it is chosen among these candidates on the
/// "Monitoring" tab, where they are evaluated.
/// </summary>
public partial class MeasuresViewModel : ObservableObject
{
    private readonly AnalysisViewModel _analysis;
    private readonly IReadOnlyList<Measure> _measures;

    // Keeps the marks of risks that are temporarily absent from the analysis.
    private readonly Dictionary<RiskEvent, RiskMeasuresViewModel> _rowsByEvent = [];

    // The row the user clicked, as opposed to the one selected automatically.
    private RiskMeasuresViewModel? _userChoice;
    private bool _isUpdatingRows;

    public MeasuresViewModel(AnalysisViewModel analysis, IReadOnlyList<Measure> measures)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        ArgumentNullException.ThrowIfNull(measures);

        _analysis = analysis;
        _measures = measures;
        _analysis.PropertyChanged += OnAnalysisPropertyChanged;

        UpdateRows();
    }

    /// <summary>
    /// The analysed risks, the greatest first. The list is replaced after every
    /// analysis, but the rows in it are reused, so no marks are lost.
    /// </summary>
    [ObservableProperty]
    public partial IReadOnlyList<RiskMeasuresViewModel> Rows { get; private set; }

    /// <summary>The risk whose measures are being edited; null if there are no risks.</summary>
    [ObservableProperty]
    public partial RiskMeasuresViewModel? SelectedRow { get; set; }

    /// <summary>How many risks have at least one measure.</summary>
    [ObservableProperty]
    public partial int PlannedCount { get; private set; }

    private void OnAnalysisPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AnalysisViewModel.Risks))
        {
            UpdateRows();
        }
    }

    [MemberNotNull(nameof(Rows))]
    private void UpdateRows()
    {
        // While the list is being replaced, the table may change the selection
        // by itself; that is not a choice of the user.
        _isUpdatingRows = true;

        Rows = _analysis.Risks.Select(GetRow).ToList();

        // Until the user clicks a row, the greatest risk is selected.
        SelectedRow = _userChoice is not null && Rows.Contains(_userChoice)
            ? _userChoice
            : Rows.FirstOrDefault();

        _isUpdatingRows = false;

        UpdatePlannedCount();
    }

    partial void OnSelectedRowChanged(RiskMeasuresViewModel? value)
    {
        if (!_isUpdatingRows)
        {
            _userChoice = value;
        }
    }

    private RiskMeasuresViewModel GetRow(RankedRisk ranked)
    {
        RiskEvent riskEvent = ranked.Risk.Event;

        if (_rowsByEvent.TryGetValue(riskEvent, out RiskMeasuresViewModel? row))
        {
            row.Ranked = ranked;
        }
        else
        {
            row = new RiskMeasuresViewModel(ranked, _measures);
            row.PropertyChanged += OnRowPropertyChanged;
            _rowsByEvent.Add(riskEvent, row);
        }

        return row;
    }

    private void OnRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(RiskMeasuresViewModel.HasMeasures))
        {
            UpdatePlannedCount();
        }
    }

    private void UpdatePlannedCount()
    {
        PlannedCount = Rows.Count(row => row.HasMeasures);
    }
}
