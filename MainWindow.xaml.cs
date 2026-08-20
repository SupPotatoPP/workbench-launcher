using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Data;
using Microsoft.Win32;
using System.IO;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace WorkbenchLauncher;

public partial class MainWindow : Window
{
    private readonly DataStore _store = new();
    private LauncherData _data = new();
    private readonly ObservableCollection<LaunchItem> _visible = [];
    private string _filter = "All";
    private Point _dragStart;
    private Point _categoryDragStart;
    private ListBoxItem? _dragHoverContainer;
    private const string CardDragFormat = "WorkbenchLauncher.Card";
    private const string CategoryDragFormat = "WorkbenchLauncher.Category";

    public MainWindow()
    {
        InitializeComponent();
        _data = _store.Load();
        if (NormalizeOrdering()) _store.Save(_data);
        ItemsList.ItemsSource = _visible;
        RefreshProjects(); Refresh();
    }

    private void RefreshProjects()
    {
        ProjectList.ItemsSource = _data.Items.Where(x => !string.IsNullOrWhiteSpace(x.Project))
            .GroupBy(x => x.Project).OrderBy(g => g.Min(x => x.CategoryOrder)).ThenBy(g => g.Key)
            .Select(g => g.Key).ToList();
    }

    private void Refresh()
    {
        IEnumerable<LaunchItem> query = _data.Items;
        if (_filter == "Favorite") query = query.Where(x => x.Favorite);
        else if (_filter == "Recent") query = query.Where(x => x.LastOpened != null).OrderByDescending(x => x.LastOpened);
        else if (_filter.StartsWith("Project:")) query = query.Where(x => x.Project == _filter[8..]);
        var term = SearchBox.Text.Trim();
        if (term.Length > 0) query = query.Where(x => ($"{x.Name} {x.Target} {x.Project}").Contains(term, StringComparison.OrdinalIgnoreCase));
        query = query.OrderBy(x => x.CategoryOrder).ThenBy(x => x.SortOrder).ThenBy(x => x.Name);
        _visible.Clear(); foreach (var item in query) _visible.Add(item);
        var view = CollectionViewSource.GetDefaultView(ItemsList.ItemsSource);
        view.GroupDescriptions.Clear();
        if (_filter == "All")
            view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(LaunchItem.Project)));
        else if (_filter.StartsWith("Project:"))
            view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(LaunchItem.KindLabel)));
        CountText.Text = $"{_visible.Count} 个资源";
        EmptyText.Visibility = _visible.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Filter_Click(object sender, RoutedEventArgs e)
    {
        _filter = (string)((Button)sender).Tag; ProjectList.SelectedItem = null;
        PageTitle.Text = _filter switch { "Favorite" => "收藏", "Recent" => "最近使用", _ => "全部资源" }; Refresh();
        UpdateNavigation();
    }

    private void ProjectList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProjectList.SelectedItem is not string project) return;
        _filter = "Project:" + project; PageTitle.Text = project; Refresh(); UpdateNavigation();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => Refresh();
    private void Add_Click(object sender, RoutedEventArgs e)
    {
        var item = new LaunchItem(); var dialog = new EditItemWindow(item) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        _data.Items.Add(item); SaveAndRefresh();
    }

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "工作台配置 (*.json)|*.json|所有文件 (*.*)|*.*", CheckFileExists = true };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            var imported = _store.Import(dialog.FileName);
            if (MessageBox.Show(this, $"将导入 {imported.Items.Count} 个资源并替换当前配置。\n当前配置会自动备份，是否继续？", "导入配置", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            _data = imported; NormalizeOrdering(); _store.Save(_data);
            _filter = "All"; ProjectList.SelectedItem = null;
            PageTitle.Text = "全部资源"; RefreshProjects(); Refresh(); UpdateNavigation();
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "配置导入失败", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { Filter = "工作台配置 (*.json)|*.json", FileName = $"工作台配置_{DateTime.Now:yyyyMMdd}.json", AddExtension = true };
        if (dialog.ShowDialog(this) != true) return;
        try { _store.Export(dialog.FileName, _data); MessageBox.Show(this, "配置已经导出。", "导出完成", MessageBoxButton.OK, MessageBoxImage.Information); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "配置导出失败", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private void ItemsList_MouseDoubleClick(object sender, MouseButtonEventArgs e) { if (ItemsList.SelectedItem is LaunchItem item) Open(item); }
    private void ItemsList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) => _dragStart = e.GetPosition(ItemsList);

    private void ItemsList_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        var current = e.GetPosition(ItemsList);
        if (Math.Abs(current.X - _dragStart.X) < SystemParameters.MinimumHorizontalDragDistance && Math.Abs(current.Y - _dragStart.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        if (FindParent<Button>(e.OriginalSource as DependencyObject) != null) return;
        var container = FindParent<ListBoxItem>(e.OriginalSource as DependencyObject);
        if (container?.DataContext is not LaunchItem item) return;
        container.Opacity = 0.42;
        try { DragDrop.DoDragDrop(container, new DataObject(CardDragFormat, item.Id.ToString()), DragDropEffects.Move); }
        finally { container.Opacity = 1; ClearDragVisuals(); }
    }

    private void ItemsList_DragOver(object sender, DragEventArgs e)
    {
        var isFile = e.Data.GetDataPresent(DataFormats.FileDrop);
        var isCard = e.Data.GetDataPresent(CardDragFormat);
        e.Effects = isFile ? DragDropEffects.Copy : isCard ? DragDropEffects.Move : DragDropEffects.None;
        ItemsList.Background = new System.Windows.Media.SolidColorBrush(isFile
            ? System.Windows.Media.Color.FromRgb(238, 242, 255)
            : System.Windows.Media.Color.FromRgb(245, 246, 250));
        var container = FindParent<ListBoxItem>(e.OriginalSource as DependencyObject);
        if (_dragHoverContainer != container)
        {
            if (_dragHoverContainer != null) _dragHoverContainer.Opacity = 1;
            _dragHoverContainer = container;
            if (_dragHoverContainer != null) _dragHoverContainer.Opacity = 0.68;
        }
        e.Handled = true;
    }

    private void ItemsList_DragLeave(object sender, DragEventArgs e)
    {
        if (e.OriginalSource == ItemsList) ClearDragVisuals();
    }

    private void ItemsList_Drop(object sender, DragEventArgs e)
    {
        ClearDragVisuals();
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] paths) AddDroppedPaths(paths);
            return;
        }
        if (!e.Data.GetDataPresent(CardDragFormat)) return;
        if (!Guid.TryParse(e.Data.GetData(CardDragFormat)?.ToString(), out var sourceId)) return;
        var source = _data.Items.FirstOrDefault(x => x.Id == sourceId);
        var targetContainer = FindParent<ListBoxItem>(e.OriginalSource as DependencyObject);
        var target = targetContainer?.DataContext as LaunchItem;
        if (source == null || target == null || source == target) return;
        if (source.Project != target.Project) { MessageBox.Show(this, "卡片只能在同一分类内调整顺序。", "无法移动"); return; }
        if (_filter.StartsWith("Project:") && source.Kind != target.Kind)
        {
            MessageBox.Show(this, "当前页面按资源类型分组，卡片只能在同一类型内调整顺序。", "无法插入");
            return;
        }
        var sourceIndexes = _data.Items.Select((item, index) => (item, index)).ToDictionary(x => x.item.Id, x => x.index);
        var categoryItems = _data.Items.Where(x => x.Project == source.Project).OrderBy(x => x.SortOrder).ThenBy(x => sourceIndexes[x.Id]).ToList();
        categoryItems.Remove(source);
        var insertIndex = categoryItems.IndexOf(target);
        var pointer = e.GetPosition(targetContainer!);
        if (pointer.Y > targetContainer!.ActualHeight / 2) insertIndex++;
        categoryItems.Insert(Math.Clamp(insertIndex, 0, categoryItems.Count), source);
        for (var i = 0; i < categoryItems.Count; i++) categoryItems[i].SortOrder = i;
        SaveAndRefresh(); AnimateCard(source);
    }

    private void AddDroppedPaths(IEnumerable<string> paths)
    {
        var added = new List<LaunchItem>();
        foreach (var path in paths)
        {
            if (!File.Exists(path) && !Directory.Exists(path)) continue;
            var item = CreateItemFromPath(path);
            var dialog = new EditItemWindow(item) { Owner = this };
            if (dialog.ShowDialog() == true) { _data.Items.Add(item); added.Add(item); }
        }
        SaveAndRefresh();
        foreach (var item in added) AnimateCard(item);
    }

    private LaunchItem CreateItemFromPath(string path)
    {
        var kind = Directory.Exists(path) ? ResourceKind.Folder : Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".sln" or ".slnx" => ResourceKind.Solution,
            ".exe" or ".bat" or ".cmd" => ResourceKind.Application,
            ".doc" or ".docx" or ".xls" or ".xlsx" or ".ppt" or ".pptx" or ".pdf" or ".txt" or ".md" => ResourceKind.Document,
            _ => ResourceKind.Document
        };
        var category = _filter.StartsWith("Project:") ? _filter[8..] : "未分类";
        return new LaunchItem { Name = Directory.Exists(path) ? new DirectoryInfo(path).Name : Path.GetFileNameWithoutExtension(path), Target = path, Kind = kind, Project = category, SortOrder = _data.Items.Count };
    }

    private void ProjectList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) => _categoryDragStart = e.GetPosition(ProjectList);

    private void ProjectList_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        var current = e.GetPosition(ProjectList);
        if (Math.Abs(current.X - _categoryDragStart.X) < SystemParameters.MinimumHorizontalDragDistance && Math.Abs(current.Y - _categoryDragStart.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        var container = FindParent<ListBoxItem>(e.OriginalSource as DependencyObject);
        if (container?.DataContext is string category) DragDrop.DoDragDrop(container, new DataObject(CategoryDragFormat, category), DragDropEffects.Move);
    }

    private void ProjectList_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(CategoryDragFormat)) return;
        var source = e.Data.GetData(CategoryDragFormat)?.ToString();
        var targetContainer = FindParent<ListBoxItem>(e.OriginalSource as DependencyObject);
        var target = targetContainer?.DataContext as string;
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target) || source == target) return;
        var categories = _data.Items.GroupBy(x => x.Project).OrderBy(g => g.Min(x => x.CategoryOrder)).ThenBy(g => g.Key).Select(g => g.Key).ToList();
        categories.Remove(source); categories.Insert(categories.IndexOf(target), source);
        for (var i = 0; i < categories.Count; i++) foreach (var item in _data.Items.Where(x => x.Project == categories[i])) item.CategoryOrder = i;
        SaveAndRefresh(); ProjectList.SelectedItem = source;
    }

    private static T? FindParent<T>(DependencyObject? child) where T : DependencyObject
    {
        while (child != null) { if (child is T match) return match; child = System.Windows.Media.VisualTreeHelper.GetParent(child); }
        return null;
    }

    private void ClearDragVisuals()
    {
        ItemsList.Background = System.Windows.Media.Brushes.Transparent;
        if (_dragHoverContainer != null) _dragHoverContainer.Opacity = 1;
        _dragHoverContainer = null;
    }

    private void AnimateCard(LaunchItem item)
    {
        Dispatcher.BeginInvoke(() =>
        {
            ItemsList.UpdateLayout();
            var container = FindCardContainer(ItemsList, item);
            if (container == null) return;
            container.BeginAnimation(OpacityProperty, new DoubleAnimation(0.25, 1, TimeSpan.FromMilliseconds(260)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } });
        }, DispatcherPriority.Loaded);
    }

    private static ListBoxItem? FindCardContainer(DependencyObject root, LaunchItem item)
    {
        for (var i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
            if (child is ListBoxItem container && ReferenceEquals(container.DataContext, item)) return container;
            var nested = FindCardContainer(child, item);
            if (nested != null) return nested;
        }
        return null;
    }

    private bool NormalizeOrdering()
    {
        var changed = false;
        var originalIndexes = _data.Items.Select((item, index) => (item, index)).ToDictionary(x => x.item.Id, x => x.index);
        var categories = _data.Items.GroupBy(x => x.Project)
            .OrderBy(g => g.Min(x => x.CategoryOrder)).ThenBy(g => g.Key, StringComparer.CurrentCultureIgnoreCase).ToList();
        for (var categoryIndex = 0; categoryIndex < categories.Count; categoryIndex++)
        {
            var orderedItems = categories[categoryIndex].OrderBy(x => x.SortOrder).ThenBy(x => originalIndexes[x.Id]).ToList();
            for (var itemIndex = 0; itemIndex < orderedItems.Count; itemIndex++)
            {
                var item = orderedItems[itemIndex];
                if (item.CategoryOrder != categoryIndex) { item.CategoryOrder = categoryIndex; changed = true; }
                if (item.SortOrder != itemIndex) { item.SortOrder = itemIndex; changed = true; }
            }
        }
        return changed;
    }
    private void Open_Click(object sender, RoutedEventArgs e) { if (((FrameworkElement)sender).DataContext is LaunchItem item) Open(item); }
    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is LaunchItem item) Run(() => LauncherService.OpenContainingFolder(item));
    }
    private void OpenBin_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is LaunchItem item) Run(() => LauncherService.OpenBinDirectory(item));
    }
    private void Open(LaunchItem item)
    {
        try { LauncherService.Open(item); _store.Save(_data); }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223) { }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "无法打开", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private void ItemsList_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        var container = FindParent<ListBoxItem>(e.OriginalSource as DependencyObject);
        if (container?.DataContext is not LaunchItem item) return;
        ItemsList.SelectedItem = item;
        ShowContextMenu(item);
        e.Handled = true;
    }

    private void ShowContextMenu(LaunchItem item)
    {
        var menu = new ContextMenu();
        AddMenu(menu, "打开", () => Open(item));
        AddMenu(menu, item.Favorite ? "取消收藏" : "收藏", () => { item.Favorite = !item.Favorite; SaveAndRefresh(); });
        AddMenu(menu, "复制卡片", () => DuplicateItem(item));
        if (!string.IsNullOrWhiteSpace(item.Target)) AddMenu(menu, "复制目标路径", () => Clipboard.SetText(item.Target));
        if (item.Kind is not (ResourceKind.Website or ResourceKind.AdminPowerShell or ResourceKind.AdminCmd))
            AddMenu(menu, "打开所在位置", () => Run(() => LauncherService.OpenContainingFolder(item)));
        if (item.Kind == ResourceKind.Solution)
        {
            AddMenu(menu, "打开解决方案目录", () => Run(() => LauncherService.OpenSolutionDirectory(item)));
            AddMenu(menu, "打开最新 bin 目录", () => Run(() => LauncherService.OpenBinDirectory(item)));
        }
        menu.Items.Add(new Separator());
        AddMenu(menu, "编辑", () => { if (new EditItemWindow(item) { Owner = this }.ShowDialog() == true) SaveAndRefresh(); });
        AddMenu(menu, "删除", () => { if (MessageBox.Show(this, $"确定删除“{item.Name}”吗？", "删除资源", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes) { _data.Items.Remove(item); SaveAndRefresh(); } });
        menu.PlacementTarget = ItemsList;
        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
        menu.IsOpen = true;
    }

    private void DuplicateItem(LaunchItem source)
    {
        var copy = new LaunchItem
        {
            Name = source.Name + " - 副本", Kind = source.Kind, Target = source.Target, Arguments = source.Arguments,
            WorkingDirectory = source.WorkingDirectory, Project = source.Project, Favorite = false,
            SortOrder = _data.Items.Where(x => x.Project == source.Project).Select(x => x.SortOrder).DefaultIfEmpty().Max() + 1,
            CategoryOrder = source.CategoryOrder
        };
        _data.Items.Add(copy); SaveAndRefresh(); ItemsList.SelectedItem = copy;
    }

    private void UpdateNavigation()
    {
        AllButton.Background = _filter == "All" ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(44, 51, 84)) : System.Windows.Media.Brushes.Transparent;
        FavoriteButton.Background = _filter == "Favorite" ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(44, 51, 84)) : System.Windows.Media.Brushes.Transparent;
        RecentButton.Background = _filter == "Recent" ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(44, 51, 84)) : System.Windows.Media.Brushes.Transparent;
        AllButton.Foreground = _filter == "All" ? System.Windows.Media.Brushes.White : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(197, 202, 219));
        FavoriteButton.Foreground = _filter == "Favorite" ? System.Windows.Media.Brushes.White : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(197, 202, 219));
        RecentButton.Foreground = _filter == "Recent" ? System.Windows.Media.Brushes.White : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(197, 202, 219));
    }

    private static void AddMenu(ContextMenu menu, string name, Action action) { var entry = new MenuItem { Header = name }; entry.Click += (_, _) => action(); menu.Items.Add(entry); }
    private void Run(Action action) { try { action(); } catch (Exception ex) { MessageBox.Show(this, ex.Message, "操作失败", MessageBoxButton.OK, MessageBoxImage.Warning); } }
    private void SaveAndRefresh() { _store.Save(_data); RefreshProjects(); Refresh(); }
}
