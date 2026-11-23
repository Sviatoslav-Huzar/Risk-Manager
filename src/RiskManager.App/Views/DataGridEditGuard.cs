using System.Windows.Controls;

namespace RiskManager.App.Views;

/// <summary>
/// Ends editing of a table before its rows can change. A cell with text that is
/// not a number ("abc") cannot be committed; if its row then disappeared, the
/// DataGrid would stay in that state and refuse to edit any other cell.
/// </summary>
internal sealed class DataGridEditGuard
{
    private readonly DataGrid _table;
    private DataGridCellInfo? _lastEditedCell;

    public DataGridEditGuard(DataGrid table)
    {
        _table = table;
        _table.PreparingCellForEdit += (_, e) =>
            _lastEditedCell = new DataGridCellInfo(e.Row.Item, e.Column);
    }

    /// <summary>Commits the edited row, or cancels the edit if it cannot be committed.</summary>
    public void FinishEditing()
    {
        if (_lastEditedCell is not DataGridCellInfo cell)
        {
            return;
        }

        _lastEditedCell = null;

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
