# Workbench Launcher（工作台）

面向 Windows 的个人工作启动台，集中管理文件夹、Visual Studio 解决方案、文档、网页、应用程序、管理员终端和临时草稿。

## 下载与运行

- [下载 v1.1.0 Windows x64 压缩包](https://github.com/SupPotatoPP/workbench-launcher/releases/download/v1.1.0/WorkbenchLauncher-v1.1.0-win-x64.zip)
- [查看全部版本和更新说明](https://github.com/SupPotatoPP/workbench-launcher/releases)

解压 ZIP 后，双击 `WorkbenchLauncher.exe` 即可运行。发布版为 Windows x64 自包含单文件程序，无需安装 .NET。

```text
WorkbenchLauncher-v1.1.0-win-x64/
├─ WorkbenchLauncher.exe
├─ README.md
└─ workbench-icon.png
```

## 本地开发中的新功能

- 主文件夹分类默认按子文件夹名称正序排列，点击资源区上方“名称：正序 / 倒序”切换方向；每个主文件夹独立记住设置，刷新后继续按所选方向排列。
- 点击“切换为列表 / 卡片”切换显示方式，列表包含名称、修改日期、类型三列，不显示大小；修改日期读取磁盘文件或文件夹的实际时间，无对应文件的资源显示“—”。
- 列表支持双击打开、右键操作、搜索和文件拖入，视图设置会保存。卡片拖动后使用手动顺序，点击名称排序可恢复自动排序。

以上功能尚未发布到下方 v1.1.0 下载包，可通过源码启动体验。

## v1.1.0 更新内容

- **主文件夹自动分类**：只选择一次主路径，主文件夹名称自动成为左侧分类，右侧显示其第一层子文件夹，无需逐个添加。
- **刷新同步**：点击“刷新”或重新进入分类，同步新增、删除的子文件夹；已有卡片保留收藏、排序和最近使用记录。
- **分类删除**：右键普通分类，选择“删除分类”，移除分类及资源入口，不删除磁盘文件和文件夹。
- **常用工具固定置顶**：“常用工具”始终位于分类列表第一位，不可删除或拖动，即使为空也保留。
- 修正“最近使用”的排序，使资源按最近打开时间显示。

## 使用方法

### 添加主文件夹

1. 点击左侧“＋ 添加主文件夹”，选择主路径，例如 `E:\项目`。
2. 左侧自动出现“项目”分类，右侧显示主路径下的第一层子文件夹卡片。
3. 点击卡片“打开”或双击卡片，在资源管理器中打开对应子文件夹。
4. 主路径下新增或删除子文件夹后，点击“刷新”或重新进入分类。

仅显示第一层子文件夹，不递归展开更深目录，也不显示主路径下的普通文件。空主文件夹仍保留分类；路径暂时不可用时保留已保存的卡片。同名分类会提示冲突。

右键主文件夹分类，还可以打开主路径、刷新或删除分类。自动生成卡片的名称和路径由磁盘同步，不支持手动编辑或删除；可以收藏、复制及拖动排序。

### 管理其他资源

- 点击右上角“新增资源”，手动添加文件夹、解决方案、文档、网页、应用程序、管理员终端或 TXT 草稿入口。
- 支持应用程序启动参数和工作目录；支持 Wiki、禅道等网页链接。
- 双击卡片或点击“打开”启动资源；右键卡片可收藏、复制卡片、复制目标路径及管理手动资源。
- 解决方案卡片提供所在目录和最近修改的 `bin` 目录入口；文档和应用卡片可打开所在位置。
- 可直接拖入文件、目录、解决方案或应用；卡片可在同一分类内拖动排序，按类型分组时限同一类型。
- 除固定的“常用工具”外，分类可拖动排序。
- 支持名称、路径和分类搜索，以及“全部资源”“收藏”“最近使用”视图。
- 管理员终端启动时会出现 Windows UAC 授权窗口；TXT 草稿会在指定目录创建带时间戳的新文件。

## 配置与升级

v1.1.0 沿用本次预览版的独立配置目录：

```text
%APPDATA%\WorkbenchLauncherPreview\config.json
```

- 已使用预览版的用户会继续使用已设置好的分类和资源。
- 若新版配置尚不存在，首次启动会读取原版 `%APPDATA%\WorkbenchLauncher\config.json` 作为初始数据；后续保存到新版目录，不修改原版配置。
- 每次保存前，上一版本配置备份为同目录下的 `config.backup.json`。
- 点击“导出”可另外保存 JSON 配置；“导入”会在确认后替换当前配置，并自动备份。
- 每个 Windows 用户的配置独立保存，发布包不包含私人路径和配置。

删除资源或分类仅移除工作台入口，不会删除对应的文件、目录、程序或网页内容。

## 本地开发

技术栈：C#、WPF、.NET 8。需要 Windows 和 [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)。

```powershell
git clone https://github.com/SupPotatoPP/workbench-launcher.git
cd workbench-launcher
dotnet build .\WorkbenchLauncher.csproj -c Release
dotnet run --project .\WorkbenchLauncher.csproj
```

运行验证项目，检查子文件夹同步、旧配置兼容、分类删除和固定分类：

```powershell
dotnet run --project .\verification\FolderSync.Tests.csproj
```

## 打包发布

在项目根目录执行：

```powershell
.\publish.ps1 -Version 1.1.0
```

脚本生成 Windows x64 自包含单文件 EXE，以及以下发布目录和 ZIP：

```text
artifacts/package/WorkbenchLauncher-v1.1.0-win-x64/
artifacts/package/WorkbenchLauncher-v1.1.0-win-x64.zip
```

## 主要源码

| 文件 | 作用 |
| --- | --- |
| `MainWindow.xaml` / `MainWindow.xaml.cs` | 主界面、资源筛选、拖拽及右键操作 |
| `MainWindow.Folders.cs` | 主文件夹添加、刷新和分类菜单 |
| `FolderSync.cs` | 第一层子文件夹同步 |
| `CategoryService.cs` | 固定分类排序和分类删除 |
| `EditItemWindow.xaml` / `EditItemWindow.xaml.cs` | 手动资源编辑窗口 |
| `Models.cs` | 资源和主文件夹数据结构 |
| `DataStore.cs` | 配置读取、保存、备份及导入导出 |
| `LauncherService.cs` | 启动资源及创建草稿 |
