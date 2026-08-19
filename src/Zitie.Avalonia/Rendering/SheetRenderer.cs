using Avalonia.Media;
using SkiaSharp;
using Zitie.Core.Layout;
using Zitie.Core.Models;

namespace Zitie.Avalonia.Rendering;

/// <summary>
///     唯一的页面渲染实现：屏幕预览、PDF 与 PNG 导出共用，保证所见即所得。
///     画布单位为 PDF 点，毫米坐标由 <see cref="LayoutEngine.MmToPt" /> 换算。
/// </summary>
public static class SheetRenderer
{
    private static readonly float[] DashPattern = { 3f, 2.5f };

    public static void RenderPage(
        SKCanvas canvas,
        CharacterSheetSpec spec,
        SheetPage page,
        int totalPages,
        SheetRenderTheme theme)
    {
        canvas.Clear(SKColors.White);

        if (spec.FrameBorder) DrawFrame(canvas, spec, theme);
        DrawHeader(canvas, spec, page, theme);
        DrawCells(canvas, spec, page, theme);
        DrawFooter(canvas, spec, page, totalPages, theme);
    }

    /// <summary>解析描红字颜色：规格指定时优先，否则回退到主题默认色。</summary>
    private static Color TraceGlyphColor(CharacterSheetSpec spec, SheetRenderTheme theme)
    {
        return spec.TraceColor is { } hex
            && Color.TryParse(hex, out var color)
                ? color
                : theme.TraceGlyphColor;
    }

    /// <summary>页面装饰边框：距边缘双线框。</summary>
    private static void DrawFrame(SKCanvas canvas, CharacterSheetSpec spec, SheetRenderTheme theme)
    {
        var widthPt = (float)(spec.Page.WidthMm * LayoutEngine.MmToPt);
        var heightPt = (float)(spec.Page.HeightMm * LayoutEngine.MmToPt);
        var inset = (float)(6 * LayoutEngine.MmToPt);

        using var outerPaint = new SKPaint
        {
            Color = theme.FrameColor.ToSKColor(),
            StrokeWidth = 1.4f,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke
        };
        using var innerPaint = new SKPaint
        {
            Color = theme.FrameInnerColor.ToSKColor(),
            StrokeWidth = 0.8f,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke
        };

        canvas.DrawRect(inset, inset, widthPt - inset * 2, heightPt - inset * 2, outerPaint);
        canvas.DrawRect(inset + 5, inset + 5, widthPt - (inset + 5) * 2, heightPt - (inset + 5) * 2, innerPaint);
    }

    private static void DrawHeader(SKCanvas canvas, CharacterSheetSpec spec, SheetPage page, SheetRenderTheme theme)
    {
        if (!page.HasHeader) return;

        var pageWidthPt = (float)(spec.Page.WidthMm * LayoutEngine.MmToPt);
        var topPt = (float)(spec.Page.MarginTopMm * LayoutEngine.MmToPt);

        var lineY = topPt;
        if (spec.Title is not null)
        {
            var titleHeightPt = (float)(12 * LayoutEngine.MmToPt);
            DrawCenteredText(canvas, spec.Title, 16f,
                new SKPoint(pageWidthPt / 2, lineY + titleHeightPt / 2), theme.TitleColor.ToSKColor());
            lineY += titleHeightPt;
        }

        if (spec.ShowPoemHeader && !string.IsNullOrWhiteSpace(spec.Author))
        {
            var authorHeightPt = (float)(9 * LayoutEngine.MmToPt);
            var authorLine = spec.Dynasty is { Length: > 0 }
                ? $"{spec.Dynasty} · {spec.Author}"
                : spec.Author;
            DrawCenteredText(canvas, authorLine, 11f,
                new SKPoint(pageWidthPt / 2, lineY + authorHeightPt / 2), theme.TitleColor.ToSKColor());
            lineY += authorHeightPt;
        }

        if (spec.ShowHeaderFields)
        {
            var fieldsHeightPt = (float)(10 * LayoutEngine.MmToPt);
            DrawLeftText(canvas, "班级：____________　　姓名：____________　　日期：____________", 10.5f,
                new SKPoint((float)(spec.Page.MarginLeftMm * LayoutEngine.MmToPt),
                    lineY + fieldsHeightPt / 2), theme.FieldColor.ToSKColor());
        }
    }

    private static void DrawCells(SKCanvas canvas, CharacterSheetSpec spec, SheetPage page, SheetRenderTheme theme)
    {
        using var solidPaint = new SKPaint
        {
            Color = theme.GridSolidColor.ToSKColor(),
            StrokeWidth = theme.GridSolidStrokePt,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke
        };
        using var dashPaint = new SKPaint
        {
            Color = theme.GridDashColor.ToSKColor(),
            StrokeWidth = theme.GridDashStrokePt,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            PathEffect = SKPathEffect.CreateDash(DashPattern, 0)
        };
        using var glyphPaint = new SKPaint { IsAntialias = true };

        foreach (var cell in page.Cells)
        {
            var x = (float)(cell.XMm * LayoutEngine.MmToPt);
            var y = (float)(cell.YMm * LayoutEngine.MmToPt);
            var size = (float)(cell.SizeMm * LayoutEngine.MmToPt);

            DrawGrid(canvas, spec.Grid, x, y, size, solidPaint, dashPaint);

            if (cell.Role == CellRole.Blank)
            {
                // 看拼音写词语：空格 + 顶部拼音
                if (spec.ShowPinyin)
                    DrawPinyin(canvas, cell, spec, x, y, size, theme);
                continue;
            }

            // 范字（或浅色描红字）居中；拼音四线格的音节用较小字号避免溢出
            glyphPaint.Color = (cell.Role == CellRole.Model
                ? theme.ModelGlyphColor
                : TraceGlyphColor(spec, theme)).ToSKColor();
            var fontSize = spec.Grid == GridKind.Pinyin ? size * 0.5f : size * 0.74f;
            DrawCenteredGlyph(canvas, cell.Glyph, fontSize,
                new SKPoint(x + size / 2, y + size / 2), glyphPaint,
                hollow: spec.HollowGlyph && cell.Role == CellRole.Model);

            if (spec.ShowPinyin)
                DrawPinyin(canvas, cell, spec, x, y, size, theme);
        }
    }

    private static void DrawPinyin(
        SKCanvas canvas,
        CellSlot cell,
        CharacterSheetSpec spec,
        float x,
        float y,
        float size,
        SheetRenderTheme theme)
    {
        if (spec.PinyinByGlyph is null || !spec.PinyinByGlyph.TryGetValue(cell.Glyph, out var pinyin)) return;

        var centerX = x + size / 2;
        var topY = y + size * 0.13f;
        var fontSize = Math.Max(6f, size * 0.16f);
        // 多音节拼音（如“chūn tiān”）拆开等宽排布在格子宽度内
        var syllables = pinyin.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var spacing = size * 0.92f / Math.Max(1, syllables.Length);
        for (var i = 0; i < syllables.Length; i++)
            DrawCenteredText(canvas, syllables[i], fontSize,
                new SKPoint(centerX - spacing * (syllables.Length - 1) / 2f + spacing * i, topY),
                theme.PinyinColor.ToSKColor());
    }

    private static void DrawGrid(
        SKCanvas canvas,
        GridKind kind,
        float x,
        float y,
        float size,
        SKPaint solidPaint,
        SKPaint dashPaint)
    {
        canvas.DrawRect(x, y, size, size, solidPaint);

        switch (kind)
        {
            case GridKind.Tian:
                DrawCross(canvas, x, y, size, dashPaint);
                break;
            case GridKind.Mi:
                DrawCross(canvas, x, y, size, dashPaint);
                canvas.DrawLine(x, y, x + size, y + size, dashPaint);
                canvas.DrawLine(x + size, y, x, y + size, dashPaint);
                break;
            case GridKind.HuiGong:
            {
                var inset = size * 0.2f;
                canvas.DrawRect(x + inset, y + inset, size - inset * 2, size - inset * 2, dashPaint);
                break;
            }
            case GridKind.English:
            {
                // 英文四线三格：自上而下 上中线 / 基线 / 下中线，字母主体落在基线与上中线之间
                canvas.DrawLine(x, y + size * 0.34f, x + size, y + size * 0.34f, dashPaint);
                canvas.DrawLine(x, y + size * 0.66f, x + size, y + size * 0.66f, solidPaint);
                canvas.DrawLine(x, y + size * 0.86f, x + size, y + size * 0.86f, dashPaint);
                break;
            }
            case GridKind.Nine:
            {
                // 九宫格：外框 + 井字虚线（3×3）
                var third = size / 3f;
                canvas.DrawLine(x + third, y, x + third, y + size, dashPaint);
                canvas.DrawLine(x + third * 2, y, x + third * 2, y + size, dashPaint);
                canvas.DrawLine(x, y + third, x + size, y + third, dashPaint);
                canvas.DrawLine(x, y + third * 2, x + size, y + third * 2, dashPaint);
                break;
            }
            case GridKind.Pinyin:
            {
                // 拼音四线格：与英文四线三格同构，音节主体落在基线与上中线之间，声调在顶
                canvas.DrawLine(x, y + size * 0.34f, x + size, y + size * 0.34f, dashPaint);
                canvas.DrawLine(x, y + size * 0.66f, x + size, y + size * 0.66f, solidPaint);
                canvas.DrawLine(x, y + size * 0.86f, x + size, y + size * 0.86f, dashPaint);
                break;
            }
            case GridKind.Plain:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }
    }

    private static void DrawCross(SKCanvas canvas, float x, float y, float size, SKPaint paint)
    {
        canvas.DrawLine(x + size / 2, y, x + size / 2, y + size, paint);
        canvas.DrawLine(x, y + size / 2, x + size, y + size / 2, paint);
    }

    private static void DrawFooter(
        SKCanvas canvas,
        CharacterSheetSpec spec,
        SheetPage page,
        int totalPages,
        SheetRenderTheme theme)
    {
        var pageWidthPt = (float)(spec.Page.WidthMm * LayoutEngine.MmToPt);
        var pageHeightPt = (float)(spec.Page.HeightMm * LayoutEngine.MmToPt);
        var footerCenterY = pageHeightPt - (float)(5 * LayoutEngine.MmToPt);

        DrawCenteredText(canvas, $"第 {page.Index + 1} 页 / 共 {totalPages} 页", 9f,
            new SKPoint(pageWidthPt / 2, footerCenterY), theme.FooterColor.ToSKColor());
    }

    private static void DrawCenteredGlyph(
        SKCanvas canvas,
        string glyph,
        float sizePt,
        SKPoint center,
        SKPaint paint,
        bool hollow = false)
    {
        using var font = new SKFont(ZitieFonts.WenKai, sizePt);
        var baselineY = center.Y - (font.Metrics.Descent - font.Metrics.Ascent) / 2 - font.Metrics.Ascent;

        if (hollow)
        {
            // 双钩填墨：沿字形轮廓描边，中间留空
            using var hollowPaint = new SKPaint
            {
                Color = paint.Color,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = Math.Max(0.9f, sizePt * 0.018f),
                StrokeJoin = SKStrokeJoin.Round,
                StrokeCap = SKStrokeCap.Round
            };
            canvas.DrawText(glyph, center.X, baselineY, SKTextAlign.Center, font, hollowPaint);
            return;
        }

        canvas.DrawText(glyph, center.X, baselineY, SKTextAlign.Center, font, paint);
    }

    private static void DrawCenteredText(SKCanvas canvas, string text, float sizePt, SKPoint center, SKColor color)
    {
        using var font = new SKFont(ZitieFonts.WenKai, sizePt);
        using var paint = new SKPaint { Color = color, IsAntialias = true };
        var baselineY = center.Y - (font.Metrics.Descent - font.Metrics.Ascent) / 2 - font.Metrics.Ascent;
        canvas.DrawText(text, center.X, baselineY, SKTextAlign.Center, font, paint);
    }

    private static void DrawLeftText(SKCanvas canvas, string text, float sizePt, SKPoint center, SKColor color)
    {
        using var font = new SKFont(ZitieFonts.WenKai, sizePt);
        using var paint = new SKPaint { Color = color, IsAntialias = true };
        var baselineY = center.Y - (font.Metrics.Descent - font.Metrics.Ascent) / 2 - font.Metrics.Ascent;
        canvas.DrawText(text, center.X, baselineY, SKTextAlign.Left, font, paint);
    }
}
