using System.Globalization;
using Avalonia.Media;
using SkiaSharp;
using Zitie.Core.Layout;
using Zitie.Core.Models;

namespace Zitie.Avalonia.Rendering;

internal sealed class FiveCharacterPoemCalligraphyRenderer : BracketPracticeLayoutRendererBase
{
    private const double CardCenterXMm = 105;
    private const double CardCenterYMm = 147;
    private const double CardRadiusXMm = 67;
    private const double CardRadiusYMm = 67;
    private const double GridLeftMm = 70;
    private const double GridTopMm = 124;
    private const double CellMm = 14;
    private const int Columns = 5;
    private const int Rows = 4;
    private const string ReferenceImageFileName = "五言古诗书法.jpg";

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
        var trace = ResolveTrace(spec);
        var usedReferenceBackground = DrawInkWashBackground(canvas, spec);
        if (!usedReferenceBackground)
            DrawDecorations(canvas, spec);
        DrawMedallion(canvas, green, usedReferenceBackground);
        DrawTitleBlock(canvas, spec, PoemTitle(spec), green, ink);
        DrawAuthorLine(canvas, spec, PoemSubtitle(spec), ink);
        DrawPoemGrid(canvas, spec, page, green, trace);
        if (!usedReferenceBackground)
            DrawNameLine(canvas, spec, green);
    }

    private static bool DrawInkWashBackground(SKCanvas canvas, CharacterSheetSpec spec)
    {
        if (DrawReferenceBackground(canvas, spec))
            return true;

        using var paper = new SKPaint
        {
            Color = Color.FromRgb(0xFA, 0xF8, 0xEF).ToSKColor(),
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        canvas.DrawRect(0, 0, Mm(spec.Page.WidthMm), Mm(spec.Page.HeightMm), paper);

        using var fleck = new SKPaint
        {
            Color = new SKColor(0xC8, 0xB8, 0x9C, 42),
            IsAntialias = true,
            StrokeWidth = Mm(0.12),
            Style = SKPaintStyle.Stroke
        };
        for (var i = 0; i < 90; i++)
        {
            var x = 5 + i * 37 % 198;
            var y = 4 + i * 53 % 286;
            canvas.DrawLine(Mm(x), Mm(y), Mm(x + 1.1), Mm(y + 0.35), fleck);
        }

        DrawMountains(canvas, spec);
        return false;
    }

    private static bool DrawReferenceBackground(SKCanvas canvas, CharacterSheetSpec spec)
    {
        var path = ResolveReferenceImagePath();
        if (!File.Exists(path)) return false;

        try
        {
            using var bitmap = SKBitmap.Decode(path);
            if (bitmap is null) return false;

            using var image = SKImage.FromBitmap(bitmap);
            canvas.DrawImage(
                image,
                new SKRect(0, 0, Mm(spec.Page.WidthMm), Mm(spec.Page.HeightMm)),
                new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string ResolveReferenceImagePath()
    {
        var outputPath = Path.Combine(
            AppContext.BaseDirectory,
            "resources",
            "module-styles",
            "images",
            ReferenceImageFileName);
        if (File.Exists(outputPath)) return outputPath;

        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            var repositoryPath = Path.Combine(
                directory.FullName,
                "docs",
                "modules",
                ReferenceImageFileName);
            if (File.Exists(repositoryPath)) return repositoryPath;
        }

        return outputPath;
    }

    private static void DrawMountains(SKCanvas canvas, CharacterSheetSpec spec)
    {
        using var far = new SKPaint
        {
            Color = new SKColor(0xB9, 0xDD, 0xD4, 120),
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        using var near = new SKPaint
        {
            Color = new SKColor(0x8E, 0xC8, 0xBB, 115),
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };

        using var farPath = new SKPath();
        farPath.MoveTo(0, Mm(spec.Page.HeightMm));
        farPath.LineTo(0, Mm(250));
        farPath.CubicTo(Mm(24), Mm(236), Mm(38), Mm(247), Mm(58), Mm(239));
        farPath.CubicTo(Mm(83), Mm(229), Mm(99), Mm(246), Mm(123), Mm(234));
        farPath.CubicTo(Mm(146), Mm(223), Mm(167), Mm(241), Mm(210), Mm(226));
        farPath.LineTo(Mm(210), Mm(spec.Page.HeightMm));
        farPath.Close();
        canvas.DrawPath(farPath, far);

        using var nearPath = new SKPath();
        nearPath.MoveTo(0, Mm(spec.Page.HeightMm));
        nearPath.LineTo(0, Mm(271));
        nearPath.CubicTo(Mm(27), Mm(260), Mm(40), Mm(268), Mm(63), Mm(258));
        nearPath.CubicTo(Mm(93), Mm(246), Mm(119), Mm(270), Mm(147), Mm(251));
        nearPath.CubicTo(Mm(170), Mm(236), Mm(187), Mm(253), Mm(210), Mm(245));
        nearPath.LineTo(Mm(210), Mm(spec.Page.HeightMm));
        nearPath.Close();
        canvas.DrawPath(nearPath, near);
    }

    private static void DrawDecorations(SKCanvas canvas, CharacterSheetSpec spec)
    {
        DrawWillow(canvas);
        DrawBird(canvas);
        DrawPavilion(canvas, spec);
    }

    private static void DrawWillow(SKCanvas canvas)
    {
        using var branchPaint = new SKPaint
        {
            Color = new SKColor(0x56, 0x54, 0x47, 165),
            StrokeWidth = Mm(1.0),
            StrokeCap = SKStrokeCap.Round,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true
        };
        using var branch = new SKPath();
        branch.MoveTo(Mm(42), Mm(34));
        branch.CubicTo(Mm(70), Mm(18), Mm(95), Mm(43), Mm(126), Mm(27));
        branch.CubicTo(Mm(145), Mm(17), Mm(166), Mm(28), Mm(190), Mm(16));
        canvas.DrawPath(branch, branchPaint);

        using var leafPaint = new SKPaint
        {
            Color = new SKColor(0x65, 0xBD, 0x7E, 120),
            StrokeWidth = Mm(0.42),
            StrokeCap = SKStrokeCap.Round,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true
        };
        for (var group = 0; group < 24; group++)
        {
            var startX = 58 + group * 5.3 % 116;
            var startY = 9 + group * 2.7 % 28;
            var length = 18 + group % 5 * 3.8;
            var bend = group % 2 == 0 ? 3.2 : -2.7;
            using var willow = new SKPath();
            willow.MoveTo(Mm(startX), Mm(startY));
            willow.CubicTo(Mm(startX + bend), Mm(startY + length * 0.35), Mm(startX - bend), Mm(startY + length * 0.65), Mm(startX + bend * 0.5), Mm(startY + length));
            canvas.DrawPath(willow, leafPaint);
        }
    }

    private static void DrawBird(SKCanvas canvas)
    {
        using var paint = new SKPaint
        {
            Color = new SKColor(0x20, 0x20, 0x35, 220),
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        using var bird = new SKPath();
        bird.MoveTo(Mm(21), Mm(30));
        bird.CubicTo(Mm(12), Mm(25), Mm(7), Mm(21), Mm(2), Mm(17));
        bird.CubicTo(Mm(15), Mm(18), Mm(23), Mm(20), Mm(31), Mm(25));
        bird.CubicTo(Mm(27), Mm(26), Mm(24), Mm(28), Mm(21), Mm(30));
        bird.CubicTo(Mm(25), Mm(31), Mm(30), Mm(33), Mm(37), Mm(38));
        bird.CubicTo(Mm(28), Mm(38), Mm(22), Mm(35), Mm(16), Mm(31));
        bird.Close();
        canvas.DrawPath(bird, paint);
    }

    private static void DrawPavilion(SKCanvas canvas, CharacterSheetSpec spec)
    {
        var baseX = spec.Page.WidthMm - 38;
        var baseY = spec.Page.HeightMm - 92;
        using var wash = new SKPaint
        {
            Color = new SKColor(0x8F, 0xC8, 0xA7, 80),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };
        canvas.DrawOval(new SKRect(Mm(baseX - 20), Mm(baseY - 12), Mm(baseX + 28), Mm(baseY + 48)), wash);

        using var line = new SKPaint
        {
            Color = new SKColor(0x43, 0x39, 0x2C, 185),
            StrokeWidth = Mm(0.9),
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true
        };
        using var roof = new SKPath();
        roof.MoveTo(Mm(baseX - 16), Mm(baseY + 13));
        roof.QuadTo(Mm(baseX), Mm(baseY - 7), Mm(baseX + 23), Mm(baseY + 10));
        roof.QuadTo(Mm(baseX + 12), Mm(baseY + 13), Mm(baseX - 16), Mm(baseY + 13));
        canvas.DrawPath(roof, line);
        canvas.DrawLine(Mm(baseX - 9), Mm(baseY + 16), Mm(baseX - 9), Mm(baseY + 42), line);
        canvas.DrawLine(Mm(baseX + 14), Mm(baseY + 15), Mm(baseX + 14), Mm(baseY + 42), line);
        canvas.DrawArc(new SKRect(Mm(baseX - 8), Mm(baseY + 22), Mm(baseX + 14), Mm(baseY + 54)), 180, 180, false, line);
    }

    private static void DrawMedallion(SKCanvas canvas, SKColor green, bool preserveReferenceOutline)
    {
        using var fill = new SKPaint
        {
            Color = new SKColor(0xFF, 0xFF, 0xFC),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        if (preserveReferenceOutline)
        {
            EraseReferenceExampleContent(canvas, fill);
            return;
        }

        using var stroke = new SKPaint
        {
            Color = green.WithAlpha(170),
            StrokeWidth = Mm(0.68),
            Style = SKPaintStyle.Stroke,
            IsAntialias = true
        };
        using var dash = stroke.Clone();
        dash.Color = green.WithAlpha(85);
        dash.StrokeWidth = Mm(0.36);
        dash.PathEffect = SKPathEffect.CreateDash([Mm(0.9), Mm(1.3)], 0);

        using var outer = CreateMedallionPath(CardRadiusXMm, CardRadiusYMm);
        canvas.DrawPath(outer, fill);
        canvas.DrawPath(outer, stroke);

        using var inner = CreateMedallionPath(CardRadiusXMm - 3.3, CardRadiusYMm - 3.3);
        canvas.DrawPath(inner, dash);
    }

    private static void EraseReferenceExampleContent(SKCanvas canvas, SKPaint fill)
    {
        canvas.DrawRect(Mm(72), Mm(96), Mm(66), Mm(18), fill);
        canvas.DrawRect(Mm(84), Mm(112), Mm(42), Mm(10), fill);
        canvas.DrawRect(Mm(68), Mm(123), Mm(74), Mm(59), fill);
    }

    private static SKPath CreateMedallionPath(double radiusX, double radiusY)
    {
        var path = new SKPath();
        const int steps = 176;
        for (var i = 0; i <= steps; i++)
        {
            var angle = Math.PI * 2 * i / steps;
            var scallop = 1 + 0.055 * Math.Sin(angle * 8) + 0.018 * Math.Sin(angle * 17);
            var x = CardCenterXMm + Math.Cos(angle) * radiusX * scallop;
            var y = CardCenterYMm + Math.Sin(angle) * radiusY * scallop;
            if (i == 0)
                path.MoveTo(Mm(x), Mm(y));
            else
                path.LineTo(Mm(x), Mm(y));
        }

        path.Close();
        return path;
    }

    private static void DrawTitleBlock(
        SKCanvas canvas,
        CharacterSheetSpec spec,
        string title,
        SKColor green,
        SKColor ink)
    {
        const double y = 104;
        const double width = 62;
        const double height = 14;
        var left = CardCenterXMm - width / 2;
        var right = CardCenterXMm + width / 2;

        using var linePaint = new SKPaint
        {
            Color = green.WithAlpha(190),
            StrokeWidth = Mm(0.9),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke
        };
        using var fill = new SKPaint
        {
            Color = green.WithAlpha(160),
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        canvas.DrawLine(Mm(left), Mm(y - height / 2), Mm(right), Mm(y - height / 2), linePaint);
        canvas.DrawLine(Mm(left), Mm(y + height / 2), Mm(right), Mm(y + height / 2), linePaint);
        canvas.DrawRect(Mm(left), Mm(y - height / 2), Mm(4.2), Mm(height), fill);
        canvas.DrawRect(Mm(right - 4.2), Mm(y - height / 2), Mm(4.2), Mm(height), fill);
        canvas.DrawLine(Mm(left + 6.2), Mm(y - height / 2), Mm(left + 6.2), Mm(y + height / 2), linePaint);
        canvas.DrawLine(Mm(right - 6.2), Mm(y - height / 2), Mm(right - 6.2), Mm(y + height / 2), linePaint);

        DrawCenteredText(canvas, spec, SpaceTitle(title), 17.5f,
            new SKPoint(Mm(CardCenterXMm), Mm(y + 0.1)),
            ink);
    }

    private static void DrawAuthorLine(SKCanvas canvas, CharacterSheetSpec spec, string subtitle, SKColor ink)
    {
        DrawCenteredText(canvas, spec, subtitle, 8.6f,
            new SKPoint(Mm(CardCenterXMm), Mm(116)),
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

    private static void DrawNameLine(SKCanvas canvas, CharacterSheetSpec spec, SKColor green)
    {
        using var paint = new SKPaint
        {
            Color = green.WithAlpha(170),
            StrokeWidth = Mm(0.55),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke
        };
        DrawFittedText(canvas, spec, "姓名：", 8.6f,
            new SKPoint(Mm(82), Mm(194.5)),
            SKTextAlign.Left,
            green.WithAlpha(210),
            Mm(22));
        canvas.DrawLine(Mm(99), Mm(195.2), Mm(128), Mm(195.2), paint);
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
