using System.IO;

namespace WorkbenchLauncher;

public static class FolderSync
{
    // Only replace entries after enumeration succeeds: unavailable drives must not erase saved cards.
    public static List<string> Refresh(LauncherData data)
    {
        var errors = new List<string>();
        foreach (var source in data.FolderSources)
        {
            try
            {
                var paths = Directory.GetDirectories(source.Path)
                    .OrderBy(Path.GetFileName, StringComparer.CurrentCultureIgnoreCase).ToList();
                var existing = data.Items.Where(x => x.SourceFolderId == source.Id).ToList();
                var pathsSet = paths.ToHashSet(StringComparer.OrdinalIgnoreCase);
                data.Items.RemoveAll(x => x.SourceFolderId == source.Id && !pathsSet.Contains(x.Target));
                foreach (var path in paths)
                {
                    var item = existing.FirstOrDefault(x => string.Equals(x.Target, path, StringComparison.OrdinalIgnoreCase));
                    if (item == null)
                    {
                        item = new LaunchItem { SourceFolderId = source.Id, Target = path,
                            SortOrder = data.Items.Where(x => x.Project == source.Name).Select(x => x.SortOrder).DefaultIfEmpty(-1).Max() + 1 };
                        data.Items.Add(item);
                    }
                    item.Name = Path.GetFileName(path);
                    item.Kind = ResourceKind.Folder;
                    item.Project = source.Name;
                    item.CategoryOrder = source.CategoryOrder;
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException or ArgumentException)
            {
                errors.Add($"{source.Name}：{ex.Message}");
            }
        }
        return errors;
    }
}
