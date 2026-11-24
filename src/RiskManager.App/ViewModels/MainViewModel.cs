using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RiskManager.App.Services;
using RiskManager.Core.Data;
using RiskManager.Core.Domain;

namespace RiskManager.App.ViewModels;

/// <summary>
/// The view model of the main window: holds the open project and the commands
/// of the "File" menu.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private const string ApplicationName = "Управління ризиками розроблення ПЗ";
    private const string UntitledName = "Без назви";

    private readonly RiskCatalog _catalog;
    private readonly IDialogService _dialogs;

    // The content of the file as it was last saved or opened.
    private string _savedContent;

    public MainViewModel(RiskCatalog catalog, IDialogService dialogs)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(dialogs);

        _catalog = catalog;
        _dialogs = dialogs;

        Project = new ProjectViewModel(catalog);
        _savedContent = ProjectStore.Serialize(Project.ToProject());
    }

    /// <summary>The open project. Replaced as a whole by New and Open.</summary>
    [ObservableProperty]
    public partial ProjectViewModel Project { get; private set; }

    /// <summary>The file of the project; null if it has not been saved yet.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProjectName))]
    [NotifyPropertyChangedFor(nameof(Title))]
    public partial string? FilePath { get; private set; }

    /// <summary>True while a file is being read or written; the commands are disabled then.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NewCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveAsCommand))]
    public partial bool IsBusy { get; private set; }

    public string ProjectName => FilePath is null ? UntitledName : Path.GetFileNameWithoutExtension(FilePath);

    public string Title => $"{ProjectName} — {ApplicationName}";

    /// <summary>
    /// Compares the data instead of remembering that something was touched: a
    /// check box that was checked and unchecked again is not a change.
    /// </summary>
    public bool HasChanges => ProjectStore.Serialize(Project.ToProject()) != _savedContent;

    private bool IsIdle => !IsBusy;

    /// <summary>
    /// Offers to save unsaved changes. False means that the user has cancelled
    /// or saving has failed, so the project must stay open.
    /// </summary>
    public async Task<bool> ConfirmLeavingAsync()
    {
        if (!HasChanges)
        {
            return true;
        }

        return _dialogs.AskToSaveChanges(ProjectName) switch
        {
            SaveChoice.Save => await TrySaveAsync(FilePath),
            SaveChoice.Discard => true,
            _ => false,
        };
    }

    [RelayCommand(CanExecute = nameof(IsIdle))]
    private async Task NewAsync()
    {
        if (await ConfirmLeavingAsync())
        {
            Show(new ProjectViewModel(_catalog), path: null);
        }
    }

    [RelayCommand(CanExecute = nameof(IsIdle))]
    private async Task OpenAsync()
    {
        if (!await ConfirmLeavingAsync() || _dialogs.AskOpenPath() is not string path)
        {
            return;
        }

        RiskProject? project = null;

        bool isRead = await TryAccessFileAsync(
            async () => project = await ProjectStore.LoadAsync(path, _catalog),
            $"Не вдалося відкрити файл «{path}».");

        if (isRead)
        {
            Show(new ProjectViewModel(_catalog, project), path);
        }
    }

    [RelayCommand(CanExecute = nameof(IsIdle))]
    private Task SaveAsync() => TrySaveAsync(FilePath);

    [RelayCommand(CanExecute = nameof(IsIdle))]
    private Task SaveAsAsync() => TrySaveAsync(path: null);

    private async Task<bool> TrySaveAsync(string? path)
    {
        path ??= _dialogs.AskSavePath(ProjectName);

        if (path is null)
        {
            return false;
        }

        // Taken before the first await: what the user changes while the file is
        // being written must count as unsaved.
        RiskProject project = Project.ToProject();

        bool isSaved = await TryAccessFileAsync(
            () => ProjectStore.SaveAsync(path, project),
            $"Не вдалося зберегти файл «{path}».");

        if (isSaved)
        {
            FilePath = path;
            _savedContent = ProjectStore.Serialize(project);
        }

        return isSaved;
    }

    private async Task<bool> TryAccessFileAsync(Func<Task> access, string errorMessage)
    {
        IsBusy = true;

        try
        {
            await access();
            return true;
        }
        // Only what can really happen to a file; anything else is a defect of the
        // program and goes to the global handler.
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            _dialogs.ShowError(errorMessage, ex);
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void Show(ProjectViewModel project, string? path)
    {
        Project = project;
        FilePath = path;
        _savedContent = ProjectStore.Serialize(project.ToProject());
    }
}
