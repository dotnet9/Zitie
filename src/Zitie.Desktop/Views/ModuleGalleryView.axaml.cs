using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Zitie.Desktop.ViewModels;

namespace Zitie.Desktop.Views;

public partial class ModuleGalleryView : UserControl
{
    public ModuleGalleryView()
    {
        InitializeComponent();
    }

    private void CategoryButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ModuleGalleryViewModel viewModel ||
            sender is not ToggleButton { DataContext: CategoryChoiceViewModel choice }) return;

        viewModel.SelectedCategory = choice.Name;
    }
}
