using System.Windows.Controls;

namespace RiskManager.App.Views;

/// <summary>
/// An identification checklist tab. There is deliberately no logic here: everything
/// lives in <see cref="ViewModels.ChecklistViewModel"/>, which the parent view
/// passes in through DataContext.
/// </summary>
public partial class ChecklistView : UserControl
{
    public ChecklistView()
    {
        InitializeComponent();
    }
}
