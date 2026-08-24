using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Zitie.Desktop.ViewModels;

namespace Zitie.Desktop.Views;

public partial class ModuleGalleryView : UserControl
{
    private const double GalleryHorizontalMargin = 52;

    public ModuleGalleryView()
    {
        InitializeComponent();
        AddHandler(PointerWheelChangedEvent, OnCtrlPointerWheelChanged, RoutingStrategies.Tunnel);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        UpdateGalleryWidth(Bounds.Width);
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateGalleryWidth(e.NewSize.Width);
    }

    private void UpdateGalleryWidth(double viewWidth)
    {
        if (DataContext is ModuleGalleryViewModel viewModel)
            viewModel.UpdateGalleryWidth(Math.Max(1, viewWidth - GalleryHorizontalMargin));
    }

    private void OnCtrlPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) &&
            DataContext is ModuleGalleryViewModel viewModel)
        {
            viewModel.AdjustZoom(e.Delta.Y > 0 ? 0.05 : -0.05);
            e.Handled = true;
        }
    }
}
