using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using RiskManager.App.ViewModels;

namespace RiskManager.App.Views;

/// <summary>
/// The "Expert estimates" tab. The code-behind contains only view concerns:
/// markup that would otherwise be repeated ten times and the editing state of
/// the table. The logic lives in <see cref="EstimatesViewModel"/>.
/// </summary>
public partial class EstimatesView : UserControl
{
    private DataGridCellInfo? _lastEditedCell;

    public EstimatesView()
    {
        InitializeComponent();
        AddExpertColumns();

        Table.PreparingCellForEdit += (_, e) =>
            _lastEditedCell = new DataGridCellInfo(e.Row.Item, e.Column);

        IsVisibleChanged += (_, _) =>
        {
            if (!IsVisible)
            {
                FinishEditing();
            }
        };
    }

    /// <summary>
    /// Ends editing when the user leaves the tab. A cell with text that is not
    /// a number ("abc") cannot be committed; if its row then disappeared, the
    /// DataGrid would stay in that state and refuse to edit any other cell.
    /// </summary>
    private void FinishEditing()
    {
        if (_lastEditedCell is not DataGridCellInfo cell)
        {
            return;
        }

        _lastEditedCell = null;

        // CommitEdit and CancelEdit work with the current cell, which may be
        // another one if the user has clicked elsewhere.
        Table.CurrentCell = cell;

        if (!Table.CommitEdit(DataGridEditingUnit.Row, exitEditingMode: true))
        {
            Table.CancelEdit(DataGridEditingUnit.Cell);
            Table.CancelEdit(DataGridEditingUnit.Row);
        }
    }

    private void AddExpertColumns()
    {
        var converter = (IValueConverter)FindResource("NullableNumberConverter");
        var cellStyle = (Style)FindResource("NumberCell");
        var editorStyle = (Style)FindResource("NumberEditor");

        int firstIndex = Table.Columns.IndexOf(LossColumn);

        for (int i = 0; i < EstimateRowViewModel.ExpertCount; i++)
        {
            var column = new DataGridTextColumn
            {
                Header = $"Е{i + 1}",
                Width = 46,
                ElementStyle = cellStyle,
                EditingElementStyle = editorStyle,
                // By default a DataGrid writes values only when the user leaves the
                // whole row; LostFocus writes each cell as soon as it is committed.
                Binding = new Binding($"Estimates[{i}].Value")
                {
                    Converter = converter,
                    UpdateSourceTrigger = UpdateSourceTrigger.LostFocus,
                },
            };

            Table.Columns.Insert(firstIndex + i, column);
        }
    }
}
