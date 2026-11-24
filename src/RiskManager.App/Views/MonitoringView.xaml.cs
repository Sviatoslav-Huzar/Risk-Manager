using System.Windows.Controls;

namespace RiskManager.App.Views;

/// <summary>
/// The "Monitoring" tab; the code-behind only looks after the editing state of
/// the table, the logic lives in MonitoringViewModel.
/// </summary>
public partial class MonitoringView : UserControl
{
    public MonitoringView()
    {
        InitializeComponent();

        DataGridEditGuard.Attach(EvaluationTable);
    }
}
