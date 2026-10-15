using WorkbenchLauncher;
using System.Text.Json;

var root = Path.Combine(AppContext.BaseDirectory, "sync-fixture-" + Guid.NewGuid());
Directory.CreateDirectory(root);
void Check(bool condition, string message) { if (!condition) throw new Exception(message); Console.WriteLine("PASS: " + message); }
try
{
    var source = new FolderSource { Name = "项目", Path = root, CategoryOrder = 3 };
    var manual = new LaunchItem { Name = "手动资源", Project = "其他" };
    var data = new LauncherData { FolderSources = [source], Items = [manual] };
    Check(FolderSync.Refresh(data).Count == 0 && data.Items.Count == 1 && data.FolderSources.Count == 1, "空主目录保留分类及手动资源");
    var a = Directory.CreateDirectory(Path.Combine(root, "01常州")).FullName;
    var b = Directory.CreateDirectory(Path.Combine(root, "02益阳")).FullName;
    Directory.CreateDirectory(Path.Combine(a, "不递归显示"));
    File.WriteAllText(Path.Combine(root, "不显示.txt"), "test");
    Check(FolderSync.Refresh(data).Count == 0 && data.Items.Count == 3, "仅展示第一层子文件夹，忽略文件和更深目录");
    var item = data.Items.Single(x => x.Target == a);
    var id = item.Id;
    item.Favorite = true; item.SortOrder = 10; item.LastOpened = DateTime.Now;
    FolderSync.Refresh(data);
    Check(data.Items.Count == 3 && item.Id == id && item.Favorite && item.SortOrder == 10 && item.LastOpened != null, "重复刷新无重复卡片，保留收藏、排序、最近使用及标识");
    Directory.Delete(b);
    Directory.CreateDirectory(Path.Combine(root, "03银川"));
    FolderSync.Refresh(data);
    Check(data.Items.Count == 3 && !data.Items.Any(x => x.Target == b) && data.Items.Any(x => x.Name == "03银川"), "同步新增与删除的子文件夹");
    var categoryItems = data.Items.Where(x => x.Project == source.Name);
    Check(ResourcePresentation.Order(categoryItems, data).Select(x => x.Name).SequenceEqual(new[] { "01常州", "03银川" }), "默认按子文件夹名称正序，忽略历史手动序号");
    source.NameDescending = true;
    Check(ResourcePresentation.Order(categoryItems, data).Select(x => x.Name).SequenceEqual(new[] { "03银川", "01常州" }), "切换名称倒序");
    Directory.CreateDirectory(Path.Combine(root, "02益阳"));
    FolderSync.Refresh(data);
    Check(ResourcePresentation.Order(data.Items.Where(x => x.Project == source.Name), data).Select(x => x.Name).SequenceEqual(new[] { "03银川", "02益阳", "01常州" }), "倒序刷新后新文件夹进入正确位置");
    source.UseManualOrder = true;
    Check(ResourcePresentation.Order(categoryItems, data).Select(x => x.Id).SequenceEqual(categoryItems.OrderBy(x => x.SortOrder).ThenBy(x => x.Name).Select(x => x.Id)), "手动拖动顺序仍可保留");
    source.UseManualOrder = false;
    data.UseListView = true;
    var viewRoundtrip = JsonSerializer.Deserialize<LauncherData>(JsonSerializer.Serialize(data))!;
    Check(viewRoundtrip.UseListView && viewRoundtrip.FolderSources.Single().NameDescending, "视图和各分类排序方向保存到配置");
    ResourcePresentation.RefreshMetadata(item);
    Check(item.ModifiedAt == Directory.GetLastWriteTime(a) && item.ModifiedAtLabel != "—", "修改日期来自实际文件夹");
    var website = new LaunchItem { Kind = ResourceKind.Website, Target = "https://example.com" };
    ResourcePresentation.RefreshMetadata(website);
    Check(website.ModifiedAtLabel == "—", "网页等无文件日期资源显示占位符");
    Directory.Delete(Path.Combine(root, "02益阳"));
    FolderSync.Refresh(data);
    source.Path = Path.Combine(root, "不存在的路径");
    Check(FolderSync.Refresh(data).Count == 1 && data.Items.Count == 3, "路径不可用时报错并保留已有卡片");
    var restored = JsonSerializer.Deserialize<LauncherData>(JsonSerializer.Serialize(data))!;
    Check(restored.FolderSources.Single().Id == source.Id && restored.Items.Any(x => x.SourceFolderId == source.Id), "配置序列化保留主路径和自动卡片关联");
    Check(JsonSerializer.Deserialize<LauncherData>("{\"Items\":[]}")!.FolderSources.Count == 0, "兼容原版配置");
    data.Items.Add(new LaunchItem { Project = CategoryService.PinnedCategory, CategoryOrder = 999 });
    Check(CategoryService.GetNames(data)[0] == CategoryService.PinnedCategory, "常用工具始终位于分类第一位");
    Check(!CategoryService.Remove(data, CategoryService.PinnedCategory) && data.Items.Any(x => x.Project == CategoryService.PinnedCategory), "禁止删除常用工具分类");
    Check(CategoryService.Remove(data, "其他") && !data.Items.Contains(manual), "普通分类删除其资源入口");
    Check(CategoryService.Remove(data, source.Name) && data.FolderSources.Count == 0 && !data.Items.Any(x => x.SourceFolderId == source.Id), "主文件夹分类删除来源及自动卡片");
    Check(Directory.Exists(a), "删除分类不删除磁盘文件夹");
    Check(CategoryService.GetNames(new LauncherData()).SequenceEqual(new[] { CategoryService.PinnedCategory }), "空配置也保留固定分类");
}
finally { Directory.Delete(root, true); }
