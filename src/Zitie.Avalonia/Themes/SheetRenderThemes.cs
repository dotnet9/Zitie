using Avalonia.Controls;
using Zitie.Avalonia.Rendering;
using Zitie.Avalonia.Themes.Tokens;

namespace Zitie.Avalonia.Themes;

/// <summary>提供不依赖视觉树的渲染主题，例如 PDF/PNG 导出主题。</summary>
public static class SheetRenderThemes
{
    private static readonly Lazy<SheetRenderTheme> PrintTheme = new(LoadPrintTheme);

    public static SheetRenderTheme Print => PrintTheme.Value;

    private static SheetRenderTheme LoadPrintTheme()
    {
        ResourceDictionary dictionary = new ZitiePrintTokens();
        if (dictionary.TryGetValue("ZitiePrintRenderTheme", out var value) &&
            value is SheetRenderTheme theme)
            return theme;

        throw new InvalidOperationException(
            "打印主题缺少 ZitiePrintRenderTheme，请检查 Themes/Tokens/ZitiePrintTokens.axaml。");
    }
}
