using System.Globalization;
using Avalonia.Media;
using SkiaSharp;
using Zitie.Core.Layout;
using Zitie.Core.Models;

namespace Zitie.Avalonia.Rendering;

internal sealed class FiveCharacterPoemCalligraphyRenderer : BracketPracticeLayoutRendererBase
{
    private const double ContentCenterXMm = 105;
    private const double GridLeftMm = 70;
    private const double GridTopMm = 124;
    private const double CellMm = 14;
    private const int Columns = 5;
    private const int Rows = 4;

    public override PracticeLayoutKind Kind => PracticeLayoutKind.FiveCharacterPoemCalligraphy;

    protected override void DrawTemplateHeader(
        SKCanvas canvas,
        CharacterSheetSpec spec,
        SheetPage page,
        SKColor color)
    {
    }

    protected override void Draw(
        SKCanvas canvas,
        CharacterSheetSpec spec,
        SheetPage page,
        SKPaint solidPaint,
        SKPaint dashPaint,
        SKPaint glyphPaint)
    {
        var green = ResolveGreen(spec, solidPaint.Color);
        var ink = ResolveInk(spec, glyphPaint.Color);
        DrawTitle(canvas, spec, PoemTitle(spec), ink);
        DrawAuthorLine(canvas, spec, PoemSubtitle(spec), ink);
        DrawPoemGrid(canvas, spec, page, green, ResolveTrace(spec));
    }

    private static void DrawTitle(
        SKCanvas canvas,
        CharacterSheetSpec spec,
        string title,
        SKColor ink)
    {
        DrawCenteredText(canvas, spec, SpaceTitle(title), 17.5f,
            new SKPoint(Mm(ContentCenterXMm), Mm(104.1)),
            ink);
    }

    private static void DrawAuthorLine(SKCanvas canvas, CharacterSheetSpec spec, string subtitle, SKColor ink)
    {
        DrawCenteredText(canvas, spec, subtitle, 8.6f,
            new SKPoint(Mm(ContentCenterXMm), Mm(116)),
            ink.WithAlpha(135));
    }

    private static void DrawPoemGrid(
        SKCanvas canvas,
        CharacterSheetSpec spec,
        SheetPage page,
        SKColor green,
        SKColor glyphColor)
    {
        using var grid = new SKPaint
        {
            Color = green.WithAlpha(95),
            StrokeWidth = Mm(0.45),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke
        };
        var width = Columns * CellMm;
        var height = Rows * CellMm;
        canvas.DrawRect(Mm(GridLeftMm), Mm(GridTopMm), Mm(width), Mm(height), grid);
        for (var column = 1; column < Columns; column++)
        {
            var x = GridLeftMm + column * CellMm;
            canvas.DrawLine(Mm(x), Mm(GridTopMm), Mm(x), Mm(GridTopMm + height), grid);
        }

        for (var row = 1; row < Rows; row++)
        {
            var y = GridTopMm + row * CellMm;
            canvas.DrawLine(Mm(GridLeftMm), Mm(y), Mm(GridLeftMm + width), Mm(y), grid);
        }

        var glyphs = page.Cells.Select(static cell => cell.Glyph).ToArray();
        for (var row = 0; row < Rows; row++)
        for (var column = 0; column < Columns; column++)
        {
            var index = row * Columns + column;
            if (index >= glyphs.Length || string.IsNullOrWhiteSpace(glyphs[index])) continue;
            DrawCenteredText(canvas, spec, glyphs[index], 20.8f,
                new SKPoint(Mm(GridLeftMm + column * CellMm + CellMm / 2), Mm(GridTopMm + row * CellMm + CellMm / 2)),
                glyphColor);
        }
    }

    private static string PoemTitle(CharacterSheetSpec spec)
    {
        if (string.IsNullOrWhiteSpace(spec.Title) ||
            string.Equals(spec.Title.Trim(), "五言古诗书法", StringComparison.Ordinal))
            return string.Empty;

        return spec.Title.Trim();
    }

    private static string PoemSubtitle(CharacterSheetSpec spec)
    {
        if (string.IsNullOrWhiteSpace(spec.Title))
            return string.Empty;

        var dynasty = string.IsNullOrWhiteSpace(spec.Dynasty) ? null : spec.Dynasty.Trim();
        var author = string.IsNullOrWhiteSpace(spec.Author) ? null : spec.Author.Trim();
        if (dynasty is null && author is null) return string.Empty;
        if (dynasty is null) return author!;
        if (author is null) return $"[{dynasty}]";
        return $"[{dynasty}]  {author}";
    }

    private static string SpaceTitle(string title)
    {
        var elements = TextElements(title).Take(4).ToArray();
        return elements.Length <= 1 ? title : string.Join("   ", elements);
    }

    private static IEnumerable<string> TextElements(string text)
    {
        var enumerator = StringInfo.GetTextElementEnumerator(text);
        while (enumerator.MoveNext())
        {
            var element = enumerator.GetTextElement();
            if (!string.IsNullOrWhiteSpace(element))
                yield return element;
        }
    }

    private static SKColor ResolveGreen(CharacterSheetSpec spec, SKColor fallback)
    {
        return !string.IsNullOrWhiteSpace(spec.GridColor) && Color.TryParse(spec.GridColor, out var color)
            ? color.ToSKColor()
            : fallback;
    }

    private static SKColor ResolveInk(CharacterSheetSpec spec, SKColor fallback)
    {
        return !string.IsNullOrWhiteSpace(spec.TextColor) && Color.TryParse(spec.TextColor, out var color)
            ? color.ToSKColor()
            : fallback;
    }

    private static SKColor ResolveTrace(CharacterSheetSpec spec)
    {
        return !string.IsNullOrWhiteSpace(spec.TraceColor) && Color.TryParse(spec.TraceColor, out var color)
            ? color.ToSKColor()
            : new SKColor(0xB9, 0xC5, 0xBE, 210);
    }
}
