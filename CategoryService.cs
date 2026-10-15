namespace WorkbenchLauncher;

public static class CategoryService
{
    public const string PinnedCategory = "常用工具";

    public static List<string> GetNames(LauncherData data) => data.Items.Select(x => (Name: x.Project, Order: x.CategoryOrder))
        .Concat(data.FolderSources.Select(x => (Name: x.Name, Order: x.CategoryOrder)))
        .Append((Name: PinnedCategory, Order: 0))
        .Where(x => !string.IsNullOrWhiteSpace(x.Name))
        .GroupBy(x => x.Name).OrderBy(g => g.Key == PinnedCategory ? 0 : 1)
        .ThenBy(g => g.Min(x => x.Order)).ThenBy(g => g.Key, StringComparer.CurrentCultureIgnoreCase)
        .Select(g => g.Key).ToList();

    public static bool Remove(LauncherData data, string name)
    {
        if (name == PinnedCategory) return false;
        var sourceIds = data.FolderSources.Where(x => x.Name == name).Select(x => x.Id).ToHashSet();
        data.FolderSources.RemoveAll(x => x.Name == name);
        data.Items.RemoveAll(x => x.Project == name || (x.SourceFolderId is Guid id && sourceIds.Contains(id)));
        return true;
    }
}
