# Context Menu Manager

**Manage and restore Windows right-click menus (Windows 10/11)**

A desktop tool to safely and clearly manage Windows 10/11 context menu items.

---

[English](#english) | [中文 (Chinese)](#中文)

---

<a name="english"></a>
## English

### Features

#### Core
- **Toggle menu style**: Switch between Win10 classic and Win11 new context menu with one click (**current user only**; uses HKCU `{86ca1aa0-...}` key, not system-wide).
- **Four scenarios**: Browse items by **File** / **Directory** / **Directory background** / **Desktop background**.
- **Per-item toggles**: Enable or disable each menu item independently.
- **Search**: Quickly find menu items by name or description.

#### Safety
- **Disable, don’t delete**: All actions are reversible enable/disable; no registry keys are removed.
- **Auto backup on first run**: Saves the current configuration as “default” for easy rollback.
- **Auto backup before apply**: A backup is created before each apply.
- **Restore**: Restore from the last backup or reset to default.
- **Least privilege**: Only current-user settings are changed by default; administrator is requested when needed.

#### Transparency
- Each item shows: name, description, source (system/third-party), risk level, and scope (current user / all users).
- Risk levels come from built-in rules (e.g. system handlers, Explorer, file-open related items) and may be updated in future versions.
- Unapplied changes are clearly indicated.
- Status bar reflects the result of actions.

#### Language
- UI is available in **English** (default) and **中文**. Use the dropdown next to the title to switch.
- The choice is stored in `%APPDATA%\ContextMenuManager\settings.json`; the app restarts to apply.
- The single-file .exe includes both languages; no extra files are required.

### Requirements

- Windows 10 / Windows 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (to build)
- .NET 8 runtime (to run)

### Build and Run

#### Build
```bash
dotnet build
```

#### Run
```bash
dotnet run
```

#### Publish single-file EXE

**Option 1: Script (recommended)**
Supports automatic size optimization and architecture selection.
```powershell
# Default (win-x64)
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\Scripts\publish-standalone.ps1"

# ARM64
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\Scripts\publish-standalone.ps1" -Arch "win-arm64"
```
Output: `src\dist\<arch>\` containing `ContextMenuManager.exe` (and a `lib/` folder for debug symbols).

**Option 2: Command line**
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o src/dist
```

### Project Layout

```
ContextMenuManager/
├── src/
│   ├── App.xaml, App.xaml.cs           # App entry & Startup logic
│   ├── Converters/                     # WPF value converters
│   ├── Models/                         # Data models & enums
│   ├── Services/                       # Registry, backup, language, Explorer helpers
│   ├── Styles/                         # Global styles & color resources
│   │   └── AppStyles.xaml
│   ├── ViewModels/                     # Main UI logic and commands
│   ├── Views/                          # WPF windows & pages
│   │   ├── MainWindow.xaml
│   │   └── MainWindow.xaml.cs
│   └── dist/                           # Publish output (gitignored)
│       └── win-x64/
│           ├── ContextMenuManager.exe  # Standalone executable
│           └── lib/                    # Debug symbols (pdb) and config
├── ContextMenuManager.csproj
├── Assets/
│   ├── app.ico
│   └── app.manifest
├── Properties/
│   ├── Resources.resx, *.zh-CN.resx    # UI strings (en / zh-CN)
│   └── PublishProfiles/                # Publish config
├── Scripts/
│   ├── publish-standalone.ps1          # Build script (supports x64/ARM64)
│   └── publish-standalone.cmd          # Launcher
└── README.md
```

### Design Summary

| Action              | How it’s done                                                                 | Reversible |
|---------------------|-------------------------------------------------------------------------------|------------|
| Disable shell command | Add `LegacyDisable` registry value                                         | Yes, remove value |
| Disable shell extension | Add CLSID to `Shell Extensions\Blocked`                                   | Yes, remove value |
| Toggle classic menu | Create/delete HKCU `{86ca1aa0-...}\InprocServer32` (current user only)       | Yes |
| Backup              | JSON under `%APPDATA%\ContextMenuManager\Backups`                            | Yes, restore anytime |

---

<a name="中文"></a>
## 中文 (Chinese)

### 功能特性

#### 核心功能
- **切换右键菜单风格**：一键切换 Win10 经典菜单和 Win11 新菜单（**仅限当前用户**；修改 HKCU `{86ca1aa0-...}`，不影响系统全局）。
- **四大场景**：支持管理 **文件** / **文件夹** / **文件夹背景** / **桌面背景** 的右键菜单。
- **独立开关**：每个菜单项都可以独立启用或禁用。
- **快速搜索**：通过名称或描述快速查找菜单项。

#### 安全性
- **禁用而非删除**：所有操作均为可逆的“禁用/启用”，不删除注册表键值。
- **首次运行备份**：首次运行时自动备份当前配置为“默认”，方便随时回滚。
- **应用前备份**：每次应用更改前自动创建备份。
- **一键还原**：支持还原至上一次备份或重置为初始状态。
- **最小权限**：默认仅修改当前用户设置；需要修改系统项时会请求管理员权限。

#### 透明化
- 每个项目显示：名称、描述、来源（系统/第三方）、风险等级和作用范围（当前用户/所有用户）。
- 风险等级基于内置规则（如系统处理程序、Explorer 组件等），并将持续更新。
- 未应用的更改会清晰标记。
- 状态栏实时反馈操作结果。

#### 多语言
- 界面支持 **English**（默认）和 **中文**。点击标题栏旁边的下拉菜单即可切换。
-设置保存在 `%APPDATA%\ContextMenuManager\settings.json`中，重启应用生效。
- 单文件 EXE 内置双语资源，无需额外文件。

### 系统要求

- Windows 10 / Windows 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (编译需要)
- .NET 8 runtime (运行需要)

### 构建与运行

#### 编译
```bash
dotnet build
```

#### 运行
```bash
dotnet run
```

#### 发布单文件 EXE

**方法 1：使用脚本（推荐）**
支持自动体积优化和架构选择。
```powershell
# 默认 (win-x64)
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\Scripts\publish-standalone.ps1"

# ARM64
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\Scripts\publish-standalone.ps1" -Arch "win-arm64"
```
输出目录：`src\dist\<arch>\`，包含 `ContextMenuManager.exe`（以及存放调试符号的 `lib/` 文件夹）。

**方法 2：命令行**
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o src/dist
```

### 项目结构

```
ContextMenuManager/
├── src/
│   ├── App.xaml, App.xaml.cs           # 程序入口与启动逻辑
│   ├── Converters/                     # WPF 值转换器
│   ├── Models/                         # 数据模型与枚举
│   ├── Services/                       # 注册表、备份、语言服务等
│   ├── Styles/                         # 全局样式与颜色资源
│   │   └── AppStyles.xaml
│   ├── ViewModels/                     # 主 UI 逻辑与命令
│   ├── Views/                          # WPF 窗口与页面
│   │   ├── MainWindow.xaml
│   │   └── MainWindow.xaml.cs
│   └── dist/                           # 发布输出目录 (gitignored)
│       └── win-x64/
│           ├── ContextMenuManager.exe  # 独立可执行文件
│           └── lib/                    # 调试符号 (pdb) 与配置
├── ContextMenuManager.csproj
├── Assets/
│   ├── app.ico
│   └── app.manifest
├── Properties/
│   ├── Resources.resx, *.zh-CN.resx    # UI 字符串资源 (英/中)
│   └── PublishProfiles/                # 发布配置
├── Scripts/
│   ├── publish-standalone.ps1          # 构建脚本 (支持 x64/ARM64)
│   └── publish-standalone.cmd          # 启动器
└── README.md
```

### 开源许可

MIT

- Backups are in `%APPDATA%\ContextMenuManager\Backups\`. The first run saves a “default” backup for rollback.

## License

MIT

