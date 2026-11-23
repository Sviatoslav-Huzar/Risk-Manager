using System.Windows.Controls;

namespace RiskManager.App.Views;

/// <summary>
/// The "Risk sources" tab. There is deliberately no logic here: everything
/// lives in <see cref="ViewModels.SourcesViewModel"/>, which the parent view
/// passes in through DataContext.
/// </summary>
public partial class SourcesView : UserControl
{
    public SourcesView()
    {
        InitializeComponent();
    }
}
