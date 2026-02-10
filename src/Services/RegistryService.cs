using System.IO;
using Microsoft.Win32;
using ContextMenuManager.Models;

namespace ContextMenuManager.Services
{
    /// <summary>Registry service: scan, disable, enable context menu items.</summary>
    public class RegistryService
    {
        // Win11 classic menu toggle key (HKCU)
        private const string Win11ClassicMenuKey = @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32";

        // Shell Extension Blocked key
        private const string ShellExtBlockedKey = @"Software\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked";

        // Registry paths per scenario

        private static readonly Dictionary<MenuScenario, string[]> ShellPaths = new()
        {
            [MenuScenario.File] = new[]
            {
                @"*\shell",
                @"SystemFileAssociations\*\shell"
            },
            [MenuScenario.Directory] = new[]
            {
                @"Directory\shell"
            },
            [MenuScenario.DirectoryBackground] = new[]
            {
                @"Directory\Background\shell"
            },
            [MenuScenario.DesktopBackground] = new[]
            {
                @"DesktopBackground\Shell"
            }
        };

        private static readonly Dictionary<MenuScenario, string[]> ShellExPaths = new()
        {
            [MenuScenario.File] = new[]
            {
                @"*\shellex\ContextMenuHandlers",
                @"AllFilesystemObjects\shellex\ContextMenuHandlers"
            },
            [MenuScenario.Directory] = new[]
            {
                @"Directory\shellex\ContextMenuHandlers"
            },
            [MenuScenario.DirectoryBackground] = new[]
            {
                @"Directory\Background\shellex\ContextMenuHandlers"
            },
            [MenuScenario.DesktopBackground] = new[]
            {
                @"DesktopBackground\shellex\ContextMenuHandlers"
            }
        };

        // Known system items (fallback name/description)
        private static readonly Dictionary<string, (string Name, string Desc, RiskLevel Risk)> KnownItems = new(StringComparer.OrdinalIgnoreCase)
        {
            ["open"]          = ("Open", "Open file with default program", RiskLevel.High),
            ["edit"]          = ("Edit", "Open with editor", RiskLevel.Medium),
            ["print"]         = ("Print", "Print this file", RiskLevel.Low),
            ["printto"]       = ("Print to...", "Print with chosen printer", RiskLevel.Low),
            ["runas"]         = ("Run as administrator", "Run with elevated privileges", RiskLevel.Medium),
            ["runasuser"]     = ("Run as different user", "Run as another user", RiskLevel.Low),
            ["explore"]       = ("Explore", "Open folder in Explorer", RiskLevel.High),
            ["find"]          = ("Search", "Search in this location", RiskLevel.Low),
            ["delete"]        = ("Delete", "Move to Recycle Bin", RiskLevel.High),
            ["rename"]        = ("Rename", "Rename this item", RiskLevel.High),
            ["cut"]           = ("Cut", "Cut this file", RiskLevel.High),
            ["copy"]          = ("Copy", "Copy this file", RiskLevel.High),
            ["paste"]         = ("Paste", "Paste from clipboard", RiskLevel.High),
            ["properties"]    = ("Properties", "View file properties", RiskLevel.Medium),
            ["opennewwindow"] = ("Open in new window", "Open in new Explorer window", RiskLevel.Low),
            ["opennewtab"]    = ("Open in new tab", "Open folder in new tab", RiskLevel.Low),
            ["cmd"]           = ("Open command prompt here", "Open CMD in this directory", RiskLevel.Low),
            ["PowerShell"]    = ("Open PowerShell here", "Open PowerShell in this directory", RiskLevel.Low),
            ["powershell"]    = ("Open PowerShell here", "Open PowerShell in this directory", RiskLevel.Low),
            ["Terminal"]      = ("Open in Terminal", "Open in Windows Terminal", RiskLevel.Low),
            ["WSL"]           = ("Open in Linux", "Open Linux shell here via WSL", RiskLevel.Low),
            ["git_gui"]       = ("Git GUI Here", "Open Git GUI here", RiskLevel.Low),
            ["git_shell"]     = ("Git Bash Here", "Open Git Bash here", RiskLevel.Low),
            ["CopyAsPathMenu"]     = ("Copy as path", "Copy full path to clipboard", RiskLevel.Low),
            ["SendTo"]             = ("Send to", "Send file to location", RiskLevel.Low),
            ["OpenWith"]           = ("Open with", "Choose program to open with", RiskLevel.Medium),
            ["Sharing"]            = ("Share", "Share this file or folder", RiskLevel.Low),
            ["PintoStartScreen"]   = ("Pin to Start", "Pin to Start menu", RiskLevel.Low),
            ["PintoTaskbar"]       = ("Pin to taskbar", "Pin to taskbar", RiskLevel.Low),
            ["NewMenu"]            = ("New", "New file or folder here", RiskLevel.High),
            ["WorkFolders"]        = ("Work Folders", "Windows Work Folders sync", RiskLevel.Low),
            ["CompatibilityPage"]  = ("Troubleshoot compatibility", "Run compatibility troubleshooter", RiskLevel.Low),
            ["ModernSharing"]      = ("Share", "Share via modern share panel", RiskLevel.Low),
            ["7-Zip"]              = ("7-Zip", "7-Zip compress/extract menu", RiskLevel.Low),
            ["WinRAR"]             = ("WinRAR", "WinRAR compress/extract menu", RiskLevel.Low),
            ["ANotepad++64"]       = ("Notepad++", "Edit with Notepad++", RiskLevel.Low),
            ["VSCode"]             = ("Visual Studio Code", "Open with VS Code", RiskLevel.Low),
        };

        /// <summary>Returns true if Win10 classic context menu is enabled (key exists and InprocServer32 default value is set).</summary>
        public bool IsClassicMenuEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(Win11ClassicMenuKey);
                if (key == null) return false;
                var defaultValue = key.GetValue("") as string;
                return !string.IsNullOrEmpty(defaultValue);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Enable Win10 classic context menu.</summary>
        public void EnableClassicMenu()
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(Win11ClassicMenuKey);
                key?.SetValue("", "", RegistryValueKind.String);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to enable classic menu: {ex.Message}", ex);
            }
        }

        /// <summary>Restore Win11 new context menu.</summary>
        public void DisableClassicMenu()
        {
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree(
                    @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}",
                    throwOnMissingSubKey: false);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to restore new menu: {ex.Message}", ex);
            }
        }

        /// <summary>Scan all context menu items for the given scenario.</summary>
        public List<ContextMenuItem> ScanMenuItems(MenuScenario scenario)
        {
            var items = new List<ContextMenuItem>();

            if (ShellPaths.TryGetValue(scenario, out var shellPaths))
            {
                foreach (var path in shellPaths)
                {
                    items.AddRange(ScanShellCommands(path, scenario));
                }
            }

            if (ShellExPaths.TryGetValue(scenario, out var shellExPaths))
            {
                foreach (var path in shellExPaths)
                {
                    items.AddRange(ScanShellExtensions(path, scenario));
                }
            }

            // Dedupe by Scenario|ItemType|RegistryPath so same-named items in different scenarios are kept
            return items
                .GroupBy(i => $"{i.Scenario}|{i.ItemType}|{i.RegistryPath}")
                .Select(g => g.First())
                .OrderBy(i => i.DisplayName)
                .ToList();
        }

        /// <summary>Scan menu items for all scenarios.</summary>
        public List<ContextMenuItem> ScanAllMenuItems()
        {
            var all = new List<ContextMenuItem>();
            foreach (MenuScenario scenario in Enum.GetValues<MenuScenario>())
            {
                all.AddRange(ScanMenuItems(scenario));
            }
            return all;
        }

        /// <summary>Disable a menu item.</summary>
        public void DisableItem(ContextMenuItem item)
        {
            switch (item.DisableMethod)
            {
                case DisableMethod.LegacyDisable:
                    DisableShellCommand(item);
                    break;
                case DisableMethod.BlockedClsid:
                    DisableShellExtension(item);
                    break;
                default:
                    break;
            }
        }

        /// <summary>Enable a menu item.</summary>
        public void EnableItem(ContextMenuItem item)
        {
            switch (item.DisableMethod)
            {
                case DisableMethod.LegacyDisable:
                    EnableShellCommand(item);
                    break;
                case DisableMethod.BlockedClsid:
                    EnableShellExtension(item);
                    break;
                default:
                    break;
            }
        }

        /// <summary>Scan shell commands: HKCR first, then HKCU overrides by name. RequiresAdmin true only for HKLM.</summary>
        private List<ContextMenuItem> ScanShellCommands(string registryPath, MenuScenario scenario)
        {
            var byId = new Dictionary<string, ContextMenuItem>(StringComparer.OrdinalIgnoreCase);

            try
            {
                using var hkcr = Registry.ClassesRoot.OpenSubKey(registryPath);
                if (hkcr != null)
                {
                    foreach (var subKeyName in hkcr.GetSubKeyNames())
                    {
                        var item = ReadShellCommand(hkcr, subKeyName, $"HKCR\\{registryPath}\\{subKeyName}", scenario);
                        if (item != null)
                            byId[subKeyName] = item;
                    }
                }

                var hkcuPath = $"Software\\Classes\\{registryPath}";
                using var hkcu = Registry.CurrentUser.OpenSubKey(hkcuPath);
                if (hkcu != null)
                {
                    foreach (var subKeyName in hkcu.GetSubKeyNames())
                    {
                        var item = ReadShellCommand(hkcu, subKeyName, $"HKCU\\{hkcuPath}\\{subKeyName}", scenario);
                        if (item != null)
                            byId[subKeyName] = item;
                    }
                }
            }
            catch
            {
                // Registry read failed, skip
            }

            return byId.Values.ToList();
        }

        /// <summary>Read a single shell command item.</summary>
        private ContextMenuItem? ReadShellCommand(RegistryKey parentKey, string subKeyName, string fullPath, MenuScenario scenario)
        {
            try
            {
                using var subKey = parentKey.OpenSubKey(subKeyName);
                if (subKey == null) return null;

                string displayName = subKey.GetValue("MUIVerb") as string
                    ?? subKey.GetValue("") as string
                    ?? subKeyName;

                if (displayName.StartsWith("@"))
                    displayName = subKeyName;

                string? command = null;
                using (var cmdKey = subKey.OpenSubKey("command"))
                {
                    command = cmdKey?.GetValue("") as string;
                }

                bool isDisabled = subKey.GetValue("LegacyDisable") != null
                    || (subKey.GetValue("ProgrammaticAccessOnly") != null);

                bool isExtended = subKey.GetValue("Extended") != null;

                var (knownName, knownDesc, knownRisk) = GetKnownItemInfo(subKeyName);

                var requiresAdmin = fullPath.StartsWith("HKLM\\");
                var scope = fullPath.StartsWith("HKCU\\") ? RegistryScope.CurrentUser
                    : fullPath.StartsWith("HKLM\\") ? RegistryScope.LocalMachine
                    : RegistryScope.MergedView;

                var effectiveDisplay = (string.IsNullOrEmpty(displayName) || displayName.StartsWith("@"))
                    ? (!string.IsNullOrEmpty(knownName) ? knownName : subKeyName)
                    : displayName;

                var item = new ContextMenuItem
                {
                    Id = subKeyName,
                    DisplayName = CleanDisplayName(effectiveDisplay),
                    Description = !string.IsNullOrEmpty(knownDesc) ? knownDesc : (isExtended ? "Shown when holding Shift and right-clicking" : ""),
                    ItemType = MenuItemType.ShellCommand,
                    DisableMethod = DisableMethod.LegacyDisable,
                    RegistryScope = scope,
                    Scenario = scenario,
                    Source = DetectSource(command, subKeyName),
                    Risk = knownRisk,
                    RegistryPath = fullPath,
                    Command = command,
                    IsEnabled = !isDisabled,
                    OriginalIsEnabled = !isDisabled,
                    RequiresAdmin = requiresAdmin,
                    SourceName = DetectSourceName(command, subKeyName)
                };

                return item;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Scan shell extensions from HKLM and HKCU Software\Classes; HKCU overrides by name.</summary>
        private List<ContextMenuItem> ScanShellExtensions(string registryPath, MenuScenario scenario)
        {
            var blockedClsids = GetBlockedShellExtensions();
            var byId = new Dictionary<string, ContextMenuItem>(StringComparer.OrdinalIgnoreCase);

            var hklmPath = $"Software\\Classes\\{registryPath}";
            var hkcuPath = $"Software\\Classes\\{registryPath}";

            try
            {
                using var hklm = Registry.LocalMachine.OpenSubKey(hklmPath);
                if (hklm != null)
                {
                    foreach (var handlerName in hklm.GetSubKeyNames())
                    {
                        var item = ReadShellExtensionHandler(hklm, handlerName, $"HKLM\\{hklmPath}\\{handlerName}", scenario, blockedClsids, requiresAdmin: true);
                        if (item != null)
                            byId[handlerName] = item;
                    }
                }
            }
            catch { }

            try
            {
                using var hkcu = Registry.CurrentUser.OpenSubKey(hkcuPath);
                if (hkcu != null)
                {
                    foreach (var handlerName in hkcu.GetSubKeyNames())
                    {
                        var item = ReadShellExtensionHandler(hkcu, handlerName, $"HKCU\\{hkcuPath}\\{handlerName}", scenario, blockedClsids, requiresAdmin: false);
                        if (item != null)
                            byId[handlerName] = item;
                    }
                }
            }
            catch { }

            return byId.Values.ToList();
        }

        /// <summary>
        /// Read one shell extension handler from the given root; CLSID description still from HKCR.
        /// </summary>
        private ContextMenuItem? ReadShellExtensionHandler(RegistryKey parentKey, string handlerName, string fullPath, MenuScenario scenario, HashSet<string> blockedClsids, bool requiresAdmin)
        {
            try
            {
                using var handlerKey = parentKey.OpenSubKey(handlerName);
                if (handlerKey == null) return null;

                var clsid = handlerKey.GetValue("") as string;
                if (string.IsNullOrEmpty(clsid)) return null;

                string displayName = handlerName;
                string description = "";
                string sourceName = "";

                using (var clsidKey = Registry.ClassesRoot.OpenSubKey($"CLSID\\{clsid}"))
                {
                    if (clsidKey != null)
                    {
                        var clsidName = clsidKey.GetValue("") as string;
                        if (!string.IsNullOrEmpty(clsidName) && !clsidName.StartsWith("@"))
                            displayName = clsidName;

                        using var inprocKey = clsidKey.OpenSubKey("InprocServer32");
                        var dllPath = inprocKey?.GetValue("") as string;
                        if (!string.IsNullOrEmpty(dllPath))
                            sourceName = DetectSourceFromDll(dllPath);
                    }
                }

                var (knownName, knownDesc, knownRisk) = GetKnownItemInfo(handlerName);
                var effectiveDisplay = (string.IsNullOrEmpty(displayName) || displayName.StartsWith("@"))
                    ? (!string.IsNullOrEmpty(knownName) ? knownName : handlerName)
                    : displayName;

                bool isBlocked = blockedClsids.Contains(clsid);
                var scope = fullPath.StartsWith("HKCU\\") ? RegistryScope.CurrentUser : RegistryScope.LocalMachine;

                return new ContextMenuItem
                {
                    Id = handlerName,
                    DisplayName = CleanDisplayName(effectiveDisplay),
                    Description = !string.IsNullOrEmpty(knownDesc) ? knownDesc : description,
                    ItemType = MenuItemType.ShellExtension,
                    DisableMethod = DisableMethod.BlockedClsid,
                    RegistryScope = scope,
                    Scenario = scenario,
                    Source = IsSystemClsid(clsid) ? MenuSource.System : MenuSource.ThirdParty,
                    Risk = knownRisk,
                    RegistryPath = fullPath,
                    Clsid = clsid,
                    IsEnabled = !isBlocked,
                    OriginalIsEnabled = !isBlocked,
                    RequiresAdmin = requiresAdmin,
                    SourceName = !string.IsNullOrEmpty(sourceName) ? sourceName : ""
                };
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Disable shell command (add LegacyDisable). Create HKCU overlay if HKCR is not writable.</summary>
        private void DisableShellCommand(ContextMenuItem item)
        {
            var (rootKey, subPath) = ParseRegistryPath(item.RegistryPath);
            try
            {
                using var key = rootKey.OpenSubKey(subPath, writable: true);
                if (key != null)
                {
                    key.SetValue("LegacyDisable", "", RegistryValueKind.String);
                    return;
                }
            }
            catch (UnauthorizedAccessException) { }

            var hkcuPath = ConvertToHkcuPath(item.RegistryPath);
            using var hkcuKey = Registry.CurrentUser.CreateSubKey(hkcuPath);
            hkcuKey?.SetValue("LegacyDisable", "", RegistryValueKind.String);
        }

        /// <summary>Enable shell command (remove LegacyDisable). Also clear HKCU overlay.</summary>
        private void EnableShellCommand(ContextMenuItem item)
        {
            var (rootKey, subPath) = ParseRegistryPath(item.RegistryPath);
            using var key = rootKey.OpenSubKey(subPath, writable: true);
            if (key != null)
            {
                try { key.DeleteValue("LegacyDisable", throwOnMissingValue: false); } catch { }
            }

            var hkcuPath = ConvertToHkcuPath(item.RegistryPath);
            try
            {
                using var hkcuKey = Registry.CurrentUser.OpenSubKey(hkcuPath, writable: true);
                hkcuKey?.DeleteValue("LegacyDisable", throwOnMissingValue: false);
            }
            catch { }
        }

        /// <summary>Disable shell extension (add to Blocked). Prefer HKLM (admin), fallback to HKCU.</summary>
        private void DisableShellExtension(ContextMenuItem item)
        {
            if (string.IsNullOrEmpty(item.Clsid)) return;

            try
            {
                using var key = Registry.LocalMachine.CreateSubKey(ShellExtBlockedKey);
                key?.SetValue(item.Clsid, "", RegistryValueKind.String);
            }
            catch (UnauthorizedAccessException)
            {
                using var key = Registry.CurrentUser.CreateSubKey(ShellExtBlockedKey);
                key?.SetValue(item.Clsid, "", RegistryValueKind.String);
            }
        }

        /// <summary>
        /// Enable shell extension (remove from Blocked). Clear both HKLM and HKCU.
        /// </summary>
        private void EnableShellExtension(ContextMenuItem item)
        {
            if (string.IsNullOrEmpty(item.Clsid)) return;

            try
            {
                using var keyLm = Registry.LocalMachine.OpenSubKey(ShellExtBlockedKey, writable: true);
                keyLm?.DeleteValue(item.Clsid, throwOnMissingValue: false);
            }
            catch { }

            try
            {
                using var keyCu = Registry.CurrentUser.OpenSubKey(ShellExtBlockedKey, writable: true);
                keyCu?.DeleteValue(item.Clsid, throwOnMissingValue: false);
            }
            catch { }
        }

        /// <summary>Get the set of blocked shell extension CLSIDs (HKCU and HKLM).</summary>
        private HashSet<string> GetBlockedShellExtensions()
        {
            var blocked = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(ShellExtBlockedKey);
                if (key != null)
                {
                    foreach (var name in key.GetValueNames())
                    {
                        if (!string.IsNullOrEmpty(name))
                            blocked.Add(name);
                    }
                }
            }
            catch { }

            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(ShellExtBlockedKey);
                if (key != null)
                {
                    foreach (var name in key.GetValueNames())
                    {
                        if (!string.IsNullOrEmpty(name))
                            blocked.Add(name);
                    }
                }
            }
            catch { }

            return blocked;
        }

        private (string Name, string Desc, RiskLevel Risk) GetKnownItemInfo(string keyName)
        {
            if (KnownItems.TryGetValue(keyName, out var info))
                return info;
            return ("", "", RiskLevel.Low);
        }

        private static string CleanDisplayName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "(Unnamed)";
            return name.Replace("&", "").Trim();
        }

        private static MenuSource DetectSource(string? command, string keyName)
        {
            if (string.IsNullOrEmpty(command))
                return MenuSource.System;

            var lowerCmd = command.ToLowerInvariant();

            if (lowerCmd.Contains("system32") || lowerCmd.Contains("syswow64") ||
                lowerCmd.Contains("windows\\") || lowerCmd.Contains("%systemroot%"))
                return MenuSource.System;

            var systemKeys = new[] { "open", "edit", "print", "printto", "runas", "explore",
                "find", "delete", "rename", "cut", "copy", "paste", "properties" };
            if (systemKeys.Contains(keyName.ToLowerInvariant()))
                return MenuSource.System;

            return MenuSource.ThirdParty;
        }

        private static string DetectSourceName(string? command, string keyName)
        {
            if (string.IsNullOrEmpty(command)) return "";
            var lower = command.ToLowerInvariant();

            if (lower.Contains("7-zip") || lower.Contains("7z")) return "7-Zip";
            if (lower.Contains("winrar")) return "WinRAR";
            if (lower.Contains("notepad++")) return "Notepad++";
            if (lower.Contains("code.exe") || lower.Contains("vscode")) return "VS Code";
            if (lower.Contains("git")) return "Git";
            if (lower.Contains("tortoise")) return "TortoiseSVN/Git";
            if (lower.Contains("beyond compare")) return "Beyond Compare";
            if (lower.Contains("bandizip")) return "Bandizip";

            return "";
        }

        private static string DetectSourceFromDll(string dllPath)
        {
            var lower = dllPath.ToLowerInvariant();
            if (lower.Contains("7-zip") || lower.Contains("7z")) return "7-Zip";
            if (lower.Contains("winrar")) return "WinRAR";
            if (lower.Contains("notepad++")) return "Notepad++";
            if (lower.Contains("tortoise")) return "TortoiseSVN/Git";
            if (lower.Contains("dropbox")) return "Dropbox";
            if (lower.Contains("onedrive")) return "OneDrive";
            if (lower.Contains("nvidia")) return "NVIDIA";
            if (lower.Contains("intel")) return "Intel";
            if (lower.Contains("amd")) return "AMD";

            try
            {
                var dir = Path.GetDirectoryName(dllPath);
                if (!string.IsNullOrEmpty(dir))
                {
                    var parts = dir.Split(Path.DirectorySeparatorChar);
                    foreach (var part in parts)
                    {
                        if (part.Equals("Program Files", StringComparison.OrdinalIgnoreCase) ||
                            part.Equals("Program Files (x86)", StringComparison.OrdinalIgnoreCase) ||
                            part.Equals("Windows", StringComparison.OrdinalIgnoreCase))
                            continue;
                        if (!string.IsNullOrWhiteSpace(part) && part.Length > 2)
                            return part;
                    }
                }
            }
            catch { }

            return "";
        }

        private static bool IsSystemClsid(string clsid)
        {
            var systemClsids = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "{F81E9010-6EA4-11CE-A7FF-00AA003CA9F6}", // SendTo
                "{09799AFB-AD67-11d1-ABCD-00C04FC30936}", // CopyTo/MoveTo
                "{C2FBB630-2971-11D1-A18C-00C04FD75D13}", // CopyTo
                "{C2FBB631-2971-11D1-A18C-00C04FD75D13}", // MoveTo
                "{49707376-2346-4A76-A68B-C1D00F8A0F82}", // Pin to Start
                "{90AA3A4E-1CBA-4233-B8BB-535773D48449}", // Pin to Taskbar
                "{a2a9545d-a0c2-42b4-9708-a0b2badd77c8}", // New Menu
                "{1f43a58c-ea28-43e6-9ec4-34574a16ebb7}", // OpenWith
                "{E2BF9676-5F8F-435C-97EB-11607A5BEDF7}", // Sharing
            };

            return systemClsids.Contains(clsid);
        }

        private static (RegistryKey Root, string SubPath) ParseRegistryPath(string fullPath)
        {
            if (fullPath.StartsWith("HKCR\\"))
                return (Registry.ClassesRoot, fullPath[5..]);
            if (fullPath.StartsWith("HKCU\\"))
                return (Registry.CurrentUser, fullPath[5..]);
            if (fullPath.StartsWith("HKLM\\"))
                return (Registry.LocalMachine, fullPath[5..]);

            return (Registry.ClassesRoot, fullPath);
        }

        private static string ConvertToHkcuPath(string registryPath)
        {
            if (registryPath.StartsWith("HKCR\\"))
                return "Software\\Classes\\" + registryPath[5..];
            if (registryPath.StartsWith("HKCU\\"))
                return registryPath[5..];
            return registryPath;
        }
    }
}
