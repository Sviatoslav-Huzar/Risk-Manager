using System.Diagnostics;
using System.IO;

namespace RiskManager.App.Services;

/// <summary>Appends unexpected errors to a text file.</summary>
public sealed class ErrorLog
{
    public ErrorLog(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        FilePath = path;
    }

    /// <summary>%LOCALAPPDATA%\RiskManager\errors.log</summary>
    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RiskManager",
        "errors.log");

    public string FilePath { get; }

    /// <summary>
    /// Never throws: it is called while another error is being handled, and an
    /// unwritable log must not hide that error.
    /// </summary>
    public void Write(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(FilePath))!);
            File.AppendAllText(FilePath, $"{DateTimeOffset.Now:O}\n{exception}\n\n");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Debug.WriteLine($"The error log is not available: {ex.Message}");
        }
    }
}
