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
public sealed class FontOption(
    string name,
    SheetFontCategory category,
    FontStrokeStyle strokeStyle,
    bool isBundled = false) : LocalizedBindableBase
{
    public string Name { get; } = name;
    public SheetFontCategory Category { get; } = category;
    public FontStrokeStyle StrokeStyle { get; } = strokeStyle;
    public bool IsBundled { get; } = isBundled;

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
        SheetFontCategory.StandardKai => AppText.Get(Language.StandardKai),
        SheetFontCategory.Handwriting => AppText.Get(Language.Handwriting),
        SheetFontCategory.Print => AppText.Get(Language.PrintFont),
        _ => AppText.Get(Language.OtherFonts)
    };

    public string StrokeStyleName => StrokeStyle switch
    {
        FontStrokeStyle.Clear => AppText.Get(Language.ClearStrokes),
        FontStrokeStyle.Natural => AppText.Get(Language.NaturalStrokes),
        FontStrokeStyle.NotEmphasized => AppText.Get(Language.UnemphasizedStrokes),
        _ => AppText.Get(Language.UnassessedStrokes)
    };

    public string DisplayName => $"{Name} · {CategoryName}";

    public string Description => $"{CategoryName} · {StrokeStyleName}" + (IsBundled ? AppText.Get(Language.BundledFontSuffix) : string.Empty);

    public string PreviewFamilyName => IsBundled
        ? "avares://Zitie.Avalonia/Fonts#LXGW WenKai"
        : Name;

    public static FontOption CreateUnclassified(string name)
    {
        return new FontOption(name, SheetFontCategory.Other, FontStrokeStyle.Unknown);
    }

    public override string ToString() => DisplayName;
}
