using System.Windows.Input;
using Zitie.Core.Layout;
using Zitie.Core.Models;
using Zitie.Desktop.Services;

namespace Zitie.Desktop.ViewModels;

/// <summary>
///     模块卡片展示项：包一层命令，避免视图里做 $parent 绑定。
/// </summary>
public sealed class ModuleCardViewModel
{
    public ModuleCardViewModel(ModuleDefinition module, ICommand openCommand)
    {
        Module = module;
        OpenCommand = openCommand;
        Preview = ModulePreviewFactory.Create(module);
    }

    public ModuleDefinition Module { get; }

    public ICommand OpenCommand { get; }

    public CharacterSheetSpec PreviewSpec => Preview.Spec;

    public IReadOnlyList<SheetPage> PreviewPages => Preview.Pages;

    // 预览控件以 2 倍像素密度渲染，0.26 保持卡片内约 25% 的逻辑显示比例。
    public double PreviewZoom => 0.26;

    public string CategoryText => Preview.Category;

    public string StatusText => Module.Enabled ? "可用" : "即将上线";

    private ModulePreview Preview { get; }
}
