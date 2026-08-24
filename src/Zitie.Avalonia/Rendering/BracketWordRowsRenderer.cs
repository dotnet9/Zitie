using SkiaSharp;
using Zitie.Core.Layout;
using Zitie.Core.Models;

namespace Zitie.Avalonia.Rendering;

internal sealed class BracketWordRowsRenderer : BracketPracticeLayoutRendererBase
{
    public override PracticeLayoutKind Kind => PracticeLayoutKind.BracketWordRows;

    protected override void Draw(
        SKCanvas canvas,
        CharacterSheetSpec spec,
        SheetPage page,
        SKPaint solidPaint,
        SKPaint dashPaint,
        SKPaint glyphPaint)
    {
        var availableWidth = spec.Page.UsableWidthMm;
        var leadingWidth = Math.Min(34, availableWidth * 0.22);
        var blankCount = 4;
        var blankWidth = Math.Max(18, (availableWidth - leadingWidth - 4) / blankCount);

        foreach (var cell in page.Cells)
        {
            var centerY = cell.YMm + cell.SizeMm / 2;
            DrawParenthesizedText(canvas, spec, cell.Glyph, cell.XMm + leadingWidth / 2, centerY,
                leadingWidth, cell.SizeMm, glyphPaint.Color, solidPaint.Color);

            for (var index = 0; index < blankCount; index++)
                DrawBracketBlank(canvas, spec,
                    cell.XMm + leadingWidth + index * blankWidth,
                    centerY,
                    blankWidth * 0.82,
                    cell.SizeMm,
                    solidPaint,
                    dashPaint,
                    centerDash: false);
        }
    }
}
