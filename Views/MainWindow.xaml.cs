using System.Windows;
using System.Windows.Input;
using WindowsCleanBooster.ViewModels;

namespace WindowsCleanBooster.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new WindowsCleanBooster.ViewModels.MainViewModel();
    }
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }
        else
        {
            DragMove();
        }
    }

    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void Maximize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
    private void TargetsGrid_CurrentCellChanged(object? sender, EventArgs e)
    {
        if (DataContext is WindowsCleanBooster.ViewModels.MainViewModel vm)
            vm.RefreshModeText();
    }
}