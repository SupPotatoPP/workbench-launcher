# Workbench Launcher（工作台）

一个面向 Windows 的个人工作启动台，用项目分组管理解决方案、目录、文档、网页、应用程序、管理员终端和临时草稿。

## 分享版使用方式

解压发布包后直接双击 `WorkbenchLauncher.exe`。分享版为 Windows x64 自包含程序，不要求接收者预先安装 .NET。

每个 Windows 用户的配置独立保存在自己的 `%APPDATA%\WorkbenchLauncher` 中，程序包不包含制作者的私人路径和配置。

## 当前功能

- 自定义名称并配置 Wiki、禅道等网页链接
- 配置 Visual Studio `.sln` / `.slnx`，双击直接打开
- 从解决方案卡片打开所在目录或最近修改的 `bin` 目录
- 配置文件夹、文档和任意应用程序（支持启动参数及工作目录）
- 通过 UAC 以管理员身份打开 PowerShell 或 CMD
- 在指定目录新建带时间戳的 TXT 草稿并立即打开
- 全部资源按分类分组；进入某个分类后再按资源类型分组，支持全文搜索
- 新增、编辑、删除资源；配置自动保存
- JSON 配置导入、导出，并在每次保存前保留上一版本备份
- 分类可拖拽调整；卡片通过插入式拖放调整全局顺序
- 将文件、目录、解决方案或应用直接拖入窗口并自动识别类型，拖放过程带有状态动画
- 右键复制卡片，或复制卡片的目标路径

## 启动

已经构建的程序：

```text
F:\idea\bin\Release\net8.0-windows\WorkbenchLauncher.exe
```

电脑需要安装 .NET 8 Desktop Runtime。开发构建命令：

```powershell
dotnet build .\WorkbenchLauncher.csproj -c Release
```

## 配置位置

配置保存在：

```text
%APPDATA%\WorkbenchLauncher\config.json
```

上一版本配置自动保存在同一目录的 `config.backup.json`。

删除资源只会删除工作台中的入口，不会删除对应的文件、目录、程序或网页内容。

## 操作提示

- 双击资源卡片或点击“打开”启动资源。
- 右键卡片可收藏、打开所在位置、编辑或删除。
- 解决方案卡片直接提供解决方案目录和最新 `bin` 目录入口，文档和应用卡片可直接打开所在目录。
- 管理员终端启动时会出现 Windows UAC 确认，这是预期行为。
