using CommunityToolkit.Mvvm.ComponentModel;
using RiskManager.Core.Domain;

namespace RiskManager.App.ViewModels;

/// <summary>
/// One check box of an identification checklist: a risk source (stage 1.1)
/// or a risk event (stage 1.2). Wraps the immutable catalog item and adds
/// the project-specific state: whether the item applies to the project.
/// </summary>
public partial class ChecklistItemViewModel : ObservableObject
{
    public ChecklistItemViewModel(ICategorizedItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        Item = item;
    }

    /// <summary>The catalog item: a <see cref="RiskSource"/> or a <see cref="RiskEvent"/>.</summary>
    public ICategorizedItem Item { get; }

    public string Code => Item.Code;

    public string Description => Item.Description;

    public RiskCategory Category => Item.Category;

    /// <summary>
    /// The flag of the methodology: true means 1 (the item applies to the
    /// project), false means 0. The source generator implements the setter so
    /// that it raises PropertyChanged, which keeps the check box and the totals in sync.
    /// </summary>
    [ObservableProperty]
    public partial bool IsPresent { get; set; }
}
