namespace Zitie.Desktop.Services;

/// <summary>内置资源目录统一位于程序目录 resources/ 下。</summary>
internal static class ResourcePaths
{
    public static string Root { get; } = ResolveRoot();

    private static string ResolveRoot()
    {
        var direct = Path.Combine(AppContext.BaseDirectory, "resources");
        var bundled = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "Resources", "resources"));
        return Directory.Exists(direct) ? direct : Directory.Exists(bundled) ? bundled : direct;
    }

    public static string ModuleStyles { get; } = Path.Combine(Root, "module-styles");

    public static string ModuleStyleImages { get; } = Path.Combine(ModuleStyles, "images");

    public static string Modules { get; } = Path.Combine(Root, "modules");

    public static string Texts { get; } = Path.Combine(Root, "texts");

    public static string Pinyin { get; } = Path.Combine(Root, "pinyin");

    public static string Strokes { get; } = Path.Combine(Root, "strokes");

    public static string Textbooks { get; } = Path.Combine(Root, "textbooks");
}
