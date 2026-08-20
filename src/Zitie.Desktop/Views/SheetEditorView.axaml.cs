using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Zitie.Desktop.ViewModels;

namespace Zitie.Desktop.Views;

public partial class SheetEditorView : UserControl
{
    public SheetEditorView()
    {
        InitializeComponent();
        AddHandler(PointerWheelChangedEvent, OnCtrlPointerWheelChanged, RoutingStrategies.Tunnel);
    }

    private void OnCtrlPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) &&
            DataContext is SheetEditorViewModel viewModel)
        {
            viewModel.AdjustZoom(e.Delta.Y > 0 ? 0.1 : -0.1);
            e.Handled = true;
        }
    }
}
