using System.Windows.Controls;

namespace RiskManager.App.Views;

/// <summary>
/// Ends editing of a table as soon as the focus leaves it: the user may be about
/// to change its rows (another tab, another risk, another project). A cell with text that is
/// not a number ("abc") cannot be committed; if its row then disappeared, the
/// DataGrid would stay in that state and refuse to edit any other cell.
/// </summary>
internal sealed class DataGridEditGuard
{
    private readonly DataGrid _table;
    private DataGridCellInfo? _lastEditedCell;

    private DataGridEditGuard(DataGrid table)
    {
        _table = table;
        _table.PreparingCellForEdit += (_, e) =>
            _lastEditedCell = new DataGridCellInfo(e.Row.Item, e.Column);

        _table.IsKeyboardFocusWithinChanged += (_, e) =>
        {
            if (!(bool)e.NewValue)
            {
                FinishEditing();
            }
        };

        // A tab can also be left while the focus is elsewhere.
        _table.IsVisibleChanged += (_, e) =>
        {
            if (!(bool)e.NewValue)
            {
                FinishEditing();
            }
        };
    }

    public static void Attach(DataGrid table) => _ = new DataGridEditGuard(table);

    /// <summary>Commits the edited row, or cancels the edit if it cannot be committed.</summary>
    private void FinishEditing()
    {
        if (_lastEditedCell is not DataGridCellInfo cell)
        {
            return;
        }

        _lastEditedCell = null;

        // The rows have already been replaced, there is nothing to finish.
        if (!_table.Items.Contains(cell.Item))
        {
            return;
        }

        // CommitEdit and CancelEdit work with the current cell, which may be
        // another one if the user has clicked elsewhere.
        _table.CurrentCell = cell;

        if (!_table.CommitEdit(DataGridEditingUnit.Row, exitEditingMode: true))
        {
            _table.CancelEdit(DataGridEditingUnit.Cell);
            _table.CancelEdit(DataGridEditingUnit.Row);
        }
    }
}
