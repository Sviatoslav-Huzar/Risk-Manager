using System.IO;
using RiskManager.App.Services;
using RiskManager.App.ViewModels;
using RiskManager.Core.Data;

namespace RiskManager.Tests;

public sealed class MainViewModelTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("RiskManagerTests").FullName;
    private readonly FakeDialogs _dialogs = new();
    private readonly MainViewModel _vm;

    public MainViewModelTests()
    {
        _vm = new MainViewModel(CatalogLoader.LoadDefault(), _dialogs);
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    [Fact]
    public void NewWindow_HasUntitledUnchangedProject()
    {
        Assert.Null(_vm.FilePath);
        Assert.StartsWith("Без назви — ", _vm.Title);
        Assert.False(_vm.HasChanges);
    }

    [Fact]
    public void HasChanges_ComparesData()
    {
        ChangeProject();
        Assert.True(_vm.HasChanges);

        _vm.Project.Events.ClearAllCommand.Execute(null);
        Assert.False(_vm.HasChanges);
    }

    [Fact]
    public async Task Save_FirstTime_AsksForPath()
    {
        ChangeProject();
        _dialogs.SavePath = PathOf("first");

        await _vm.SaveCommand.ExecuteAsync(null);

        Assert.Equal("Без назви", _dialogs.SuggestedName);
        Assert.Equal(PathOf("first"), _vm.FilePath);
        Assert.StartsWith("first — ", _vm.Title);
        Assert.False(_vm.HasChanges);
        Assert.True(File.Exists(PathOf("first")));
    }

    [Fact]
    public async Task Save_SecondTime_UsesSamePath()
    {
        await SaveTo("first");
        _dialogs.SavePath = PathOf("unused");
        ChangeProject();

        await _vm.SaveCommand.ExecuteAsync(null);

        Assert.False(File.Exists(PathOf("unused")));
        Assert.False(_vm.HasChanges);
        Assert.Contains("t1", await File.ReadAllTextAsync(PathOf("first")));
    }

    [Fact]
    public async Task SaveAs_AsksForPathAndSwitchesToIt()
    {
        await SaveTo("first");
        _dialogs.SavePath = PathOf("second");

        await _vm.SaveAsCommand.ExecuteAsync(null);

        Assert.Equal("first", _dialogs.SuggestedName);
        Assert.Equal(PathOf("second"), _vm.FilePath);
        Assert.True(File.Exists(PathOf("first")));
    }

    [Fact]
    public async Task Save_CancelledDialog_ChangesNothing()
    {
        ChangeProject();
        _dialogs.SavePath = null;

        await _vm.SaveCommand.ExecuteAsync(null);

        Assert.Null(_vm.FilePath);
        Assert.True(_vm.HasChanges);
    }

    [Fact]
    public async Task Save_Failure_ShowsErrorAndKeepsChanges()
    {
        ChangeProject();
        _dialogs.SavePath = Path.Combine(_directory, "no such folder", "x.riskproj");

        await _vm.SaveCommand.ExecuteAsync(null);

        Assert.Equal(1, _dialogs.ErrorCount);
        Assert.Null(_vm.FilePath);
        Assert.True(_vm.HasChanges);
        Assert.False(_vm.IsBusy);
    }

    [Fact]
    public async Task Open_RestoresSavedProject()
    {
        ChangeProject();
        await SaveTo("first");
        ProjectViewModel original = _vm.Project;
        _vm.Project.Events.ClearAllCommand.Execute(null);
        _dialogs.Choice = SaveChoice.Discard;
        _dialogs.OpenPath = PathOf("first");

        await _vm.OpenCommand.ExecuteAsync(null);

        Assert.NotSame(original, _vm.Project);
        Assert.Equal(["t1"], _vm.Project.Events.PresentCodes);
        Assert.Equal(PathOf("first"), _vm.FilePath);
        Assert.False(_vm.HasChanges);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("""{ "version": 99 }""")]
    public async Task Open_InvalidFile_ShowsErrorAndKeepsProject(string content)
    {
        await SaveTo("first");
        ProjectViewModel original = _vm.Project;
        await File.WriteAllTextAsync(PathOf("bad"), content);
        _dialogs.OpenPath = PathOf("bad");

        await _vm.OpenCommand.ExecuteAsync(null);

        Assert.Equal(1, _dialogs.ErrorCount);
        Assert.Same(original, _vm.Project);
        Assert.Equal(PathOf("first"), _vm.FilePath);
    }

    [Fact]
    public async Task Open_MissingFile_ShowsError()
    {
        _dialogs.OpenPath = PathOf("missing");

        await _vm.OpenCommand.ExecuteAsync(null);

        Assert.Equal(1, _dialogs.ErrorCount);
    }

    [Fact]
    public async Task Open_CancelledDialog_KeepsProject()
    {
        ProjectViewModel original = _vm.Project;

        await _vm.OpenCommand.ExecuteAsync(null);

        Assert.Same(original, _vm.Project);
        Assert.Equal(0, _dialogs.ErrorCount);
    }

    [Fact]
    public async Task New_ReplacesProjectAndForgetsFile()
    {
        ChangeProject();
        await SaveTo("first");

        await _vm.NewCommand.ExecuteAsync(null);

        Assert.Equal(0, _dialogs.QuestionCount); // everything is saved, nothing to ask
        Assert.Empty(_vm.Project.Events.PresentCodes);
        Assert.Null(_vm.FilePath);
        Assert.False(_vm.HasChanges);
    }

    [Fact]
    public async Task New_Cancelled_KeepsProject()
    {
        ChangeProject();
        _dialogs.Choice = SaveChoice.Cancel;

        await _vm.NewCommand.ExecuteAsync(null);

        Assert.Equal(1, _dialogs.QuestionCount);
        Assert.True(_vm.HasChanges);
    }

    [Fact]
    public async Task Open_CancelledQuestion_DoesNotAskForPath()
    {
        ChangeProject();
        await SaveTo("first");
        _vm.Project.Events.ClearAllCommand.Execute(null);
        _dialogs.Choice = SaveChoice.Cancel;
        _dialogs.OpenPath = PathOf("first");

        await _vm.OpenCommand.ExecuteAsync(null);

        Assert.Empty(_vm.Project.Events.PresentCodes);
    }

    [Theory]
    [InlineData(SaveChoice.Save, true)]
    [InlineData(SaveChoice.Discard, true)]
    [InlineData(SaveChoice.Cancel, false)]
    public async Task ConfirmLeaving_FollowsChoice(SaveChoice choice, bool expected)
    {
        ChangeProject();
        _dialogs.Choice = choice;
        _dialogs.SavePath = PathOf("first");

        Assert.Equal(expected, await _vm.ConfirmLeavingAsync());

        Assert.Equal(choice == SaveChoice.Save, File.Exists(PathOf("first")));
    }

    [Fact]
    public async Task ConfirmLeaving_SaveWithoutPath_StaysOpen()
    {
        ChangeProject();
        _dialogs.Choice = SaveChoice.Save;
        _dialogs.SavePath = null;

        Assert.False(await _vm.ConfirmLeavingAsync());
    }

    [Fact]
    public async Task ConfirmLeaving_NoChanges_DoesNotAsk()
    {
        Assert.True(await _vm.ConfirmLeavingAsync());
        Assert.Equal(0, _dialogs.QuestionCount);
    }

    [Fact]
    public async Task Commands_AreDisabledWhileFileIsAccessed()
    {
        var states = new List<(bool Busy, bool CanOpen)>();
        _vm.OpenCommand.CanExecuteChanged += (_, _) => states.Add((_vm.IsBusy, _vm.OpenCommand.CanExecute(null)));

        await SaveTo("first");

        Assert.Equal([(true, false), (false, true)], states);
    }

    [Fact]
    public async Task Title_IsNotified()
    {
        List<string?> changed = [];
        _vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        await SaveTo("first");

        Assert.Contains(nameof(MainViewModel.Title), changed);
        Assert.Contains(nameof(MainViewModel.ProjectName), changed);
    }

    [Fact]
    public void NullArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => new MainViewModel(null!, _dialogs));
        Assert.Throws<ArgumentNullException>(() => new MainViewModel(CatalogLoader.LoadDefault(), null!));
    }

    private void ChangeProject() =>
        _vm.Project.Events.Items.Single(item => item.Code == "t1").IsPresent = true;

    private async Task SaveTo(string name)
    {
        _dialogs.SavePath = PathOf(name);
        await _vm.SaveCommand.ExecuteAsync(null);
    }

    private string PathOf(string name) => Path.Combine(_directory, name + ".riskproj");

    private sealed class FakeDialogs : IDialogService
    {
        public string? OpenPath { get; set; }

        public string? SavePath { get; set; }

        public SaveChoice Choice { get; set; }

        public string? SuggestedName { get; private set; }

        public int QuestionCount { get; private set; }

        public int ErrorCount { get; private set; }

        public string? AskOpenPath() => OpenPath;

        public string? AskSavePath(string suggestedName)
        {
            SuggestedName = suggestedName;
            return SavePath;
        }

        public SaveChoice AskToSaveChanges(string projectName)
        {
            QuestionCount++;
            return Choice;
        }

        public void ShowError(string message, Exception exception) => ErrorCount++;
    }
}
