using SkiaSharp;
using Zitie.Core.Layout;
using Zitie.Core.Models;

namespace Zitie.Avalonia.Rendering;

internal sealed class BracketGridWordsRenderer : BracketPracticeLayoutRendererBase
{
    public override PracticeLayoutKind Kind => PracticeLayoutKind.BracketGridWords;

    protected override SKColor ResolveChromeColor(SKPaint solidPaint, SKPaint glyphPaint)
    {
        return glyphPaint.Color;
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
        var gridSize = Math.Min(19, Math.Max(12, itemWidth - 6));
        using var boxPaint = solidPaint.Clone();
        boxPaint.Color = glyphPaint.Color;

        foreach (var cell in page.Cells)
        {
            var x = cell.XMm + (itemWidth - gridSize) / 2;
            var y = cell.YMm;
            DrawTwoByTwoWordGrid(canvas, spec, cell.Glyph, x, y, gridSize, boxPaint, glyphPaint);

            var wordY = y + gridSize + 5;
            DrawParenthesizedText(canvas, spec, cell.Glyph, x + gridSize / 2, wordY,
                gridSize + 3, 5.5, glyphPaint.Color, glyphPaint.Color);
            DrawBracketBlank(canvas, spec, x - 1, y + gridSize + 12.5,
                gridSize + 2, 5.5, solidPaint, dashPaint, centerDash: false, verticalGuideCount: 3);
            DrawBracketBlank(canvas, spec, x - 1, y + gridSize + 19.5,
                gridSize + 2, 5.5, solidPaint, dashPaint, centerDash: false, verticalGuideCount: 3);
        }
    }
}
