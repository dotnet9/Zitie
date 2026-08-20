using Avalonia.Controls;
using Avalonia.Interactivity;
using Prism.Regions;
using Zitie.Desktop.Controls;
using Zitie.Desktop.Services;

namespace Zitie.Desktop.Views;

public partial class MainWindow : ZitieWindow
{
    private readonly IRegionManager? _regionManager;

    public MainWindow()
        : this(null)
    {
    }

    public MainWindow(IRegionManager? regionManager)
    {
        _regionManager = regionManager;
        InitializeComponent();
        AttachWindowChrome(
            TitleBar,
            MinimizeButton,
            MaximizeButton,
            CloseButton,
            (ResizeTopLeft, WindowEdge.NorthWest),
            (ResizeTop, WindowEdge.North),
            (ResizeTopRight, WindowEdge.NorthEast),
            (ResizeLeft, WindowEdge.West),
            (ResizeRight, WindowEdge.East),
            (ResizeBottomLeft, WindowEdge.SouthWest),
            (ResizeBottom, WindowEdge.South),
            (ResizeBottomRight, WindowEdge.SouthEast));
    }

    private void NavigateGalleryButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _regionManager?.RequestNavigate("MainRegion", "ModuleGallery");
        SetActiveNavigation(GalleryNavButton);
    }

    private void NavigateEditorButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _regionManager?.RequestNavigate("MainRegion", "SheetEditor");
        SetActiveNavigation(EditorNavButton);
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

    private void SetActiveNavigation(Button activeButton)
    {
        foreach (var button in new[] { GalleryNavButton, EditorNavButton })
        {
            button.Classes.Set("active", ReferenceEquals(button, activeButton));
        }
    }
}
