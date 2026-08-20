using System.Diagnostics;
using System.IO;

namespace WorkbenchLauncher;

public static class LauncherService
{
    public static void Open(LaunchItem item)
    {
        switch (item.Kind)
        {
            case ResourceKind.AdminPowerShell:
                Elevated("powershell.exe", item.Arguments, item.WorkingDirectory); break;
            case ResourceKind.AdminCmd:
                Elevated("cmd.exe", item.Arguments, item.WorkingDirectory); break;
            case ResourceKind.Draft:
                CreateDraft(item.Target); break;
            default:
                Shell(item.Target, item.Arguments, item.WorkingDirectory); break;
        }
        item.LastOpened = DateTime.Now;
    }

    public static void OpenContainingFolder(LaunchItem item)
    {
        var target = Environment.ExpandEnvironmentVariables(item.Target);
        if (Directory.Exists(target)) Shell(target);
        else if (File.Exists(target)) Process.Start("explorer.exe", $"/select,\"{target}\"");
        else throw new FileNotFoundException("目标路径不存在。", target);
    }

    public static void OpenSolutionDirectory(LaunchItem item) => Shell(Path.GetDirectoryName(item.Target) ?? item.Target);

    public static void OpenBinDirectory(LaunchItem item)
    {
        var root = Directory.Exists(item.Target) ? item.Target : Path.GetDirectoryName(item.Target);
        if (string.IsNullOrWhiteSpace(root)) throw new DirectoryNotFoundException("无法确定项目目录。");
        var bins = Directory.EnumerateDirectories(root, "bin", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(Directory.GetLastWriteTime).ToList();
        if (bins.Count == 0) throw new DirectoryNotFoundException("项目中还没有找到 bin 目录，请先编译项目。 ");
        Shell(bins[0]);
    }

    private static void CreateDraft(string folder)
    {
        folder = Environment.ExpandEnvironmentVariables(folder);
        if (!Directory.Exists(folder)) throw new DirectoryNotFoundException($"草稿目录不存在：{folder}");
        var baseName = $"草稿_{DateTime.Now:yyyyMMdd_HHmmss}";
        var path = Path.Combine(folder, baseName + ".txt");
        for (var i = 2; File.Exists(path); i++) path = Path.Combine(folder, $"{baseName}_{i}.txt");
        File.WriteAllText(path, "");
        Shell(path);
    }

    private static void Shell(string target, string arguments = "", string workingDirectory = "")
    {
        if (string.IsNullOrWhiteSpace(target)) throw new InvalidOperationException("请先配置目标路径或网址。");
        Process.Start(new ProcessStartInfo
        {
            FileName = Environment.ExpandEnvironmentVariables(target), Arguments = arguments,
            WorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory) ? "" : Environment.ExpandEnvironmentVariables(workingDirectory),
            UseShellExecute = true
        });
    }

    private static void Elevated(string file, string arguments, string workingDirectory) => Process.Start(new ProcessStartInfo
    {
        FileName = file, Arguments = arguments, Verb = "runas", UseShellExecute = true,
        WorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory) ? "" : Environment.ExpandEnvironmentVariables(workingDirectory)
    });
}
