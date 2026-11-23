using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RiskManager.Core.Calculations;
using RiskManager.Core.Domain;

namespace RiskManager.App.ViewModels;

/// <summary>
/// An identification checklist: the "Risk sources" tab (stage 1.1, formulas
/// 5.2-5.3) or the "Risk events" tab (stage 1.2, formula 5.4). Check boxes
/// are grouped by category and the shares are recalculated on every change.
/// </summary>
public partial class ChecklistViewModel : ObservableObject
{
    private readonly IReadOnlyList<ICategorizedItem> _catalogItems;

    /// <param name="catalogItems">
    /// Sources or events. IReadOnlyList is covariant (out T), so
    /// IReadOnlyList&lt;RiskSource&gt; can be passed here without conversion.
    /// </param>
    public ChecklistViewModel(IReadOnlyList<ICategorizedItem> catalogItems)
    {
        ArgumentNullException.ThrowIfNull(catalogItems);
        _catalogItems = catalogItems;

        Items = catalogItems
            .Select(item => new ChecklistItemViewModel(item))
            .ToList();

        // Enum.GetValues keeps the methodology order (t, c, p, m) and creates
        // a group for every category — the same approach as in Identification.
        Groups = Enum.GetValues<RiskCategory>()
            .Select(category => new ChecklistGroupViewModel(
                category,
                Items.Where(item => item.Category == category).ToList()))
            .ToList();

        // Every check box reports its changes; the checklist reacts by recalculating.
        foreach (ChecklistItemViewModel item in Items)
        {
            item.PropertyChanged += OnItemPropertyChanged;
        }

        Recalculate();
    }

    /// <summary>All items in catalog order.</summary>
    public IReadOnlyList<ChecklistItemViewModel> Items { get; }

    /// <summary>The four category blocks.</summary>
    public IReadOnlyList<ChecklistGroupViewModel> Groups { get; }

    /// <summary>R = tc + cc + pc + mc (formula 5.3).</summary>
    [ObservableProperty]
    public partial double Total { get; private set; }

    [RelayCommand(CanExecute = nameof(CanSelectAll))]
    private void SelectAll()
    {
        // Each assignment triggers a recalculation (one per item). For a few
        // dozen items that is negligible, so we keep the code simple.
        foreach (ChecklistItemViewModel item in Items)
        {
            item.IsPresent = true;
        }
    }

    private bool CanSelectAll() => Items.Any(item => !item.IsPresent);

    [RelayCommand(CanExecute = nameof(CanClearAll))]
    private void ClearAll()
    {
        foreach (ChecklistItemViewModel item in Items)
        {
            item.IsPresent = false;
        }
    }

    private bool CanClearAll() => Items.Any(item => item.IsPresent);

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ChecklistItemViewModel.IsPresent))
        {
            Recalculate();
        }
    }

    /// <summary>
    /// The ViewModel does not compute anything itself: it collects the flags,
    /// asks the model (Identification) and stores the result for the view.
    /// </summary>
    private void Recalculate()
    {
        HashSet<string> presentCodes = Items
            .Where(item => item.IsPresent)
            .Select(item => item.Code)
            .ToHashSet();

        IdentificationResult result = Identification.Calculate(_catalogItems, presentCodes);

        foreach (ChecklistGroupViewModel group in Groups)
        {
            group.Update(result[group.Category]);
        }

        Total = result.Total;

        // Buttons re-check whether they should be enabled.
        SelectAllCommand.NotifyCanExecuteChanged();
        ClearAllCommand.NotifyCanExecuteChanged();
    }
}
