using System.Globalization;
using System.Windows;
using System.Windows.Markup;
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
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ApplyUkrainianCulture();

        RiskCatalog catalog = CatalogLoader.LoadDefault();

        var mainWindow = new MainWindow
        {
            DataContext = new MainViewModel(catalog),
        };
        mainWindow.Show();
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
