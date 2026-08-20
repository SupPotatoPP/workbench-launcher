using System.Text.Json.Serialization;

namespace WorkbenchLauncher;

public enum ResourceKind { Folder, Solution, Document, Website, Application, AdminPowerShell, AdminCmd, Draft }

public sealed class LaunchItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "新资源";
    public ResourceKind Kind { get; set; }
    public string Target { get; set; } = "";
    public string Arguments { get; set; } = "";
    public string WorkingDirectory { get; set; } = "";
    public string Project { get; set; } = "默认项目";
    public bool Favorite { get; set; }
    public int SortOrder { get; set; }
    public int CategoryOrder { get; set; }
    public DateTime? LastOpened { get; set; }
    [JsonIgnore] public string KindLabel => Kind switch
    {
        ResourceKind.Folder => "文件夹", ResourceKind.Solution => "VS 解决方案",
        ResourceKind.Document => "文档", ResourceKind.Website => "网页",
        ResourceKind.Application => "应用程序", ResourceKind.AdminPowerShell => "管理员 PowerShell",
        ResourceKind.AdminCmd => "管理员 CMD", ResourceKind.Draft => "新建草稿", _ => Kind.ToString()
    };
    [JsonIgnore] public string Icon => Kind switch
    {
        ResourceKind.Folder => "📁", ResourceKind.Solution => "◈", ResourceKind.Document => "▤",
        ResourceKind.Website => "↗", ResourceKind.Application => "⬢", ResourceKind.AdminPowerShell => ">_",
        ResourceKind.AdminCmd => "C:\\", ResourceKind.Draft => "✎", _ => "•"
    };
    [JsonIgnore] public string Accent => Kind switch
    {
        ResourceKind.Solution => "#7357D9", ResourceKind.Document => "#2F78D1", ResourceKind.Website => "#159B8A",
        ResourceKind.Application => "#E0713B", ResourceKind.AdminPowerShell or ResourceKind.AdminCmd => "#39465E",
        ResourceKind.Draft => "#C18B20", _ => "#5B67F1"
    };
    [JsonIgnore] public string CardBackground => Kind switch
    {
        ResourceKind.Solution => "#FBF9FF", ResourceKind.Document => "#F8FBFF", ResourceKind.Website => "#F7FCFB",
        ResourceKind.Application => "#FFFAF7", ResourceKind.Draft => "#FFFCF5", _ => "#FFFFFF"
    };
    [JsonIgnore] public bool ShowDirectoryButton => Kind is ResourceKind.Solution or ResourceKind.Document or ResourceKind.Application;
    [JsonIgnore] public bool ShowBinButton => Kind == ResourceKind.Solution;
    [JsonIgnore] public string Description => string.IsNullOrWhiteSpace(Target) ? KindLabel : Target;
}

public sealed class LauncherData
{
    public List<LaunchItem> Items { get; set; } = [];
}
