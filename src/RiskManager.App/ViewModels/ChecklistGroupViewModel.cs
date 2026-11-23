using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using RiskManager.Core.Calculations;
using RiskManager.Core.Domain;

namespace RiskManager.App.ViewModels;

/// <summary>
/// One category block of a checklist, e.g. "Technical — 2 of 7 (11.11 %)",
/// together with its check boxes.
/// </summary>
public partial class ChecklistGroupViewModel : ObservableObject
{
    public ChecklistGroupViewModel(RiskCategory category, IReadOnlyList<ChecklistItemViewModel> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        Category = category;
        Items = items;
    }

    public RiskCategory Category { get; }

    /// <summary>
    /// The items of this category. The list itself never changes (only the
    /// items' IsPresent flags do), so a plain read-only list is enough —
    /// no ObservableCollection is needed.
    /// </summary>
    public IReadOnlyList<ChecklistItemViewModel> Items { get; }

    public int CategoryCount => Items.Count;

    /// <summary>How many items of the category are flagged.</summary>
    [ObservableProperty]
    public partial int PresentCount { get; private set; }

    /// <summary>The category share of formula (5.2) or (5.4), e.g. tc.</summary>
    [ObservableProperty]
    public partial double Share { get; private set; }

    /// <summary>Called by <see cref="ChecklistViewModel"/> after every recalculation.</summary>
    internal void Update(CategoryShare result)
    {
        // A developer-only sanity check: it is removed from Release builds.
        Debug.Assert(result.Category == Category, "Result belongs to another category.");

        PresentCount = result.PresentCount;
        Share = result.Share;
    }
}
