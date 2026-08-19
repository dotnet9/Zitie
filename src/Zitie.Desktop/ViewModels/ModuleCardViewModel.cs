using System.Windows.Input;
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
    }

    public ModuleDefinition Module { get; }

    public ICommand OpenCommand { get; }

    public string StatusText => Module.Enabled ? "可用" : "即将上线";
}
