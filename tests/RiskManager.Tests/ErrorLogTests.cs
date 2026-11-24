using System.IO;
using RiskManager.App.Services;

namespace RiskManager.Tests;

public sealed class ErrorLogTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("RiskManagerTests").FullName;

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    [Fact]
    public void Write_CreatesFolderAndAppends()
    {
        var log = new ErrorLog(Path.Combine(_directory, "new folder", "errors.log"));

        log.Write(new InvalidOperationException("first"));
        log.Write(new ArgumentException("second"));

        string text = File.ReadAllText(log.FilePath);
        Assert.Contains("InvalidOperationException: first", text);
        Assert.Contains("ArgumentException: second", text);
    }

    [Fact]
    public void Write_UnwritableFile_DoesNotThrow()
    {
        // The path is a folder, so it cannot be opened as a file.
        var log = new ErrorLog(_directory);

        log.Write(new InvalidOperationException());
    }

    [Fact]
    public void DefaultPath_IsInLocalApplicationData()
    {
        Assert.StartsWith(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            ErrorLog.DefaultPath);
    }
}
