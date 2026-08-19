using Avalonia.Media;

namespace Zitie.Avalonia.Rendering;

/// <summary>
///     字帖渲染外观。默认值面向打印（黑白打印机友好），屏幕预览从主题资源覆盖。
/// </summary>
public sealed record SheetRenderTheme
{
    /// <summary>格子外框颜色。</summary>
    public Color GridSolidColor { get; init; } = Color.FromRgb(0xB0, 0x4A, 0x3F);

    /// <summary>格子辅助线（十字/对角线）颜色。</summary>
    public Color GridDashColor { get; init; } = Color.FromRgb(0xD2, 0x8C, 0x83);

    public float GridSolidStrokePt { get; init; } = 1.1f;

    public float GridDashStrokePt { get; init; } = 0.7f;

    /// <summary>范字颜色。</summary>
    public Color ModelGlyphColor { get; init; } = Colors.Black;

    /// <summary>描红字颜色（浅色轮廓）。</summary>
    public Color TraceGlyphColor { get; init; } = Color.FromRgb(0xDF, 0x9C, 0x93);

    /// <summary>页眉标题颜色。</summary>
    public Color TitleColor { get; init; } = Colors.Black;

    /// <summary>页眉填写栏颜色。</summary>
    public Color FieldColor { get; init; } = Color.FromRgb(0x40, 0x40, 0x40);

    /// <summary>页脚页码颜色。</summary>
    public Color FooterColor { get; init; } = Color.FromRgb(0x90, 0x90, 0x90);

    /// <summary>拼音标注颜色（打印建议深灰）。</summary>
    public Color PinyinColor { get; init; } = Color.FromRgb(0x50, 0x50, 0x50);

    public static SheetRenderTheme Print { get; } = new();
}
