using System.Windows;
using System.Windows.Controls;

namespace RiskManager.App.Views;

/// <summary>
/// An identification checklist tab. There is deliberately no logic here: everything
/// lives in <see cref="ViewModels.ChecklistViewModel"/>, which the parent view
/// passes in through DataContext.
/// </summary>
public partial class ChecklistView : UserControl
{
    /// <summary>
    /// Registers <see cref="Instruction"/> in the WPF property system.
    /// One static field per class, not per instance: the values themselves
    /// are stored by WPF, not in a field of this object.
    /// </summary>
    public static readonly DependencyProperty InstructionProperty =
        DependencyProperty.Register(
            name: nameof(Instruction),
            propertyType: typeof(string),
            ownerType: typeof(ChecklistView),
            typeMetadata: new PropertyMetadata(string.Empty));

    public ChecklistView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// The text shown above the checklist. It is a view-only parameter, so it
    /// is set in XAML by the parent instead of coming from the view model.
    /// </summary>
    public string Instruction
    {
        // Only GetValue/SetValue here: XAML and bindings may call SetValue
        // directly, bypassing this wrapper, so any extra logic could be skipped.
        get => (string)GetValue(InstructionProperty);
        set => SetValue(InstructionProperty, value);
    }
}
