using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using RiskManager.Core.Data;
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

    // The marks of a project file. A risk that is not analysed yet has no row,
    // so its marks wait here until it gets one.
    private readonly Dictionary<string, IReadOnlyList<string>> _savedMarks;

    // The row the user clicked, as opposed to the one selected automatically.
    private RiskMeasuresViewModel? _userChoice;
    private bool _isUpdatingRows;

    /// <param name="analysis">The "Analysis" tab.</param>
    /// <param name="measures">All measures of the catalog.</param>
    /// <param name="saved">The marks of a project file.</param>
    public MeasuresViewModel(
        AnalysisViewModel analysis,
        IReadOnlyList<Measure> measures,
        IReadOnlyCollection<RiskData>? saved = null)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        ArgumentNullException.ThrowIfNull(measures);

        _analysis = analysis;
        _measures = measures;
        _savedMarks = (saved ?? []).ToDictionary(risk => risk.Event, risk => risk.Measures);
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

    /// <summary>
    /// The risks that have at least one measure. Replaced after every analysis
    /// and every marked check box, so the "Monitoring" tab follows only this list.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PlannedCount))]
    public partial IReadOnlyList<RiskMeasuresViewModel> PlannedRows { get; private set; }

    /// <summary>How many risks have at least one measure.</summary>
    public int PlannedCount => PlannedRows.Count;

    /// <summary>Codes of the measures marked for the risk, even if it has no row at the moment.</summary>
    public IReadOnlyList<string> GetMarkedCodes(RiskEvent riskEvent)
    {
        ArgumentNullException.ThrowIfNull(riskEvent);

        return _rowsByEvent.TryGetValue(riskEvent, out RiskMeasuresViewModel? row)
            ? row.SelectedMeasures.Select(measure => measure.Code).ToList()
            : _savedMarks.GetValueOrDefault(riskEvent.Code, []);
    }

    private void OnAnalysisPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AnalysisViewModel.Risks))
        {
            UpdateRows();
        }
    }

    [MemberNotNull(nameof(Rows), nameof(PlannedRows))]
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

        UpdatePlannedRows();
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
            row = new RiskMeasuresViewModel(ranked, _measures, GetMarkedCodes(riskEvent));
            row.PropertyChanged += OnRowPropertyChanged;
            _rowsByEvent.Add(riskEvent, row);
        }

        return row;
    }

    private void OnRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(RiskMeasuresViewModel.SelectedMeasures))
        {
            UpdatePlannedRows();
        }
    }

    [MemberNotNull(nameof(PlannedRows))]
    private void UpdatePlannedRows()
    {
        PlannedRows = Rows.Where(row => row.HasMeasures).ToList();
    }
}
