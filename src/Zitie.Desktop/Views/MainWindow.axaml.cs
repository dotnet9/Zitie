using Avalonia.Controls;
using Avalonia.Interactivity;
using Zitie.Desktop.Services;

namespace Zitie.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void ToggleLogMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        LogHost.Content ??= ZitieLogging.CreateView();
        LogPanel.IsVisible = !LogPanel.IsVisible;
    }

    private void CloseLogButton_OnClick(object? sender, RoutedEventArgs e)
    {
        LogPanel.IsVisible = false;
    }
}
