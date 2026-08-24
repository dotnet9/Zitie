using SkiaSharp;
using Zitie.Core.Layout;
using Zitie.Core.Models;

namespace Zitie.Avalonia.Rendering;

internal sealed class BracketWordColumnsRenderer : BracketPracticeLayoutRendererBase
{
    public override PracticeLayoutKind Kind => PracticeLayoutKind.BracketWordColumns;

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
        var wordWidth = Math.Min(11, itemWidth * 0.24);

        foreach (var cell in page.Cells)
        {
            var centerY = cell.YMm + cell.SizeMm / 2;
            DrawFittedText(canvas, spec, cell.Glyph, Mm(cell.SizeMm * 0.72),
                new SKPoint(Mm(cell.XMm + wordWidth / 2), Mm(centerY)),
                SKTextAlign.Center,
                glyphPaint.Color,
                Mm(wordWidth));
            DrawBracketBlank(canvas, spec,
                cell.XMm + wordWidth + 1.5,
                centerY,
                Math.Max(18, itemWidth - wordWidth - 5),
                cell.SizeMm,
                solidPaint,
                dashPaint,
                centerDash: false,
                verticalGuideCount: 3);
        }
    }
}
