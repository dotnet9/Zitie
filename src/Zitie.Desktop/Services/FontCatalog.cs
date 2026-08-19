using Avalonia.Media;
using SkiaSharp;

namespace Zitie.Desktop.Services;

/// <summary>系统字体目录：供字帖画布选择字体，UI 自身仍使用内嵌霞鹜文楷。</summary>
public sealed class FontCatalog
{
    private const string DefaultChineseSample = "永";

    public FontCatalog()
    {
        var families = CollectFontFamilies();
        Fonts = families
            .Select(name => new FontOption(name, IsRecommendedChineseFont(name)))
            .OrderByDescending(option => option.IsRecommended)
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

        foreach (var fontFamily in FontManager.Current.SystemFonts)
            AddFontFamily(names, fontFamily.Name);

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
        var typeface = SKFontManager.Default.MatchCharacter(familyName, DefaultChineseSample[0]) ??
                       SKTypeface.FromFamilyName(familyName);
        return typeface is not null;
    }

    private static bool IsRecommendedChineseFont(string name)
    {
        string[] keywords =
        [
            "Kai",
            "楷",
            "Song",
            "宋",
            "FangSong",
            "仿宋",
            "Hei",
            "黑",
            "YaHei",
            "雅黑",
            "Sim",
            "Microsoft"
        ];

        return keywords.Any(keyword => name.Contains(keyword, StringComparison.CurrentCultureIgnoreCase));
    }

    private static string ResolveDefaultFontFamily(IReadOnlyList<FontOption> fonts)
    {
        string[] candidates =
        [
            "KaiTi",
            "楷体",
            "STKaiti",
            "SimSun",
            "宋体",
            "Microsoft YaHei",
            "Microsoft YaHei UI"
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

public sealed record FontOption(string Name, bool IsRecommended)
{
    public string DisplayName => IsRecommended ? $"{Name} · 推荐" : Name;
}
