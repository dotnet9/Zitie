namespace Zitie.Desktop.Models;

public enum SheetFontCategory
{
    StandardKai,
    Handwriting,
    Print,
    Other
}

public enum FontStrokeStyle
{
    Clear,
    Natural,
    NotEmphasized,
    Unknown
}

/// <summary>字帖字体及其书写适用性；笔锋描述来自字体类别，不对字形做人工特效。</summary>
public sealed record FontOption(
    string Name,
    SheetFontCategory Category,
    FontStrokeStyle StrokeStyle,
    bool IsBundled = false)
{
    public bool IsRecommended => Category == SheetFontCategory.StandardKai;

    public int RecommendationRank => Category switch
    {
        SheetFontCategory.StandardKai => 0,
        SheetFontCategory.Handwriting => 1,
        SheetFontCategory.Print => 2,
        _ => 3
    };

    public string CategoryName => Category switch
    {
        SheetFontCategory.StandardKai => "规范楷体",
        SheetFontCategory.Handwriting => "书写体",
        SheetFontCategory.Print => "印刷体",
        _ => "其他字体"
    };

    public string StrokeStyleName => StrokeStyle switch
    {
        FontStrokeStyle.Clear => "笔锋清晰",
        FontStrokeStyle.Natural => "自然笔意",
        FontStrokeStyle.NotEmphasized => "不强调笔锋",
        _ => "笔锋未评估"
    };

    public string DisplayName => $"{Name} · {CategoryName}";

    public string Description => $"{CategoryName} · {StrokeStyleName}" + (IsBundled ? " · 内置字体" : string.Empty);

    public string PreviewFamilyName => IsBundled
        ? "avares://Zitie.Avalonia/Fonts#LXGW WenKai"
        : Name;

    public static FontOption CreateUnclassified(string name)
    {
        return new FontOption(name, SheetFontCategory.Other, FontStrokeStyle.Unknown);
    }
}
