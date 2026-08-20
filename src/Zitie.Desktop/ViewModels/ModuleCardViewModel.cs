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
    private double _previewZoom;

    public ModuleCardViewModel(ModuleDefinition module, ICommand openCommand)
    {
        Module = module;
        OpenCommand = openCommand;
        Preview = ModulePreviewFactory.Create(module);
        _previewZoom = 0.26;
    }

    public ModuleDefinition Module { get; }

    public ICommand OpenCommand { get; }

    public CharacterSheetSpec PreviewSpec => Preview.Spec;

    public IReadOnlyList<SheetPage> PreviewPages => Preview.Pages;

    public double PreviewZoom
    {
        get => _previewZoom;
        set => SetProperty(ref _previewZoom, Math.Clamp(value, 0.18, 0.48));
    }

    public string CategoryText => Preview.Category;

    public string StatusText => Module.Enabled ? "可用" : "即将上线";

    private ModulePreview Preview { get; }
}
