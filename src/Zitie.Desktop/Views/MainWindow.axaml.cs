using Avalonia;
using Avalonia.Interactivity;
using CodeWF.Avalonia.Controls.Controls;
using Prism.Ioc;
using Zitie.Desktop.ViewModels;

namespace Zitie.Desktop.Views;

public partial class MainWindow : CodeWFWindow
{
    private AboutWindow? _about;
    public MainWindow() => InitializeComponent();

    private void OnShowAbout(object? sender, RoutedEventArgs e)
    {
        if (_about is not null) { _about.Activate(); return; }
        _about = new AboutWindow { DataContext = ((App)Application.Current!).Container.Resolve<AboutViewModel>(), Icon = Icon };
        _about.Closed += (_, _) => _about = null;
        _about.ShowDialog(this);
    }
}
