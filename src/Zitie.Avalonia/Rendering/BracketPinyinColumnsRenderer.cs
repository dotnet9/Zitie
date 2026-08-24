using SkiaSharp;
using Zitie.Core.Layout;
using Zitie.Core.Models;

namespace Zitie.Avalonia.Rendering;

internal sealed class BracketPinyinColumnsRenderer : BracketPracticeLayoutRendererBase
{
    public override PracticeLayoutKind Kind => PracticeLayoutKind.BracketPinyinColumns;

    protected override void DrawTemplateHeader(
        SKCanvas canvas,
        CharacterSheetSpec spec,
        SheetPage page,
        SKColor color)
    {
        var pageWidth = Mm(spec.Page.WidthMm);
        var marginTop = spec.Page.MarginTopMm;
        var title = string.IsNullOrWhiteSpace(spec.Title) ? string.Empty : spec.Title.Trim();
        if (title.Length > 0)
            DrawCenteredText(canvas, spec, title, 17.5f,
                new SKPoint(pageWidth / 2, Mm(marginTop + 7.2)),
                color);

        if (!spec.ShowHeaderFields) return;

        var fieldY = page.Cells.Count > 0
            ? Math.Max(spec.Page.MarginTopMm + 18, page.Cells.Min(static cell => cell.YMm) - 8.2)
            : marginTop + 25;
        DrawFittedText(canvas, spec, "姓名:", 9.5f,
            new SKPoint(Mm(spec.Page.MarginLeftMm + 2), Mm(fieldY)),
            SKTextAlign.Left,
            color,
            Mm(36));
        DrawFittedText(canvas, spec, "评分： □优秀    □一般    □加油", 9.5f,
            new SKPoint(Mm(spec.Page.WidthMm - spec.Page.MarginRightMm - 3), Mm(fieldY)),
            SKTextAlign.Right,
            color,
            Mm(78));
    }

    protected override void DrawTemplateFooter(
        SKCanvas canvas,
        CharacterSheetSpec spec,
        SheetPage page,
        SKColor color)
    {
        DrawCenteredText(canvas, spec, $"- {page.Index + 1} -", 9f,
            new SKPoint(Mm(spec.Page.WidthMm / 2), Mm(spec.Page.HeightMm - spec.Page.MarginBottomMm / 2)),
            color);
    }

    protected override void Draw(
        SKCanvas canvas,
        CharacterSheetSpec spec,
        SheetPage page,
        SKPaint solidPaint,
        SKPaint dashPaint,
        SKPaint glyphPaint)
    {
        var itemWidth = spec.Page.UsableWidthMm / Math.Max(1, page.Columns);
        const double boxWidth = 10;
        const double boxHeight = 17;
        const double blankHeight = 6.5;

        foreach (var cell in page.Cells)
        {
            var boxX = cell.XMm + 2;
            var boxY = cell.YMm + 1;
            DrawPinyinWordBox(
                canvas,
                spec,
                cell.Glyph,
                boxX,
                boxY,
                boxWidth,
                boxHeight,
                solidPaint,
                glyphPaint,
                dashPaint);

            var pinyinHeight = boxHeight * PinyinWordBoxPinyinHeightRatio;
            var blankX = boxX + boxWidth + 1.5;
            var blankWidth = Math.Max(15, itemWidth - boxWidth - 5.5);
            DrawBracketBlank(canvas, spec, blankX, boxY + pinyinHeight * 0.55,
                blankWidth, blankHeight, solidPaint, dashPaint, centerDash: false);
            DrawBracketBlank(canvas, spec, blankX, boxY + pinyinHeight + (boxHeight - pinyinHeight) * 0.55,
                blankWidth, blankHeight, solidPaint, dashPaint, centerDash: false);
        }
    }
}
