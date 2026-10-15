using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WorkbenchLauncher;

public partial class MainWindow
{
    private ListBox ActiveItemsControl => _data.UseListView ? DetailsList : ItemsList;
    private FolderSource? CurrentFolderSource => _filter.StartsWith("Project:")
        ? _data.FolderSources.FirstOrDefault(x => x.Name == _filter[8..]) : null;

    private void UpdatePresentation()
    {
        ItemsList.Visibility = _data.UseListView ? Visibility.Collapsed : Visibility.Visible;
        DetailsList.Visibility = _data.UseListView ? Visibility.Visible : Visibility.Collapsed;
        ViewButton.Content = _data.UseListView ? "切换为卡片" : "切换为列表";
        ViewButton.ToolTip = _data.UseListView ? "当前：列表视图" : "当前：卡片视图";
        var source = CurrentFolderSource;
        NameSortButton.IsEnabled = source != null;
        NameSortButton.Content = source?.UseManualOrder == true ? "名称排序（当前手动）"
            : source?.NameDescending == true ? "名称：倒序 ↓" : "名称：正序 ↑";
        NameSortButton.ToolTip = source == null ? "进入主文件夹分类后，可切换子文件夹名称排序"
            : source.UseManualOrder ? "点击恢复按名称正序排列"
            : source.NameDescending ? "点击改为按名称正序排列" : "点击改为按名称倒序排列";
        ResizeDetailColumns();
    }

    private void NameSort_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentFolderSource is not { } source) return;
        source.NameDescending = !source.UseManualOrder && !source.NameDescending;
        source.UseManualOrder = false;
        Run(() => { _store.Save(_data); Refresh(); });
    }

    private void View_Click(object sender, RoutedEventArgs e)
    {
        var selected = ActiveItemsControl.SelectedItem;
        _data.UseListView = !_data.UseListView;
        Run(() => { _store.Save(_data); Refresh(); });
        ActiveItemsControl.SelectedItem = selected;
    }

    private void DetailsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (FindParent<ListViewItem>(e.OriginalSource as DependencyObject)?.DataContext is LaunchItem item)
            Open(item);
    }

    private void DetailsList_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void DetailsList_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] paths) AddDroppedPaths(paths);
        e.Handled = true;
    }

    private void DetailsList_SizeChanged(object sender, SizeChangedEventArgs e) => ResizeDetailColumns();

    private void ResizeDetailColumns()
    {
        if (DetailsList.View is GridView grid && grid.Columns.Count > 0 && DetailsList.ActualWidth > 0)
            grid.Columns[0].Width = Math.Max(220, DetailsList.ActualWidth - 390);
    }
}
