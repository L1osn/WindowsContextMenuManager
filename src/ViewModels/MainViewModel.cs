using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using ContextMenuManager.Models;
using ContextMenuManager.Properties;
using ContextMenuManager.Services;

namespace ContextMenuManager.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly RegistryService _registryService;
        private readonly BackupService _backupService;

        private bool _isClassicMenu;
        private MenuScenario _selectedScenario;
        private string _searchText = "";
        private string _statusMessage = "";
        private bool _isLoading;
        private bool _hasUnappliedChanges;
        private int _totalItemCount;
        private int _filteredItemCount;

        private List<ContextMenuItem> _allItems = new();

        public ObservableCollection<ContextMenuItem> FilteredItems { get; } = new();

        public ObservableCollection<BackupRecord> Backups { get; } = new();

        public MainViewModel()
        {
            _registryService = new RegistryService();
            _backupService = new BackupService();

            RefreshCommand = new RelayCommand(ExecuteRefresh);
            ApplyChangesCommand = new RelayCommand(ExecuteApplyChanges, () => HasUnappliedChanges);
            RestoreLastBackupCommand = new RelayCommand(ExecuteRestoreLastBackup);
            RestoreDefaultCommand = new RelayCommand(ExecuteRestoreDefault);
            RestartExplorerCommand = new RelayCommand(ExecuteRestartExplorer);
            ToggleClassicMenuCommand = new RelayCommand(ExecuteToggleClassicMenu);
            OpenBackupFolderCommand = new RelayCommand(ExecuteOpenBackupFolder);
            SelectScenarioCommand = new RelayCommand(p => ExecuteSelectScenario(p));

            _selectedScenario = MenuScenario.File;
            LoadData();
        }

        public bool IsClassicMenu
        {
            get => _isClassicMenu;
            set => SetField(ref _isClassicMenu, value);
        }

        public MenuScenario SelectedScenario
        {
            get => _selectedScenario;
            set
            {
                if (SetField(ref _selectedScenario, value))
                    ApplyFilter();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetField(ref _searchText, value))
                    ApplyFilter();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetField(ref _statusMessage, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetField(ref _isLoading, value);
        }

        public bool HasUnappliedChanges
        {
            get => _hasUnappliedChanges;
            set => SetField(ref _hasUnappliedChanges, value);
        }

        public int TotalItemCount
        {
            get => _totalItemCount;
            set => SetField(ref _totalItemCount, value);
        }

        public int FilteredItemCount
        {
            get => _filteredItemCount;
            set => SetField(ref _filteredItemCount, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand ApplyChangesCommand { get; }
        public ICommand RestoreLastBackupCommand { get; }
        public ICommand RestoreDefaultCommand { get; }
        public ICommand RestartExplorerCommand { get; }
        public ICommand ToggleClassicMenuCommand { get; }
        public ICommand OpenBackupFolderCommand { get; }
        public ICommand SelectScenarioCommand { get; }

        /// <summary>Load all data (scan menu, backup list, filter).</summary>
        private void LoadData()
        {
            IsLoading = true;
            StatusMessage = Resources.Status_Scanning;

            try
            {
                IsClassicMenu = _registryService.IsClassicMenuEnabled();

                _allItems = _registryService.ScanAllMenuItems();
                TotalItemCount = _allItems.Count;

                foreach (var item in _allItems)
                {
                    item.PropertyChanged += Item_PropertyChanged;
                }

                _backupService.EnsureDefaultBackupIfFirstRun(_allItems);

                RefreshBackupList();

                ApplyFilter();

                StatusMessage = Resources.Format("Status_Loaded", _allItems.Count);
            }
            catch (Exception ex)
            {
                StatusMessage = Resources.Format("Status_LoadFailed", ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>React to item IsEnabled/HasChanges to update HasUnappliedChanges.</summary>
        private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if ((e.PropertyName == nameof(ContextMenuItem.IsEnabled) || e.PropertyName == nameof(ContextMenuItem.HasChanges)) && sender is ContextMenuItem)
            {
                HasUnappliedChanges = _allItems.Any(i => i.HasChanges);
            }
        }

        /// <summary>Filter items by scenario and search text.</summary>
        private void ApplyFilter()
        {
            FilteredItems.Clear();

            var filtered = _allItems
                .Where(i => i.Scenario == _selectedScenario)
                .Where(i =>
                {
                    if (string.IsNullOrWhiteSpace(SearchText))
                        return true;

                    var search = SearchText.ToLowerInvariant();
                    return i.DisplayName.ToLowerInvariant().Contains(search)
                        || i.Description.ToLowerInvariant().Contains(search)
                        || i.Id.ToLowerInvariant().Contains(search)
                        || i.SourceDisplayName.ToLowerInvariant().Contains(search);
                })
                .OrderByDescending(i => i.IsEnabled)
                .ThenBy(i => i.DisplayName);

            foreach (var item in filtered)
            {
                FilteredItems.Add(item);
            }

            FilteredItemCount = FilteredItems.Count;
        }

        /// <summary>Refresh: reload data and clear change tracking.</summary>
        private void ExecuteRefresh()
        {
            foreach (var item in _allItems)
            {
                item.PropertyChanged -= Item_PropertyChanged;
            }

            HasUnappliedChanges = false;
            LoadData();
        }

        /// <summary>Apply pending changes to registry (with backup).</summary>
        private void ExecuteApplyChanges()
        {
            var changedItems = _allItems.Where(i => i.HasChanges).ToList();

            if (!changedItems.Any())
            {
                StatusMessage = Resources.Status_NoChanges;
                return;
            }

            var adminItems = changedItems.Where(i => i.RequiresAdmin).ToList();
            if (adminItems.Any())
            {
                var list = string.Join("\n", adminItems.Select(i => "  • " + i.DisplayName));
                var result = MessageBox.Show(
                    Resources.Format("Msg_AdminRequired", adminItems.Count, list),
                    Resources.Msg_AdminRequiredTitle,
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;
            }

            IsLoading = true;
            StatusMessage = Resources.Status_Applying;

            try
            {
                _backupService.CreateBackup(_allItems, $"Auto backup before apply - {DateTime.Now:HH:mm:ss}");

                int successCount = 0;
                int failCount = 0;
                var errors = new List<string>();

                foreach (var item in changedItems)
                {
                    try
                    {
                        if (item.IsEnabled)
                            _registryService.EnableItem(item);
                        else
                            _registryService.DisableItem(item);

                        item.OriginalIsEnabled = item.IsEnabled;
                        item.HasChanges = false;
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        errors.Add($"{item.DisplayName}: {ex.Message}");
                    }
                }

                HasUnappliedChanges = _allItems.Any(i => i.HasChanges);

                ExplorerHelper.NotifyShellChange();

                RefreshBackupList();

                if (failCount == 0)
                {
                    StatusMessage = Resources.Format("Status_ApplySuccess", successCount);
                }
                else
                {
                    StatusMessage = Resources.Format("Status_ApplyPartial", successCount, failCount);
                    MessageBox.Show(
                        Resources.Format("Msg_ApplyPartial", string.Join("\n", errors)),
                        Resources.Msg_ApplyPartialTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = Resources.Format("Status_ApplyFailed", ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>Restore from the latest backup.</summary>
        private void ExecuteRestoreLastBackup()
        {
            var latest = _backupService.GetLatestBackup();
            if (latest == null)
            {
                MessageBox.Show(Resources.Msg_NoBackup, Resources.Msg_NoBackupTitle, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                Resources.Format("Msg_ConfirmRestore", latest.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"), latest.Description, latest.Entries.Count),
                Resources.Msg_ConfirmRestoreTitle,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            RestoreFromBackup(latest);
        }

        /// <summary>Restore default (enable all items, clear Blocked).</summary>
        private void ExecuteRestoreDefault()
        {
            var result = MessageBox.Show(
                Resources.Msg_ConfirmReset,
                Resources.Msg_ConfirmResetTitle,
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            IsLoading = true;
            StatusMessage = Resources.Status_Resetting;

            try
            {
                _backupService.CreateBackup(_allItems, "Auto backup before restore default");

                foreach (var item in _allItems.Where(i => !i.IsEnabled))
                {
                    try
                    {
                        _registryService.EnableItem(item);
                    }
                    catch { }
                }

                ExplorerHelper.NotifyShellChange();
                StatusMessage = Resources.Status_ResetDone;

                ExecuteRefresh();
            }
            catch (Exception ex)
            {
                StatusMessage = Resources.Format("Status_ResetFailed", ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>Restore from a backup record.</summary>
        private void RestoreFromBackup(BackupRecord backup)
        {
            IsLoading = true;
            StatusMessage = Resources.Status_Restoring;

            try
            {
                _backupService.CreateBackup(_allItems, "Auto backup before restore");

                foreach (var entry in backup.Entries)
                {
                    var currentItem = _allItems.FirstOrDefault(i =>
                        i.Id == entry.Id &&
                        i.Scenario == entry.Scenario);

                    if (currentItem == null) continue;

                    try
                    {
                        if (entry.WasEnabled && !currentItem.IsEnabled)
                            _registryService.EnableItem(currentItem);
                        else if (!entry.WasEnabled && currentItem.IsEnabled)
                            _registryService.DisableItem(currentItem);
                    }
                    catch { }
                }

                ExplorerHelper.NotifyShellChange();
                StatusMessage = Resources.Status_Restored;

                ExecuteRefresh();
            }
            catch (Exception ex)
            {
                StatusMessage = Resources.Format("Status_RestoreFailed", ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>Toggle Win10 classic vs Win11 new menu.</summary>
        private void ExecuteToggleClassicMenu()
        {
            var targetClassic = IsClassicMenu;
            try
            {
                if (targetClassic)
                {
                    _registryService.EnableClassicMenu();
                    StatusMessage = Resources.Status_ClassicOn;
                }
                else
                {
                    _registryService.DisableClassicMenu();
                    StatusMessage = Resources.Status_ClassicOff;
                }

                var restart = MessageBox.Show(
                    Resources.Msg_ClassicRestart,
                    Resources.Msg_ClassicRestartTitle,
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (restart == MessageBoxResult.Yes)
                {
                    ExplorerHelper.RestartExplorer();
                }
            }
            catch (Exception ex)
            {
                IsClassicMenu = !targetClassic;
                StatusMessage = Resources.Format("Status_ClassicFailed", ex.Message);
                MessageBox.Show(
                    Resources.Format("Msg_ClassicError", ex.Message),
                    Resources.Msg_ClassicErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>Restart Explorer.</summary>
        private void ExecuteRestartExplorer()
        {
            var result = MessageBox.Show(
                Resources.Msg_ExplorerConfirm,
                Resources.Msg_ExplorerConfirmTitle,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                ExplorerHelper.RestartExplorer();
                StatusMessage = Resources.Status_ExplorerRestarted;
            }
            catch (Exception ex)
            {
                StatusMessage = Resources.Format("Status_ExplorerFailed", ex.Message);
            }
        }

        /// <summary>Open backup folder in Explorer.</summary>
        private void ExecuteOpenBackupFolder()
        {
            try
            {
                var dir = _backupService.GetBackupDirectory();
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = dir,
                    UseShellExecute = true
                });
            }
            catch { }
        }

        /// <summary>Select scenario tab.</summary>
        private void ExecuteSelectScenario(object? parameter)
        {
            if (parameter is string scenarioStr && Enum.TryParse<MenuScenario>(scenarioStr, out var scenario))
            {
                SelectedScenario = scenario;
            }
        }

        /// <summary>Refresh backup list for UI.</summary>
        private void RefreshBackupList()
        {
            Backups.Clear();
            foreach (var backup in _backupService.GetBackups().Take(10))
            {
                Backups.Add(backup);
            }
        }

        /// <summary>Restore from a backup (for UI binding).</summary>
        public void RestoreFromBackupFile(BackupRecord backup)
        {
            RestoreFromBackup(backup);
        }

        /// <summary>Sync changed items from File scenario to other scenarios (marked only, not written to registry).</summary>
        public int SyncFileChangesToScenarios(bool toDirectory, bool toDirectoryBackground, bool toDesktopBackground)
        {
            var targets = new List<MenuScenario>();
            if (toDirectory) targets.Add(MenuScenario.Directory);
            if (toDirectoryBackground) targets.Add(MenuScenario.DirectoryBackground);
            if (toDesktopBackground) targets.Add(MenuScenario.DesktopBackground);
            if (targets.Count == 0) return 0;

            var sourceChanges = _allItems
                .Where(i => i.Scenario == MenuScenario.File && i.HasChanges)
                .ToList();

            int syncedCount = 0;
            foreach (var source in sourceChanges)
            {
                foreach (var targetScenario in targets)
                {
                    var target = _allItems.FirstOrDefault(i =>
                        i.Scenario == targetScenario &&
                        i.ItemType == source.ItemType &&
                        i.Id.Equals(source.Id, StringComparison.OrdinalIgnoreCase));

                    if (target == null) continue;
                    if (target.IsEnabled == source.IsEnabled) continue;

                    target.IsEnabled = source.IsEnabled;
                    syncedCount++;
                }
            }

            HasUnappliedChanges = _allItems.Any(i => i.HasChanges);
            ApplyFilter();
            return syncedCount;
        }

        // ─── INotifyPropertyChanged ───

        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            return true;
        }
    }
}
