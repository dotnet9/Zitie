using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Zitie.Desktop.ViewModels;

namespace Zitie.Desktop.Views;

public partial class ModuleGalleryView : UserControl
{
    public ModuleGalleryView()
    {
        InitializeComponent();
        AddHandler(PointerWheelChangedEvent, OnCtrlPointerWheelChanged, RoutingStrategies.Tunnel);
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
