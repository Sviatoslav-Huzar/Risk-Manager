using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RiskManager.Core.Domain;

namespace RiskManager.App.ViewModels;

/// <summary>
/// The "Expert estimates" tab (stages 2.1-2.3). Shows one row per risk event
/// that is checked on the "Risk events" tab and follows its changes.
/// </summary>
public partial class EstimatesViewModel : ObservableObject
{
    private readonly ChecklistViewModel _events;

    // Rows exist for all events from the start, whether visible or not:
    // unchecking an event hides its row but keeps the entered numbers.
    private readonly Dictionary<ChecklistItemViewModel, EstimateRowViewModel> _rowsByItem = [];

    private readonly Random _random;

    /// <param name="events">The checklist of the "Risk events" tab.</param>
    /// <param name="random">Tests pass a seeded generator to get repeatable numbers.</param>
    public EstimatesViewModel(ChecklistViewModel events, Random? random = null)
    {
        ArgumentNullException.ThrowIfNull(events);
        _events = events;
        _random = random ?? Random.Shared;

        foreach (ChecklistItemViewModel item in events.Items)
        {
            RiskEvent riskEvent = item.Item as RiskEvent
                ?? throw new ArgumentException("The checklist must contain risk events.", nameof(events));

            var row = new EstimateRowViewModel(riskEvent);
            row.PropertyChanged += OnRowPropertyChanged;
            _rowsByItem.Add(item, row);

            if (item.IsPresent)
            {
                Rows.Add(row);
            }

            item.PropertyChanged += OnItemPropertyChanged;
        }
    }

    /// <summary>
    /// The rows of the checked events in catalog order. ObservableCollection
    /// reports every added or removed row, so the table updates itself.
    /// </summary>
    public ObservableCollection<EstimateRowViewModel> Rows { get; } = [];

    /// <summary>How many of the visible rows are filled in completely.</summary>
    [ObservableProperty]
    public partial int CompleteCount { get; private set; }

    /// <summary>Fills the empty cells of the visible rows with demonstration data.</summary>
    [RelayCommand(CanExecute = nameof(CanFillRandom))]
    private void FillRandom()
    {
        foreach (EstimateRowViewModel row in Rows)
        {
            row.FillEmpty(_random);
        }
    }

    private bool CanFillRandom() => Rows.Any(row => !row.IsComplete);

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ChecklistItemViewModel.IsPresent)
            || sender is not ChecklistItemViewModel item)
        {
            return;
        }

        EstimateRowViewModel row = _rowsByItem[item];

        if (item.IsPresent)
        {
            // The new row goes after the rows of all checked events that
            // precede it in the catalog.
            int index = _events.Items
                .TakeWhile(other => other != item)
                .Count(other => other.IsPresent);

            Rows.Insert(index, row);
        }
        else
        {
            Rows.Remove(row);
        }

        UpdateSummary();
    }

    private void OnRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EstimateRowViewModel.IsComplete))
        {
            UpdateSummary();
        }
    }

    private void UpdateSummary()
    {
        CompleteCount = Rows.Count(row => row.IsComplete);
        FillRandomCommand.NotifyCanExecuteChanged();
    }
}
