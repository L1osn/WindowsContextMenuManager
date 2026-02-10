using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using ContextMenuManager.Services;
using Res = ContextMenuManager.Properties.Resources;
using ContextMenuManager.ViewModels;

namespace ContextMenuManager.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
            Loaded += (_, _) =>
            {
                InitLanguageCombo();
            };
        }

        private void InitLanguageCombo()
        {
            if (LanguageCombo == null) return;
            LanguageCombo.SelectionChanged -= LanguageCombo_SelectionChanged;
            LanguageCombo.Items.Clear();
            var current = LanguageService.GetSavedLanguage();
            foreach (var (culture, displayName) in LanguageService.SupportedLanguages)
            {
                var item = new ComboBoxItem { Content = displayName, Tag = culture };
                LanguageCombo.Items.Add(item);
                if (culture.Equals(current, StringComparison.OrdinalIgnoreCase))
                    LanguageCombo.SelectedItem = item;
            }
            if (LanguageCombo.SelectedItem == null && LanguageCombo.Items.Count > 0)
                LanguageCombo.SelectedIndex = 0;
            LanguageCombo.SelectionChanged += LanguageCombo_SelectionChanged;
        }

        private void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LanguageCombo?.SelectedItem is not ComboBoxItem item || item.Tag is not string culture)
                return;
            if (!culture.Equals(LanguageService.GetSavedLanguage(), StringComparison.OrdinalIgnoreCase))
                LanguageService.SetLanguageAndRestart(culture);
        }

        /// <summary>Toggle backup/restore popup.</summary>
        private void BackupMenuButton_Click(object sender, RoutedEventArgs e)
        {
            BackupPopup.IsOpen = !BackupPopup.IsOpen;
        }

        /// <summary>Show sync dialog to copy File scenario changes to other scenarios.</summary>
        private void SyncSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MainViewModel vm)
            {
                return;
            }

            var dialog = BuildSyncDialog();
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            if (dialog.Tag is not SyncSelection selection)
            {
                return;
            }

            var synced = vm.SyncFileChangesToScenarios(
                selection.ToDirectory,
                selection.ToDirectoryBackground,
                selection.ToDesktopBackground);

            MessageBox.Show(
                synced > 0
                    ? Res.Format("SyncResultOk", synced)
                    : Res.SyncResultNone,
                Res.SyncResultTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private Window BuildSyncDialog()
        {
            var toDirectory = new System.Windows.Controls.CheckBox
            {
                Content = Res.SyncToDirectory,
                Margin = new Thickness(0, 0, 0, 8),
                IsChecked = true
            };
            var toDirectoryBg = new System.Windows.Controls.CheckBox
            {
                Content = Res.SyncToDirectoryBg,
                Margin = new Thickness(0, 0, 0, 8),
                IsChecked = true
            };
            var toDesktopBg = new System.Windows.Controls.CheckBox
            {
                Content = Res.SyncToDesktopBg,
                Margin = new Thickness(0, 0, 0, 4),
                IsChecked = false
            };

            var okButton = new System.Windows.Controls.Button
            {
                Content = Res.OK,
                Width = 84,
                Margin = new Thickness(0, 0, 8, 0),
                IsDefault = true
            };
            var cancelButton = new System.Windows.Controls.Button
            {
                Content = Res.Cancel,
                Width = 84,
                IsCancel = true
            };

            var buttonPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 12, 0, 0)
            };
            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            var rootPanel = new System.Windows.Controls.StackPanel
            {
                Margin = new Thickness(16)
            };
            rootPanel.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = Res.SyncDialogIntro,
                Margin = new Thickness(0, 0, 0, 12)
            });
            rootPanel.Children.Add(toDirectory);
            rootPanel.Children.Add(toDirectoryBg);
            rootPanel.Children.Add(toDesktopBg);
            rootPanel.Children.Add(buttonPanel);

            var dialog = new Window
            {
                Title = Res.SyncDialogTitle,
                Width = 360,
                Height = 240,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.ToolWindow,
                Owner = this,
                Content = rootPanel
            };

            okButton.Click += (_, _) =>
            {
                if (toDirectory.IsChecked != true &&
                    toDirectoryBg.IsChecked != true &&
                    toDesktopBg.IsChecked != true)
                {
                    MessageBox.Show(dialog, Res.SyncSelectOne, Res.SyncSelectOneTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                dialog.Tag = new SyncSelection(
                    toDirectory.IsChecked == true,
                    toDirectoryBg.IsChecked == true,
                    toDesktopBg.IsChecked == true);
                dialog.DialogResult = true;
                dialog.Close();
            };

            return dialog;
        }

        private sealed record SyncSelection(bool ToDirectory, bool ToDirectoryBackground, bool ToDesktopBackground);
    }
}
