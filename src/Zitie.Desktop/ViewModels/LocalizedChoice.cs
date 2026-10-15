namespace Zitie.Desktop.ViewModels;

/// <summary>保持选项身份不变，仅在全局语言变化时更新显示文本，防止切语言重置参数。</summary>
public sealed class LocalizedChoice(string key, bool literal = false) : LocalizedBindableBase
{
    public string Text => literal ? key : AppText.Get(key);
    public override string ToString() => Text;
}
