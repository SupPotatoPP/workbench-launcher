namespace WorkbenchLauncher;

public partial class App : System.Windows.Application
{
    static App()
    {
        // Some restricted launchers omit WINDIR; WPF's font cache requires it.
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WINDIR")))
            Environment.SetEnvironmentVariable("WINDIR", Environment.GetFolderPath(Environment.SpecialFolder.Windows));
    }
}
