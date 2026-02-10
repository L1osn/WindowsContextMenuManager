using System.Windows;
using ContextMenuManager.Services;

namespace ContextMenuManager
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            DispatcherUnhandledException += (_, args) =>
            {
                MessageBox.Show(
                    args.Exception?.ToString() ?? "Unknown error",
                    "Context Menu Manager",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                args.Handled = true;
                Shutdown(1);
            };
            LanguageService.ApplySavedLanguage();
            base.OnStartup(e);
        }
    }
}
