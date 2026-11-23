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

        // Clicking another risk replaces the rows of this table without leaving
        // the tab, so editing ends as soon as the focus goes elsewhere.
        var editGuard = new DataGridEditGuard(EvaluationTable);
        EvaluationTable.IsKeyboardFocusWithinChanged += (_, e) =>
        {
            if (!(bool)e.NewValue)
            {
                editGuard.FinishEditing();
            }
        };
    }
}
