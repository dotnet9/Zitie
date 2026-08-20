using Avalonia.Media;

namespace Zitie.Avalonia.Rendering;

/// <summary>
///     字帖渲染外观模型。具体取值由 Themes/Tokens 中的 XAML 资源创建，
///     本类型只提供渲染器需要的强类型契约。
/// </summary>
public sealed class SheetRenderTheme
{
    public Color GridSolidColor { get; set; }

    public Color GridDashColor { get; set; }

    public double GridSolidStrokePt { get; set; }

    public double GridDashStrokePt { get; set; }

    public Color ModelGlyphColor { get; set; }

    public Color TraceGlyphColor { get; set; }

    public Color TitleColor { get; set; }

    public Color FieldColor { get; set; }

    public Color FooterColor { get; set; }

    public Color PinyinColor { get; set; }

    public Color FrameColor { get; set; }

    public Color FrameInnerColor { get; set; }
}
