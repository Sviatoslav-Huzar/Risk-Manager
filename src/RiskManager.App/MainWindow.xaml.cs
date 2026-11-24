using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using RiskManager.App.ViewModels;

namespace RiskManager.App;

/// <summary>
/// The main window. The code-behind handles what only a window can do: closing
/// and the keyboard focus.
/// </summary>
public partial class MainWindow : Window
{
    private bool _isLeavingConfirmed;
    private bool _isConfirming;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnExitClick(object sender, RoutedEventArgs e) => Close();

    /// <summary>
    /// A shortcut such as Ctrl+S does not move the focus, so a number that is
    /// still being typed would not be saved. Without focus its table ends editing.
    /// </summary>
    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.KeyboardDevice.Modifiers is ModifierKeys.Control or (ModifierKeys.Control | ModifierKeys.Shift)
            && e.Key is (Key.N or Key.O or Key.S))
        {
            Keyboard.ClearFocus();
        }

        base.OnPreviewKeyDown(e);
    }

    /// <summary>
    /// Closing cannot wait for an asynchronous operation. So it is cancelled, the
    /// changes are saved, and then the window is closed again.
    /// </summary>
    protected override async void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);

        if (_isLeavingConfirmed || DataContext is not MainViewModel viewModel)
        {
            return;
        }

        Keyboard.ClearFocus();

        if (!viewModel.IsBusy && !viewModel.HasChanges)
        {
            return;
        }

        e.Cancel = true;

        // A second click on the close button while the first one is processed.
        if (_isConfirming || viewModel.IsBusy)
        {
            return;
        }

        _isConfirming = true;

        try
        {
            _isLeavingConfirmed = await viewModel.ConfirmLeavingAsync();
        }
        finally
        {
            _isConfirming = false;
        }

        if (_isLeavingConfirmed)
        {
            // Close() may not be called inside OnClosing, and without a real
            // wait we are still inside it.
            _ = Dispatcher.BeginInvoke(Close);
        }
    }
}
