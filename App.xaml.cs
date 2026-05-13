using System.Windows;

namespace WindowsCleanBooster;

public partial class App : Application
{
    public App()
    {
        this.DispatcherUnhandledException += (s, e) =>
        {
            MessageBox.Show(e.Exception.ToString(), "Unhandled UI exception");
            e.Handled = true;
        };
    }
}