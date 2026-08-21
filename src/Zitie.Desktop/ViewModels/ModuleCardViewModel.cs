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
    private const double BaseCardHeight = 282;
    private const double BaseThumbHeight = 158;
    private const double BasePreviewZoom = 0.22;

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

    public string StatusText => Module.Enabled
        ? $"{(IsBuiltIn ? "内置" : "自定义")} · {GridText}"
        : "即将上线";

    private bool IsBuiltIn
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Module.SourcePath)) return true;

            var sourcePath = Path.GetFullPath(Module.SourcePath);
            var builtInDirectory = Path.GetFullPath(ResourcePaths.Modules)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return sourcePath.StartsWith(builtInDirectory, StringComparison.OrdinalIgnoreCase);
        }
    }

    private string GridText => PreviewSpec.Grid switch
    {
        GridKind.Tian => "田字格",
        GridKind.HuiGong => "回宫格",
        GridKind.Nine => "九宫格",
        GridKind.Pinyin => "拼音四线格",
        GridKind.English => "四线三格",
        GridKind.Plain => "方格",
        _ => "米字格"
    };

    private ModulePreview Preview { get; }
}
