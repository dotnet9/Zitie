using System.Windows.Input;
using Zitie.Core.Layout;
using Zitie.Core.Models;
using Zitie.Desktop.Services;
using Prism.Mvvm;

namespace Zitie.Desktop.ViewModels;

/// <summary>
///     模块卡片展示项：包一层命令，避免视图里做 $parent 绑定。
/// </summary>
public sealed class ModuleCardViewModel : BindableBase
{
    private const double BaseCardWidth = 260;
    private const double BaseCardHeight = 320;
    private const double BaseThumbHeight = 190;
    private const double BasePreviewZoom = 0.26;

    private double _cardZoom;

    public ModuleCardViewModel(ModuleDefinition module, ICommand openCommand)
    {
        Module = module;
        OpenCommand = openCommand;
        Preview = ModulePreviewFactory.Create(module);
        _cardZoom = 1;
    }

    public ModuleDefinition Module { get; }

    public ICommand OpenCommand { get; }

    public CharacterSheetSpec PreviewSpec => Preview.Spec;

    public IReadOnlyList<SheetPage> PreviewPages => Preview.Pages;

    public double CardZoom
    {
        get => _cardZoom;
        set
        {
            var zoom = Math.Clamp(value, ModuleGalleryViewModel.MinimumGalleryZoom, ModuleGalleryViewModel.MaximumGalleryZoom);
            if (!SetProperty(ref _cardZoom, zoom)) return;

            RaisePropertyChanged(nameof(CardWidth));
            RaisePropertyChanged(nameof(CardHeight));
            RaisePropertyChanged(nameof(ThumbHeight));
            RaisePropertyChanged(nameof(PreviewZoom));
        }
    }

    public double CardWidth => Math.Round(BaseCardWidth * CardZoom);

    public double CardHeight => Math.Round(BaseCardHeight * CardZoom);

    public double ThumbHeight => Math.Round(BaseThumbHeight * CardZoom);

    public double PreviewZoom => Math.Round(BasePreviewZoom * CardZoom, 3, MidpointRounding.AwayFromZero);

    public string CategoryText => Preview.Category;

    public string StatusText => Module.Enabled ? "可用" : "即将上线";

    private ModulePreview Preview { get; }
}
