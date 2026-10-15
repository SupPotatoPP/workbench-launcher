using System.IO;

namespace WorkbenchLauncher;

public static class ResourcePresentation
{
    public static IEnumerable<LaunchItem> Order(IEnumerable<LaunchItem> items, LauncherData data)
    {
        var sources = data.FolderSources.GroupBy(x => x.Name).ToDictionary(x => x.Key, x => x.First());
        return items.GroupBy(x => x.Project)
            .OrderBy(g => g.Key == CategoryService.PinnedCategory ? 0 : 1)
            .ThenBy(g => g.Min(x => x.CategoryOrder))
            .ThenBy(g => g.Key, StringComparer.CurrentCultureIgnoreCase)
            .SelectMany(g =>
            {
                if (sources.TryGetValue(g.Key, out var source) && !source.UseManualOrder)
                    return source.NameDescending
                        ? g.OrderByDescending(x => x.Name, StringComparer.CurrentCultureIgnoreCase).ThenBy(x => x.Id)
                        : g.OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase).ThenBy(x => x.Id);
                return g.OrderBy(x => x.SortOrder).ThenBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase);
            });
    }

    public static void RefreshMetadata(LaunchItem item)
    {
        item.ModifiedAt = null;
        if (item.Kind is ResourceKind.Website or ResourceKind.AdminPowerShell or ResourceKind.AdminCmd) return;
        try
        {
            var path = Environment.ExpandEnvironmentVariables(item.Target);
            FileSystemInfo info = Directory.Exists(path) ? new DirectoryInfo(path) : new FileInfo(path);
            if (info.Exists) item.ModifiedAt = info.LastWriteTime;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or System.Security.SecurityException)
        {
            // Unavailable paths remain visible; no fabricated modification date.
        }
    }
}
