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

        DrawHeader(canvas, spec, page, theme);
        DrawCells(canvas, spec, page, theme);
        DrawFooter(canvas, spec, page, totalPages, theme);
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

            if (cell.Role == CellRole.Blank) continue;

            glyphPaint.Color = (cell.Role == CellRole.Model
                ? theme.ModelGlyphColor
                : theme.TraceGlyphColor).ToSKColor();
            DrawCenteredGlyph(canvas, cell.Glyph, size * 0.74f,
                new SKPoint(x + size / 2, y + size / 2), glyphPaint);
        }
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
        SKPaint paint)
    {
        using var font = new SKFont(ZitieFonts.WenKai, sizePt);
        var baselineY = center.Y - (font.Metrics.Descent - font.Metrics.Ascent) / 2 - font.Metrics.Ascent;
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
