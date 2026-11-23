using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using RiskManager.Core.Domain;

namespace RiskManager.App.ViewModels;

/// <summary>
/// One risk of the "Measures" tab together with its own list of 19 check boxes.
/// </summary>
public partial class RiskMeasuresViewModel : ObservableObject
{
    public RiskMeasuresViewModel(RankedRisk ranked, IReadOnlyList<Measure> measures)
    {
        ArgumentNullException.ThrowIfNull(ranked);
        ArgumentNullException.ThrowIfNull(measures);

        Ranked = ranked;
        Items = measures.Select(measure => new MeasureItemViewModel(measure)).ToList();
        SelectedMeasures = [];

        foreach (MeasureItemViewModel item in Items)
        {
            item.PropertyChanged += OnItemPropertyChanged;
        }
    }

    /// <summary>
    /// The current rank, value and priority. The estimates can change at any
    /// time, so <see cref="MeasuresViewModel"/> replaces this snapshot, while
    /// the check boxes below stay as they are.
    /// </summary>
    [ObservableProperty]
    public partial RankedRisk Ranked { get; internal set; }

    /// <summary>All measures of the catalog.</summary>
    public IReadOnlyList<MeasureItemViewModel> Items { get; }

    /// <summary>The candidates for the best measure, in catalog order.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMeasures))]
    [NotifyPropertyChangedFor(nameof(SelectedCodes))]
    public partial IReadOnlyList<Measure> SelectedMeasures { get; private set; }

    public bool HasMeasures => SelectedMeasures.Count > 0;

    /// <summary>The candidates as one string for the table, e.g. "ev1, ev7".</summary>
    public string SelectedCodes => string.Join(", ", SelectedMeasures.Select(measure => measure.Code));

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MeasureItemViewModel.IsSelected))
        {
            SelectedMeasures = Items
                .Where(item => item.IsSelected)
                .Select(item => item.Measure)
                .ToList();
        }
    }
}
