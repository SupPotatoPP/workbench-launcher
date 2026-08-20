using System.IO;
using System.Text.Json;

namespace WorkbenchLauncher;

public sealed class DataStore
{
    private readonly string _file = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WorkbenchLauncher", "config.json");
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    public LauncherData Load()
    {
        try
        {
            if (File.Exists(_file)) return JsonSerializer.Deserialize<LauncherData>(File.ReadAllText(_file), Options) ?? new();
        }
        catch { }
        return CreateSample();
    }

    public void Save(LauncherData data)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_file)!);
        if (File.Exists(_file)) File.Copy(_file, Path.Combine(Path.GetDirectoryName(_file)!, "config.backup.json"), true);
        File.WriteAllText(_file, JsonSerializer.Serialize(data, Options));
    }

    public void Export(string destination, LauncherData data) => File.WriteAllText(destination, JsonSerializer.Serialize(data, Options));

    public LauncherData Import(string source)
    {
        var data = JsonSerializer.Deserialize<LauncherData>(File.ReadAllText(source), Options)
            ?? throw new InvalidDataException("配置文件内容为空。 ");
        data.Items ??= [];
        return data;
    }

    private static LauncherData CreateSample() => new()
    {
        Items =
        [
            new() { Name = "项目目录", Kind = ResourceKind.Folder, Target = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), Project = "示例项目" },
            new() { Name = "项目 Wiki", Kind = ResourceKind.Website, Target = "https://example.com/wiki", Project = "示例项目" },
            new() { Name = "管理员 PowerShell", Kind = ResourceKind.AdminPowerShell, Project = "常用工具" },
            new() { Name = "临时草稿", Kind = ResourceKind.Draft, Target = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), Project = "常用工具" }
        ]
    };
}
