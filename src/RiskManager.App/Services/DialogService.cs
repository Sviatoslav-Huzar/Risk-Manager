using System.Windows;
using Microsoft.Win32;

namespace RiskManager.App.Services;

/// <summary>The standard Windows dialogs.</summary>
public sealed class DialogService : IDialogService
{
    private const string Title = "Управління ризиками";
    private const string Extension = ".riskproj";
    private const string Filter = $"Проєкт управління ризиками (*{Extension})|*{Extension}|Усі файли (*.*)|*.*";

    public string? AskOpenPath()
    {
        var dialog = new OpenFileDialog { Filter = Filter, DefaultExt = Extension };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? AskSavePath(string suggestedName)
    {
        var dialog = new SaveFileDialog { Filter = Filter, DefaultExt = Extension, FileName = suggestedName };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public SaveChoice AskToSaveChanges(string projectName)
    {
        MessageBoxResult result = MessageBox.Show(
            $"Зберегти зміни в проєкті «{projectName}»?",
            Title,
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        return result switch
        {
            MessageBoxResult.Yes => SaveChoice.Save,
            MessageBoxResult.No => SaveChoice.Discard,
            _ => SaveChoice.Cancel,
        };
    }

    public void ShowError(string message, Exception exception) =>
        MessageBox.Show(
            $"{message}\n\nПодробиці: {exception.Message}",
            Title,
            MessageBoxButton.OK,
            MessageBoxImage.Error);
}
