# Context Menu Manager

**Manage and restore Windows right-click menus (Windows 10/11)**

A desktop tool to safely and clearly manage Windows 10/11 context menu items.

## Features

### Core
- **Toggle menu style**: Switch between Win10 classic and Win11 new context menu with one click (**current user only**; uses HKCU `{86ca1aa0-...}` key, not system-wide).
- **Four scenarios**: Browse items by **File** / **Directory** / **Directory background** / **Desktop background**.
- **Per-item toggles**: Enable or disable each menu item independently.
- **Search**: Quickly find menu items by name or description.

### Safety
- **Disable, don’t delete**: All actions are reversible enable/disable; no registry keys are removed.
- **Auto backup on first run**: Saves the current configuration as “default” for easy rollback.
- **Auto backup before apply**: A backup is created before each apply.
- **Restore**: Restore from the last backup or reset to default.
- **Least privilege**: Only current-user settings are changed by default; administrator is requested when needed.

### Transparency
- Each item shows: name, description, source (system/third-party), risk level, and scope (current user / all users).
- Risk levels come from built-in rules (e.g. system handlers, Explorer, file-open related items) and may be updated in future versions.
- Unapplied changes are clearly indicated.
- Status bar reflects the result of actions.

### Language
- UI is available in **English** (default) and **中文**. Use the dropdown next to the title to switch.
- The choice is stored in `%APPDATA%\ContextMenuManager\settings.json`; the app restarts to apply.
- The single-file .exe includes both languages; no extra files are required.

## Requirements

- Windows 10 / Windows 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (to build)
- .NET 8 runtime (to run)

## Build and run

### Build
```bash
dotnet build
```

### Run
```bash
dotnet run
```

### Publish single-file EXE (no .NET runtime install needed)

**Option 1: Script (recommended; does not change system execution policy)**
```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\Scripts\publish-standalone.ps1"
```
Or from the project root: `.\Scripts\publish-standalone.cmd`  
Output: `src\dist\` with a single `ContextMenuManager.exe`.

**Option 2: Command line**
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o src/dist
```

## Project layout

```
ContextMenuManager/
├── src/
│   ├── App.xaml, App.xaml.cs           # App entry (resource dictionary ref only)
│   ├── Converters/                     # WPF value converters
│   │   └── ValueConverters.cs
│   ├── Models/                         # Data models & enums
│   │   └── ContextMenuItem.cs
│   ├── Services/                       # Registry, backup, language, Explorer helpers
│   │   ├── BackupService.cs
│   │   ├── ExplorerHelper.cs
│   │   ├── LanguageService.cs
│   │   └── RegistryService.cs
│   ├── Styles/                         # Global styles & color resources
│   │   └── AppStyles.xaml
│   ├── ViewModels/                     # Main UI logic and commands
│   │   ├── MainViewModel.cs
│   │   └── RelayCommand.cs
│   ├── Views/                          # WPF windows & pages
│   │   ├── MainWindow.xaml
│   │   └── MainWindow.xaml.cs
│   └── dist/                           # Publish output (gitignored)
├── ContextMenuManager.csproj
├── Assets/
│   ├── app.ico
│   └── app.manifest
├── Properties/
│   ├── Resources.resx, *.zh-CN.resx   # UI strings (en / zh-CN)
│   └── PublishProfiles/               # Publish config (standalone exe)
├── Scripts/
│   ├── publish-standalone.ps1         # Script to build standalone exe
│   └── publish-standalone.cmd         # Launcher (bypasses execution policy)
└── README.md
```

### Design summary

| Action              | How it’s done                                                                 | Reversible |
|---------------------|-------------------------------------------------------------------------------|------------|
| Disable shell command | Add `LegacyDisable` registry value                                         | Yes, remove value |
| Disable shell extension | Add CLSID to `Shell Extensions\Blocked`                                   | Yes, remove value |
| Toggle classic menu | Create/delete HKCU `{86ca1aa0-...}\InprocServer32` (current user only)       | Yes |
| Backup              | JSON under `%APPDATA%\ContextMenuManager\Backups`                            | Yes, restore anytime |

## Notes

- After changing the context menu, **restart Explorer** for it to take effect (hint at the bottom of the window and a “Restart Explorer” button).
- Some HKCR (system) changes may require administrator; the app will ask before doing them.
- Backups are in `%APPDATA%\ContextMenuManager\Backups\`. The first run saves a “default” backup for rollback.

## License

MIT
