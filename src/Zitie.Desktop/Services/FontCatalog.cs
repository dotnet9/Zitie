using Avalonia.Media;
using SkiaSharp;
using Zitie.Avalonia.Rendering;
using Zitie.Desktop.Models;

namespace Zitie.Desktop.Services;

/// <summary>字帖字体目录：按规范楷体、书写体和印刷体分类，并保证内置字体可作为跨平台回退。</summary>
public sealed class FontCatalog
{
    private const string DefaultChineseSample = "永";

    public FontCatalog()
    {
        var families = CollectFontFamilies();
        Fonts = families
            .Select(Classify)
            .Append(new FontOption(
                ZitieFonts.WenKaiFamilyName,
                SheetFontCategory.Handwriting,
                FontStrokeStyle.Natural,
                true))
            .DistinctBy(static option => option.Name, StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(static option => option.RecommendationRank)
            .ThenBy(option => option.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        DefaultFontFamily = ResolveDefaultFontFamily(Fonts);
        ZitieLogging.Info($"系统字体加载完成：{Fonts.Count} 个，默认字帖字体：{DefaultFontFamily}");
    }

    public IReadOnlyList<FontOption> Fonts { get; }

    public string DefaultFontFamily { get; }

    public FontOption? Find(string? familyName)
    {
        return string.IsNullOrWhiteSpace(familyName)
            ? null
            : Fonts.FirstOrDefault(option => string.Equals(
                option.Name,
                familyName.Trim(),
                StringComparison.CurrentCultureIgnoreCase));
    }

    private static IReadOnlyList<string> CollectFontFamilies()
    {
        var names = new SortedSet<string>(StringComparer.CurrentCultureIgnoreCase);

        foreach (var name in SKFontManager.Default.GetFontFamilies())
            AddFontFamily(names, name);

        try
        {
            foreach (var fontFamily in FontManager.Current.SystemFonts)
                AddFontFamily(names, fontFamily.Name);
        }
        catch (InvalidOperationException)
        {
            // Avalonia platform services may not be initialized in background and test hosts.
        }

        return names.ToList();
    }

    private static void AddFontFamily(ISet<string> names, string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;

        var normalized = name.Trim();
        if (CanRenderSample(normalized)) names.Add(normalized);
    }

    private static bool CanRenderSample(string familyName)
    {
        using var typeface = SKTypeface.FromFamilyName(familyName);
        if (typeface is null) return false;

        using var font = new SKFont(typeface, 16);
        return font.ContainsGlyphs(DefaultChineseSample);
    }

    public static FontOption Classify(string name)
    {
        if (ContainsAny(name, "WenKai", "文楷", "XingKai", "行楷", "行书", "草书", "Handwriting"))
            return new FontOption(name, SheetFontCategory.Handwriting, FontStrokeStyle.Natural);

        if (ContainsAny(name, "KaiTi", "Kaiti", "楷体", "楷書", "楷书", "BiauKai", "DFKai", "FZKai", "方正楷"))
            return new FontOption(name, SheetFontCategory.StandardKai, FontStrokeStyle.Clear);

        if (ContainsAny(
                name,
                "Song", "宋", "FangSong", "仿宋", "Hei", "黑", "YaHei", "雅黑",
                "SimSun", "NSimSun", "SimHei", "Sans", "Serif", "Ming", "明朝", "明體", "明体"))
            return new FontOption(name, SheetFontCategory.Print, FontStrokeStyle.NotEmphasized);

        return FontOption.CreateUnclassified(name);
    }

    private static bool ContainsAny(string name, params string[] keywords)
    {
        return keywords.Any(keyword => name.Contains(keyword, StringComparison.CurrentCultureIgnoreCase));
    }

    private static string ResolveDefaultFontFamily(IReadOnlyList<FontOption> fonts)
    {
        string[] candidates =
        [
            "KaiTi",
            "楷体",
            "STKaiti",
            "Kaiti SC",
            "DFKai-SB",
            "BiauKai",
            ZitieFonts.WenKaiFamilyName
        ];

        foreach (var candidate in candidates)
            if (fonts.Any(option => string.Equals(
                    option.Name,
                    candidate,
                    StringComparison.CurrentCultureIgnoreCase)))
                return candidate;

        return fonts.FirstOrDefault()?.Name ?? string.Empty;
    }
}
