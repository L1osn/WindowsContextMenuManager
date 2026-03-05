# Context Menu Manager

**Manage and restore Windows right-click menus (Windows 10/11)**

A desktop tool to safely manage Windows 10/11 context menu items.

---

[English](#english) | [中文 (Chinese)](#中文)

---

<a name="english"></a>

## English

### Features

#### Core

* **Toggle menu style**: Switch between Win10 classic and Win11 menu (**current user only**, uses HKCU `{86ca1aa0-...}`).
* **Four scenarios**: Manage items under **File**, **Directory**, **Directory background**, and **Desktop background**.
* **Per-item toggles**: Enable or disable items individually.
* **Search**: Find menu entries by name or description.

#### Safety

* **Disable instead of delete**: Changes are reversible; registry keys are not removed.
* **Auto backup on first run**: Saves the current configuration as default.
* **Backup before apply**: A backup is created before each change.
* **Restore**: Revert to the last backup or the default state.
* **Least privilege**: Only current-user settings are modified unless admin access is required.

#### Transparency

* Each item displays name, description, source (system/third-party), risk level, and scope.
* Risk levels follow built-in rules (e.g., Explorer handlers, system components).
* Pending changes are clearly indicated.
* Status bar shows action results.

#### Language

* UI supports **English** (default) and **中文**.
* Language selection is saved in `%APPDATA%\ContextMenuManager\settings.json`.
* Restart required to apply.
* The standalone EXE includes both languages.

---

### Project Layout

```
ContextMenuManager/
├── src/
│   ├── App.xaml, App.xaml.cs
│   ├── Converters/
│   ├── Models/
│   ├── Services/
│   ├── Styles/
│   │   └── AppStyles.xaml
│   ├── ViewModels/
│   ├── Views/
│   │   ├── MainWindow.xaml
│   │   └── MainWindow.xaml.cs
│   └── dist/                 # publish output (gitignored)
│       └── win-x64/
│           ├── ContextMenuManager.exe
│           └── lib/
├── ContextMenuManager.csproj
├── Assets/
│   ├── app.ico
│   └── app.manifest
├── Properties/
│   ├── Resources.resx, *.zh-CN.resx
│   └── PublishProfiles/
├── Scripts/
│   ├── publish-standalone.ps1
│   └── publish-standalone.cmd
└── README.md
```

---

### Requirements

* Windows 10 / Windows 11
* [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (build)
* .NET 8 runtime (run)

---

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

```powershell
# Default (win-x64)
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\Scripts\publish-standalone.ps1"

# ARM64
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\Scripts\publish-standalone.ps1" -Arch "win-arm64"
```

Output: `src\dist\<arch>\` containing `ContextMenuManager.exe` (and `lib/` for debug symbols).

**Option 2: Command line**

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o src/dist
```

---

### Design Summary

| Action                  | Method                                             | Reversible |
| ----------------------- | -------------------------------------------------- | ---------- |
| Disable shell command   | Add `LegacyDisable` registry value                 | Yes        |
| Disable shell extension | Add CLSID to `Shell Extensions\Blocked`            | Yes        |
| Toggle classic menu     | Create/delete HKCU `{86ca1aa0-...}\InprocServer32` | Yes        |
| Backup                  | JSON in `%APPDATA%\ContextMenuManager\Backups`     | Yes        |

---

## License

MIT License

---

<a name="中文"></a>

## 中文 (Chinese)

### 功能特性

#### 核心功能

* **切换菜单风格**：一键切换 Win10 经典菜单与 Win11 新菜单（**仅当前用户**，修改 HKCU `{86ca1aa0-...}`）。
* **四类场景**：管理 **文件 / 文件夹 / 文件夹背景 / 桌面背景** 菜单。
* **独立开关**：每个菜单项可单独启用或禁用。
* **快速搜索**：按名称或描述查找菜单项。

#### 安全性

* **禁用而非删除**：操作完全可逆，不删除注册表键。
* **首次运行备份**：自动保存当前配置为默认。
* **应用前备份**：每次修改前自动创建备份。
* **恢复功能**：可恢复至最近备份或默认状态。
* **最小权限原则**：默认只修改当前用户设置，需要时请求管理员权限。

#### 透明性

* 每个项目显示：名称、描述、来源（系统/第三方）、风险等级、作用范围。
* 风险等级基于内置规则（Explorer、系统组件等）。
* 未应用更改会明确标记。
* 状态栏显示操作结果。

#### 多语言

* 支持 **English**（默认）和 **中文**。
* 设置保存在 `%APPDATA%\ContextMenuManager\settings.json`。
* 重启应用生效。
* 单文件 EXE 内置双语资源。

---

### 系统要求

* Windows 10 / Windows 11
* [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)（编译）
* .NET 8 runtime（运行）

---

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

**方法 1：脚本（推荐）**

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\Scripts\publish-standalone.ps1"
```

**ARM64**

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\Scripts\publish-standalone.ps1" -Arch "win-arm64"
```

输出目录：`src\dist\<arch>\`。

---

### 开源许可

MIT

