using System.ComponentModel;
using System.Runtime.CompilerServices;
using ContextMenuManager.Properties;

namespace ContextMenuManager.Models
{
    /// <summary>Scenario where the menu item appears.</summary>
    public enum MenuScenario
    {
        File,              // Right-click on file
        Directory,         // Right-click on directory
        DirectoryBackground, // Right-click on directory background
        DesktopBackground  // Right-click on desktop background
    }

    /// <summary>Source of the menu item.</summary>
    public enum MenuSource
    {
        System,    // Built-in
        ThirdParty // Added by third-party software
    }

    /// <summary>Type of menu item.</summary>
    public enum MenuItemType
    {
        ShellCommand,    // Shell verb/command
        ShellExtension   // Shell extension handler
    }

    /// <summary>Risk level when disabling.</summary>
    public enum RiskLevel
    {
        Low,    // Safe to disable
        Medium, // Some features may be affected
        High    // Core functionality may be affected
    }

    /// <summary>How this item is disabled/enabled.</summary>
    public enum DisableMethod
    {
        LegacyDisable,  // Shell command: add/remove LegacyDisable value
        BlockedClsid,   // Shell extension: add/remove from Blocked list
        DeleteKey,      // Delete registry key (reserved)
        RenameKey,      // Rename registry key (reserved)
        Unknown
    }

    /// <summary>Registry root / scope.</summary>
    public enum RegistryScope
    {
        CurrentUser,   // HKCU, current user only
        LocalMachine,  // HKLM, all users (admin required)
        MergedView     // HKCR merged view, may require admin
    }

    /// <summary>Context menu item model.</summary>
    public class ContextMenuItem : INotifyPropertyChanged
    {
        private bool _isEnabled;
        private bool _hasChanges;

        /// <summary>Unique id (registry subkey name or CLSID).</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Display name.</summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>Short description.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Menu item type.</summary>
        public MenuItemType ItemType { get; set; }

        /// <summary>Scenario this item belongs to.</summary>
        public MenuScenario Scenario { get; set; }

        /// <summary>Source (system/third-party).</summary>
        public MenuSource Source { get; set; }

        /// <summary>Risk level.</summary>
        public RiskLevel Risk { get; set; }

        /// <summary>Full registry path.</summary>
        public string RegistryPath { get; set; } = string.Empty;

        /// <summary>CLSID for Shell Extension (ShellExtension only).</summary>
        public string? Clsid { get; set; }

        /// <summary>Command line (ShellCommand only).</summary>
        public string? Command { get; set; }

        /// <summary>Source application name.</summary>
        public string SourceName { get; set; } = string.Empty;

        /// <summary>Whether modification requires administrator.</summary>
        public bool RequiresAdmin { get; set; }

        /// <summary>Disable method (used for apply/rollback).</summary>
        public DisableMethod DisableMethod { get; set; }

        /// <summary>Registry root/scope (current user vs all users).</summary>
        public RegistryScope RegistryScope { get; set; }

        /// <summary>Whether the item is enabled.</summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    _hasChanges = _isEnabled != OriginalIsEnabled;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasChanges));
                }
            }
        }

        /// <summary>Whether there are unapplied changes.</summary>
        public bool HasChanges
        {
            get => _hasChanges;
            set
            {
                if (_hasChanges != value)
                {
                    _hasChanges = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>Original enabled state (for change detection).</summary>
        public bool OriginalIsEnabled { get; set; }

        public string ScenarioDisplayName => Scenario switch
        {
            MenuScenario.File => Resources.Scenario_File,
            MenuScenario.Directory => Resources.Scenario_Directory,
            MenuScenario.DirectoryBackground => Resources.Scenario_DirectoryBackground,
            MenuScenario.DesktopBackground => Resources.Scenario_DesktopBackground,
            _ => Resources.Scenario_Unknown
        };

        public string SourceDisplayName => Source switch
        {
            MenuSource.System => Resources.Source_System,
            MenuSource.ThirdParty => string.IsNullOrEmpty(SourceName) ? Resources.Source_ThirdParty : SourceName,
            _ => Resources.Source_Unknown
        };

        public string RiskDisplayName => Risk switch
        {
            RiskLevel.Low => Resources.Risk_Low,
            RiskLevel.Medium => Resources.Risk_Medium,
            RiskLevel.High => Resources.Risk_High,
            _ => Resources.Risk_Unknown
        };

        public string ItemTypeDisplayName => ItemType switch
        {
            MenuItemType.ShellCommand => Resources.ItemType_Command,
            MenuItemType.ShellExtension => Resources.ItemType_Extension,
            _ => Resources.ItemType_Unknown
        };

        /// <summary>Scope display: current user only / all users (admin).</summary>
        public string RegistryScopeDisplayName => RegistryScope switch
        {
            RegistryScope.CurrentUser => Resources.Scope_CurrentUser,
            RegistryScope.LocalMachine => Resources.Scope_AllUsers,
            RegistryScope.MergedView => RequiresAdmin ? Resources.Scope_AllUsers : Resources.Scope_CurrentUser,
            _ => Resources.Scenario_Unknown
        };

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    /// <summary>Backup record.</summary>
    public class BackupRecord
    {
        public DateTime Timestamp { get; set; }
        public string Description { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public List<BackupEntry> Entries { get; set; } = new();
    }

    public class BackupEntry
    {
        public string Id { get; set; } = string.Empty;
        public string RegistryPath { get; set; } = string.Empty;
        public MenuItemType ItemType { get; set; }
        public DisableMethod DisableMethod { get; set; }
        public string? Clsid { get; set; }
        public bool WasEnabled { get; set; }
        public MenuScenario Scenario { get; set; }
    }
}
