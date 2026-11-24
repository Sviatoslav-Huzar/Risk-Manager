namespace RiskManager.App.Services;

/// <summary>
/// The questions a view model asks the user. An interface, so that the tests can
/// answer them without opening any windows.
/// </summary>
public interface IDialogService
{
    /// <summary>Returns null if the user cancels.</summary>
    string? AskOpenPath();

    /// <summary>Returns null if the user cancels.</summary>
    string? AskSavePath(string suggestedName);

    /// <summary>Asks what to do with unsaved changes.</summary>
    SaveChoice AskToSaveChanges(string projectName);

    void ShowError(string message, Exception exception);
}

public enum SaveChoice
{
    Save,
    Discard,
    Cancel,
}
