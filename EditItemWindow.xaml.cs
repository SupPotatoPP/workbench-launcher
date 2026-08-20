using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace WorkbenchLauncher;

public partial class EditItemWindow : Window
{
    private readonly LaunchItem _item;
    private sealed record KindOption(ResourceKind Value, string Label) { public override string ToString() => Label; }

    public EditItemWindow(LaunchItem item)
    {
        InitializeComponent(); _item = item;
        KindBox.ItemsSource = new[]
        {
            new KindOption(ResourceKind.Folder, "文件夹"), new KindOption(ResourceKind.Solution, "Visual Studio 解决方案"),
            new KindOption(ResourceKind.Document, "文档（WPS / Office / PDF）"), new KindOption(ResourceKind.Website, "网页（Wiki / 禅道等）"),
            new KindOption(ResourceKind.Application, "应用程序"), new KindOption(ResourceKind.AdminPowerShell, "管理员 PowerShell"),
            new KindOption(ResourceKind.AdminCmd, "管理员 CMD"), new KindOption(ResourceKind.Draft, "新建 TXT 草稿")
        };
        NameBox.Text = item.Name; ProjectBox.Text = item.Project; TargetBox.Text = item.Target;
        ArgumentsBox.Text = item.Arguments; WorkingDirectoryBox.Text = item.WorkingDirectory; FavoriteBox.IsChecked = item.Favorite;
        KindBox.SelectedItem = ((IEnumerable<KindOption>)KindBox.ItemsSource).First(x => x.Value == item.Kind);
    }

    private void KindBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (KindBox.SelectedItem is not KindOption option) return;
        var noTarget = option.Value is ResourceKind.AdminPowerShell or ResourceKind.AdminCmd;
        TargetBox.IsEnabled = BrowseButton.IsEnabled = !noTarget;
        TargetLabel.Text = option.Value switch { ResourceKind.Website => "网址", ResourceKind.Draft => "草稿保存目录", _ => "目标路径" };
        HintText.Text = option.Value switch
        {
            ResourceKind.AdminPowerShell or ResourceKind.AdminCmd => "启动时 Windows 会显示 UAC 管理员授权窗口。",
            ResourceKind.Draft => "每次打开都会在指定目录创建一个带时间戳的 TXT 文件。",
            ResourceKind.Solution => "保存后可从更多菜单直接打开解决方案目录和最新的 bin 目录。",
            ResourceKind.Website => "支持 Wiki、禅道和其他 HTTP/HTTPS 链接。",
            _ => "双击卡片将使用 Windows 默认程序打开。"
        };
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        if (KindBox.SelectedItem is not KindOption option) return;
        if (option.Value is ResourceKind.Folder or ResourceKind.Draft) { PickFolder(TargetBox); return; }
        var dialog = new OpenFileDialog { CheckFileExists = true };
        dialog.Filter = option.Value switch
        {
            ResourceKind.Solution => "Visual Studio 解决方案 (*.sln;*.slnx)|*.sln;*.slnx|所有文件 (*.*)|*.*",
            ResourceKind.Application => "应用程序 (*.exe;*.bat;*.cmd)|*.exe;*.bat;*.cmd|所有文件 (*.*)|*.*",
            ResourceKind.Document => "文档|*.doc;*.docx;*.xls;*.xlsx;*.ppt;*.pptx;*.pdf;*.txt;*.md|所有文件 (*.*)|*.*",
            _ => "所有文件 (*.*)|*.*"
        };
        if (dialog.ShowDialog(this) == true) TargetBox.Text = dialog.FileName;
    }

    private void BrowseWorking_Click(object sender, RoutedEventArgs e) => PickFolder(WorkingDirectoryBox);
    private static void PickFolder(System.Windows.Controls.TextBox target)
    {
        var dialog = new OpenFolderDialog { Title = "选择目录", Multiselect = false };
        if (Directory.Exists(target.Text)) dialog.InitialDirectory = target.Text;
        if (dialog.ShowDialog() == true) target.Text = dialog.FolderName;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text)) { MessageBox.Show(this, "请输入资源名称。", "缺少名称"); NameBox.Focus(); return; }
        if (KindBox.SelectedItem is not KindOption option) return;
        if (option.Value is not (ResourceKind.AdminPowerShell or ResourceKind.AdminCmd) && string.IsNullOrWhiteSpace(TargetBox.Text)) { MessageBox.Show(this, "请配置目标路径或网址。", "缺少目标"); return; }
        if (option.Value == ResourceKind.Website && !Uri.TryCreate(TargetBox.Text, UriKind.Absolute, out var uri)) { MessageBox.Show(this, "请输入完整网址，例如 https://example.com。", "网址无效"); return; }
        _item.Name = NameBox.Text.Trim(); _item.Kind = option.Value; _item.Project = string.IsNullOrWhiteSpace(ProjectBox.Text) ? "未分类" : ProjectBox.Text.Trim();
        _item.Target = TargetBox.Text.Trim(); _item.Arguments = ArgumentsBox.Text.Trim(); _item.WorkingDirectory = WorkingDirectoryBox.Text.Trim(); _item.Favorite = FavoriteBox.IsChecked == true;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
