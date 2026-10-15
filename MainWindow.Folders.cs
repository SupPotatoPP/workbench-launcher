using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;

namespace WorkbenchLauncher;

public partial class MainWindow
{
    private void SyncFolders()
    {
        var errors = FolderSync.Refresh(_data);
        if (errors.Count > 0) MessageBox.Show(this, string.Join("\n", errors), "部分主文件夹无法读取", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void RefreshFolders_Click(object sender, RoutedEventArgs e)
    {
        SyncFolders(); SaveAndRefresh();
    }

    private void AddFolderSource_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "选择主文件夹：其名称作为分类，子文件夹自动显示" };
        if (dialog.ShowDialog(this) != true) return;
        var path = Path.TrimEndingDirectorySeparator(Path.GetFullPath(dialog.FolderName));
        var existing = _data.FolderSources.FirstOrDefault(x => string.Equals(x.Path, path, StringComparison.OrdinalIgnoreCase));
        if (existing != null) { SyncFolders(); ProjectList.SelectedItem = existing.Name; Refresh(); return; }
        var name = new DirectoryInfo(path).Name;
        if (CategoryService.GetNames(_data).Contains(name))
        {
            MessageBox.Show(this, "已有同名分类，请先调整文件夹名称或现有分类名称，再添加。", "分类名称重复"); return;
        }
        _data.FolderSources.Add(new FolderSource { Name = name, Path = path,
            CategoryOrder = _data.Items.Select(x => x.CategoryOrder).Concat(_data.FolderSources.Select(x => x.CategoryOrder)).DefaultIfEmpty(-1).Max() + 1 });
        SyncFolders(); SaveAndRefresh(); ProjectList.SelectedItem = name;
    }

    private void ProjectList_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        var container = FindParent<ListBoxItem>(e.OriginalSource as DependencyObject);
        if (container?.DataContext is not string name) return;
        var source = _data.FolderSources.FirstOrDefault(x => x.Name == name);
        var menu = new ContextMenu();
        if (name == CategoryService.PinnedCategory)
        {
            menu.Items.Add(new MenuItem { Header = "常用工具（固定分类）", IsEnabled = false });
        }
        else
        {
            if (source != null)
            {
                AddMenu(menu, "打开主文件夹", () => Run(() => LauncherService.Open(new LaunchItem { Target = source.Path })));
                AddMenu(menu, "刷新子文件夹", () => { SyncFolders(); SaveAndRefresh(); });
            }
            AddMenu(menu, "删除分类", () =>
            {
                var count = _data.Items.Count(x => x.Project == name);
                if (MessageBox.Show(this, $"确定删除分类“{name}”及其中的 {count} 个资源入口吗？\n磁盘上的文件和文件夹不会删除。", "删除分类", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
                CategoryService.Remove(_data, name);
                if (_filter == "Project:" + name) { _filter = "All"; PageTitle.Text = "全部资源"; }
                SaveAndRefresh(); UpdateNavigation();
            });
        }
        menu.PlacementTarget = container; menu.IsOpen = true; e.Handled = true;
    }
}
