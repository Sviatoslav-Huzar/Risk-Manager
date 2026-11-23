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
    public EstimatesView()
    {
        InitializeComponent();
        AddExpertColumns();

        // The rows change on another tab, so leaving this one is enough.
        var editGuard = new DataGridEditGuard(Table);
        IsVisibleChanged += (_, _) =>
        {
            if (!IsVisible)
            {
                editGuard.FinishEditing();
            }
        };
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
