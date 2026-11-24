using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
using RiskManager.App.Services;
using RiskManager.App.ViewModels;
using RiskManager.Core.Data;
using RiskManager.Core.Domain;

namespace RiskManager.App;

/// <summary>
/// Application entry point and composition root: the single place where
/// the objects of the application are created and wired together.
/// </summary>
public partial class App : Application
{
    private readonly ErrorLog _errorLog = new(ErrorLog.DefaultPath);
    private readonly IDialogService _dialogs = new DialogService();
    private bool _isShowingError;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        HandleUnexpectedErrors();

        try
        {
            ApplyUkrainianCulture();

            RiskCatalog catalog = CatalogLoader.LoadDefault();

            var mainWindow = new MainWindow
            {
                DataContext = new MainViewModel(catalog, _dialogs),
            };
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            // Without the window the program would keep running invisibly.
            _errorLog.Write(ex);
            _dialogs.ShowError("Не вдалося запустити програму.", ex);
            Shutdown(exitCode: 1);
        }
    }

    /// <summary>
    /// The last line of defence for defects of the program. Errors that are
    /// expected, such as an unreadable file, are handled where they occur.
    /// </summary>
    private void HandleUnexpectedErrors()
    {
        // The UI thread: the program can go on, and the user can save the project.
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        // Any other thread: the process is about to end, only logging is possible.
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception exception)
            {
                _errorLog.Write(exception);
            }
        };

        // A failed task that nobody has awaited.
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            _errorLog.Write(e.Exception);
            e.SetObserved();
        };
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _errorLog.Write(e.Exception);
        e.Handled = true;

        // An error in drawing would repeat while its message is open, and the
        // screen would fill with messages.
        if (_isShowingError)
        {
            return;
        }

        _isShowingError = true;

        try
        {
            _dialogs.ShowError(
                $"Сталася непередбачена помилка. Збережіть проєкт і перезапустіть програму.\n\nЖурнал помилок: {_errorLog.FilePath}",
                e.Exception);
        }
        finally
        {
            _isShowingError = false;
        }
    }

    /// <summary>
    /// The whole UI is in Ukrainian, so numbers are formatted the Ukrainian way
    /// ("38,89 %") regardless of the Windows regional settings.
    /// </summary>
    private static void ApplyUkrainianCulture()
    {
        CultureInfo culture = CultureInfo.GetCultureInfo("uk-UA");

        // For C# code: ToString(), string.Format, number parsing.
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        // For WPF bindings: they format values using the element's Language
        // property, which is en-US by default no matter what the OS uses.
        // OverrideMetadata may be called only once per process.
        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(culture.IetfLanguageTag)));
    }
}
