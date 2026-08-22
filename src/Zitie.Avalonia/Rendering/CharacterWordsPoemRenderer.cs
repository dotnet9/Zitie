using System.Globalization;
using Avalonia.Media;
using SkiaSharp;
using Zitie.Core.Layout;
using Zitie.Core.Models;

namespace Zitie.Avalonia.Rendering;

internal sealed class CharacterWordsPoemRenderer : BracketPracticeLayoutRendererBase
{
    private const double LeftMm = 13.5;
    private const double RightMm = 196.5;
    private const double TopMm = 13.5;
    private const double CharacterToWordGapMm = 9.5;
    private const double WordToPoemGapMm = 8.4;
    private const double CharacterRowHeightMm = 24.2;
    private const double CharacterHeaderHeightMm = 9.1;
    private const double WordRowHeightMm = 10.1;
    private const double PoemHeaderLineOffsetMm = 8.7;
    private const double PoemBodyTopOffsetMm = 24.0;
    private const int CharacterRows = 5;
    private const int CharacterGridColumns = 12;
    private const int WordGridColumns = 18;
    private const int WordGridRows = 4;

    private static readonly IReadOnlyDictionary<string, CharacterEntry> Defaults =
        new Dictionary<string, CharacterEntry>
        {
            ["春"] = new("chūn", "日部", "上下", ["一", "一", "三", "丿", "夫", "夫", "春", "春", "春"], ["春天", "春风"]),
            ["冬"] = new("dōng", "夂部", "上下", ["丿", "㇇", "久", "久", "冬", "冬"], ["冬天", "秋冬"]),
            ["风"] = new("fēng", "风部", "半包围", ["丿", "㇈", "风", "风", "风"], ["大风", "刮风"]),
            ["雪"] = new("xuě", "雨部", "上下", ["一", "丶", "乛", "丨", "丶", "丶", "雪", "雪"], ["下雪", "雪花"]),
            ["花"] = new("huā", "艹部", "上下", ["一", "丨", "丨", "丿", "花", "花"], ["花生", "开花"])
        };

    public override PracticeLayoutKind Kind => PracticeLayoutKind.CharacterWordsPoem;

    protected override void DrawTemplateHeader(
        SKCanvas canvas,
        CharacterSheetSpec spec,
        SheetPage page,
        SKColor color)
    {
    }

    protected override void DrawTemplateFooter(
        SKCanvas canvas,
        CharacterSheetSpec spec,
        SheetPage page,
        SKColor color)
    {
        DrawCenteredText(canvas, spec, $"第 {page.Index + 1} 页", 8.2f,
            new SKPoint(Mm(spec.Page.WidthMm / 2), Mm(spec.Page.HeightMm - 7.2)),
            Color.FromRgb(0x8A, 0x8A, 0x8A).ToSKColor());
    }

    protected override void Draw(
        SKCanvas canvas,
        CharacterSheetSpec spec,
        SheetPage page,
        SKPaint solidPaint,
        SKPaint dashPaint,
        SKPaint glyphPaint)
    {
        using var layoutPaint = solidPaint.Clone();
        layoutPaint.StrokeWidth = Math.Max(0.9f, layoutPaint.StrokeWidth);
        using var thinPaint = solidPaint.Clone();
        thinPaint.StrokeWidth = Math.Max(0.7f, thinPaint.StrokeWidth * 0.85f);
        using var tracePaint = glyphPaint.Clone();
        tracePaint.Color = ResolveTraceColor(spec);
        var strokeCurrentColor = ResolveStrokeCurrentColor();

        var content = CharacterWordsPoemContent.Parse(page.Cells.Select(static cell => cell.Glyph), spec);
        var y = TopMm;
        DrawSectionTitle(canvas, spec, "同步生字", LeftMm, y, layoutPaint.Color);
        y += 7.0;
        DrawCharacterSection(canvas, spec, content, y, layoutPaint, dashPaint, glyphPaint, tracePaint, strokeCurrentColor);

        y += CharacterRows * CharacterRowHeightMm + CharacterToWordGapMm;
        DrawSectionTitle(canvas, spec, "组词训练", LeftMm, y, layoutPaint.Color);
        y += 7.2;
        DrawWordSection(canvas, spec, content.Words, y, thinPaint, glyphPaint);

        y += WordGridRows * WordRowHeightMm + WordToPoemGapMm;
        DrawSectionTitle(canvas, spec, "同步古诗", LeftMm, y, layoutPaint.Color);
        DrawFittedText(canvas, spec, content.PoemTitle, 14.8f,
            new SKPoint(Mm(RightMm - 2), Mm(y + 3.9)),
            SKTextAlign.Right,
            glyphPaint.Color,
            Mm(46));
        canvas.DrawLine(Mm(LeftMm), Mm(y + PoemHeaderLineOffsetMm), Mm(RightMm), Mm(y + PoemHeaderLineOffsetMm), thinPaint);
        y += PoemBodyTopOffsetMm;
        DrawPoemSection(canvas, spec, content.PoemLines, y, thinPaint, glyphPaint);
    }

    private static void DrawCharacterSection(
        SKCanvas canvas,
        CharacterSheetSpec spec,
        CharacterWordsPoemContent content,
        double topMm,
        SKPaint solidPaint,
        SKPaint dashPaint,
        SKPaint glyphPaint,
        SKPaint tracePaint,
        SKColor strokeCurrentColor)
    {
        var cellWidth = (RightMm - LeftMm) / CharacterGridColumns;
        for (var row = 0; row < CharacterRows; row++)
        {
            var entry = content.Characters[row];
            var y = topMm + row * CharacterRowHeightMm;
            var gridTop = y + CharacterHeaderHeightMm;
            canvas.DrawRect(Mm(LeftMm), Mm(y), Mm(RightMm - LeftMm), Mm(CharacterRowHeightMm), solidPaint);
            canvas.DrawLine(Mm(LeftMm), Mm(gridTop), Mm(RightMm), Mm(gridTop), solidPaint);

            for (var column = 1; column < CharacterGridColumns; column++)
            {
                var x = LeftMm + column * cellWidth;
                canvas.DrawLine(Mm(x), Mm(gridTop), Mm(x), Mm(y + CharacterRowHeightMm), solidPaint);
            }

            for (var column = 0; column < CharacterGridColumns; column++)
                DrawTianGuides(canvas, LeftMm + column * cellWidth, gridTop, cellWidth, CharacterRowHeightMm - CharacterHeaderHeightMm, dashPaint);

            DrawFittedText(canvas, spec, entry.Pinyin, 10.6f,
                new SKPoint(Mm(LeftMm + cellWidth * 0.5), Mm(y + CharacterHeaderHeightMm * 0.48)),
                SKTextAlign.Center,
                glyphPaint.Color,
                Mm(cellWidth * 0.82));
            DrawFittedText(canvas, spec, entry.Radical, 9.4f,
                new SKPoint(Mm(LeftMm + cellWidth * 1.55), Mm(y + CharacterHeaderHeightMm * 0.48)),
                SKTextAlign.Center,
                solidPaint.Color,
                Mm(cellWidth * 1.05));
            DrawFittedText(canvas, spec, entry.Structure, 9.4f,
                new SKPoint(Mm(LeftMm + cellWidth * 2.45), Mm(y + CharacterHeaderHeightMm * 0.48)),
                SKTextAlign.Center,
                solidPaint.Color,
                Mm(cellWidth));
            var strokeOrderX = LeftMm + cellWidth * 3.3;
            var strokeOrderY = y + CharacterHeaderHeightMm * 0.56;
            var strokeOrderWidth = cellWidth * 4.1;
            if (entry.StrokeOrder is { Strokes.Count: > 0 } strokeOrder)
                DrawStrokeOrder(
                    canvas,
                    strokeOrder.Strokes,
                    strokeOrderX,
                    strokeOrderY,
                    strokeOrderWidth,
                    glyphPaint.Color,
                    strokeCurrentColor);
            else
                DrawStrokeHints(
                    canvas,
                    spec,
                    entry.StrokeHints,
                    strokeOrderX,
                    strokeOrderY,
                    strokeOrderWidth,
                    glyphPaint.Color,
                    strokeCurrentColor);
            DrawFittedText(canvas, spec, string.Join("  ", entry.Words), 10.2f,
                new SKPoint(Mm(RightMm - 2.2), Mm(y + CharacterHeaderHeightMm * 0.48)),
                SKTextAlign.Right,
                glyphPaint.Color,
                Mm(cellWidth * 3.2));

            var charY = gridTop + (CharacterRowHeightMm - CharacterHeaderHeightMm) / 2;
            DrawCenteredText(canvas, spec, entry.Glyph, 35f,
                new SKPoint(Mm(LeftMm + cellWidth * 0.5), Mm(charY)),
                glyphPaint.Color);
            DrawCenteredText(canvas, spec, entry.Glyph, 34f,
                new SKPoint(Mm(LeftMm + cellWidth * 1.5), Mm(charY)),
                tracePaint.Color);
            DrawCenteredText(canvas, spec, entry.Glyph, 33f,
                new SKPoint(Mm(LeftMm + cellWidth * 5.5), Mm(charY)),
                tracePaint.Color);
            DrawCenteredText(canvas, spec, entry.Glyph, 33f,
                new SKPoint(Mm(LeftMm + cellWidth * 9.5), Mm(charY)),
                tracePaint.Color);
        }
    }

    private static void DrawWordSection(
        SKCanvas canvas,
        CharacterSheetSpec spec,
        IReadOnlyList<string> words,
        double topMm,
        SKPaint paint,
        SKPaint glyphPaint)
    {
        var width = RightMm - LeftMm;
        var cellWidth = width / WordGridColumns;
        const double rowHeight = WordRowHeightMm;
        canvas.DrawRect(Mm(LeftMm), Mm(topMm), Mm(width), Mm(rowHeight * WordGridRows), paint);
        for (var column = 1; column < WordGridColumns; column++)
        {
            var x = LeftMm + column * cellWidth;
            canvas.DrawLine(Mm(x), Mm(topMm), Mm(x), Mm(topMm + rowHeight * WordGridRows), paint);
        }

        for (var row = 1; row < WordGridRows; row++)
        {
            var y = topMm + row * rowHeight;
            canvas.DrawLine(Mm(LeftMm), Mm(y), Mm(RightMm), Mm(y), paint);
        }

        DrawWordRow(canvas, spec, words.Take(6).ToArray(), topMm + rowHeight * 0.52, cellWidth, glyphPaint.Color);
        DrawWordRow(canvas, spec, words.Skip(6).Take(6).ToArray(), topMm + rowHeight * 2.52, cellWidth, glyphPaint.Color);
    }

    private static void DrawPoemSection(
        SKCanvas canvas,
        CharacterSheetSpec spec,
        IReadOnlyList<string> lines,
        double topMm,
        SKPaint linePaint,
        SKPaint glyphPaint)
    {
        const double linePitch = 15.4;
        for (var lineIndex = 0; lineIndex < 4; lineIndex++)
        {
            var y = topMm + lineIndex * linePitch;
            canvas.DrawLine(Mm(LeftMm), Mm(y), Mm(RightMm), Mm(y), linePaint);
        }

        for (var index = 0; index < Math.Min(lines.Count, 2); index++)
        {
            var writingLineIndex = index * 2;
            var writingBottomY = topMm + writingLineIndex * linePitch;
            DrawFittedText(canvas, spec, lines[index], 27.2f,
                new SKPoint(Mm(LeftMm + 1), Mm(writingBottomY - 5.2)),
                SKTextAlign.Left,
                glyphPaint.Color,
                Mm(RightMm - LeftMm - 2));
        }
    }

    private static void DrawSectionTitle(
        SKCanvas canvas,
        CharacterSheetSpec spec,
        string title,
        double xMm,
        double yMm,
        SKColor color)
    {
        using var fill = new SKPaint { Color = color, IsAntialias = true, Style = SKPaintStyle.Fill };
        var x = Mm(xMm);
        var y = Mm(yMm);
        DrawGraduationCap(canvas, xMm, yMm, fill);
        DrawFittedText(canvas, spec, title, 16.6f,
            new SKPoint(Mm(xMm + 7.8), Mm(yMm + 4.1)),
            SKTextAlign.Left,
            color,
            Mm(46));
    }

    private static void DrawTianGuides(
        SKCanvas canvas,
        double xMm,
        double yMm,
        double widthMm,
        double heightMm,
        SKPaint dashPaint)
    {
        canvas.DrawLine(
            Mm(xMm + widthMm / 2),
            Mm(yMm),
            Mm(xMm + widthMm / 2),
            Mm(yMm + heightMm),
            dashPaint);
        canvas.DrawLine(
            Mm(xMm),
            Mm(yMm + heightMm / 2),
            Mm(xMm + widthMm),
            Mm(yMm + heightMm / 2),
            dashPaint);
    }

    private static void DrawStrokeOrder(
        SKCanvas canvas,
        IReadOnlyList<string> strokePaths,
        double xMm,
        double centerYMm,
        double maxWidthMm,
        SKColor completedColor,
        SKColor currentColor)
    {
        var parsedPaths = strokePaths
            .Select(ParseStrokePath)
            .Where(static path => path is not null)
            .Cast<SKPath>()
            .ToArray();
        if (parsedPaths.Length == 0) return;

        try
        {
            using var completedPaint = new SKPaint
            {
                Color = completedColor,
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };
            using var currentPaint = new SKPaint
            {
                Color = currentColor,
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };

            var step = maxWidthMm / parsedPaths.Length;
            var boxMm = Math.Clamp(Math.Min(step * 0.95, CharacterHeaderHeightMm * 0.72), 1.8, 5.2);
            for (var diagramIndex = 0; diagramIndex < parsedPaths.Length; diagramIndex++)
            {
                var centerXMm = xMm + step * (diagramIndex + 0.5);
                for (var strokeIndex = 0; strokeIndex <= diagramIndex; strokeIndex++)
                    DrawStrokePath(
                        canvas,
                        parsedPaths[strokeIndex],
                        centerXMm - boxMm / 2,
                        centerYMm - boxMm / 2,
                        boxMm,
                        strokeIndex == diagramIndex ? currentPaint : completedPaint);
            }
        }
        finally
        {
            foreach (var path in parsedPaths)
                path.Dispose();
        }
    }

    private static SKPath? ParseStrokePath(string pathData)
    {
        try
        {
            return SKPath.ParseSvgPathData(pathData);
        }
        catch
        {
            return null;
        }
    }

    private static void DrawStrokePath(
        SKCanvas canvas,
        SKPath path,
        double xMm,
        double yMm,
        double sizeMm,
        SKPaint paint)
    {
        var scale = Mm(sizeMm) / 1024f;
        var matrix = new SKMatrix
        {
            ScaleX = scale,
            SkewX = 0,
            TransX = Mm(xMm),
            SkewY = 0,
            ScaleY = -scale,
            TransY = Mm(yMm) + 900 * scale,
            Persp0 = 0,
            Persp1 = 0,
            Persp2 = 1
        };

        using var transformed = new SKPath(path);
        transformed.Transform(matrix);
        canvas.DrawPath(transformed, paint);
    }

    private static void DrawStrokeHints(
        SKCanvas canvas,
        CharacterSheetSpec spec,
        IReadOnlyList<string> hints,
        double xMm,
        double centerYMm,
        double maxWidthMm,
        SKColor color,
        SKColor accentColor)
    {
        if (hints.Count == 0) return;

        var step = Math.Min(5.7, maxWidthMm / Math.Max(1, hints.Count));
        var start = xMm + step / 2;
        for (var index = 0; index < hints.Count; index++)
            DrawCenteredText(canvas, spec, hints[index], 15.6f,
                new SKPoint(Mm(start + index * step), Mm(centerYMm)),
                index == 0 ? accentColor : color);
    }

    private static void DrawWordRow(
        SKCanvas canvas,
        CharacterSheetSpec spec,
        IReadOnlyList<string> words,
        double centerYMm,
        double cellWidthMm,
        SKColor color)
    {
        for (var index = 0; index < words.Count; index++)
        {
            var glyphs = TextElements(words[index]).Take(3).ToArray();
            var startColumn = index * 3;
            for (var glyphIndex = 0; glyphIndex < glyphs.Length; glyphIndex++)
            {
                var x = LeftMm + (startColumn + glyphIndex + 0.5) * cellWidthMm;
                DrawFittedText(canvas, spec, glyphs[glyphIndex], 22.8f,
                    new SKPoint(Mm(x), Mm(centerYMm)),
                    SKTextAlign.Center,
                    color,
                    Mm(cellWidthMm * 0.88));
            }
        }
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

    private static void DrawGraduationCap(SKCanvas canvas, double xMm, double yMm, SKPaint fill)
    {
        var x = Mm(xMm);
        var y = Mm(yMm);
        using var cap = new SKPath();
        cap.MoveTo(x, y + Mm(2.2));
        cap.LineTo(x + Mm(3.4), y);
        cap.LineTo(x + Mm(6.8), y + Mm(2.2));
        cap.LineTo(x + Mm(3.4), y + Mm(4.4));
        cap.Close();
        canvas.DrawPath(cap, fill);

        using var body = fill.Clone();
        body.Style = SKPaintStyle.Stroke;
        body.StrokeWidth = Mm(0.65);
        body.StrokeJoin = SKStrokeJoin.Round;
        body.StrokeCap = SKStrokeCap.Round;
        using var bodyPath = new SKPath();
        bodyPath.MoveTo(x + Mm(1.25), y + Mm(3.45));
        bodyPath.LineTo(x + Mm(1.25), y + Mm(6.05));
        bodyPath.QuadTo(x + Mm(3.4), y + Mm(7.1), x + Mm(5.55), y + Mm(6.05));
        bodyPath.LineTo(x + Mm(5.55), y + Mm(3.45));
        canvas.DrawPath(bodyPath, body);
    }

    private static SKColor ResolveTraceColor(CharacterSheetSpec spec)
    {
        return !string.IsNullOrWhiteSpace(spec.TraceColor) && Color.TryParse(spec.TraceColor, out var color)
            ? color.ToSKColor()
            : Color.FromRgb(0xFE, 0xD9, 0xD3).ToSKColor();
    }

    private static SKColor ResolveStrokeCurrentColor()
    {
        return Color.FromRgb(0xE8, 0x7C, 0x70).ToSKColor();
    }

    private sealed record CharacterEntry(
        string Glyph,
        string Pinyin,
        string Radical,
        string Structure,
        IReadOnlyList<string> StrokeHints,
        IReadOnlyList<string> Words,
        CharacterStrokeOrder? StrokeOrder)
    {
        public CharacterEntry(
            string pinyin,
            string radical,
            string structure,
            IReadOnlyList<string> strokeHints,
            IReadOnlyList<string> words)
            : this(string.Empty, pinyin, radical, structure, strokeHints, words, null)
        {
        }

        public CharacterEntry WithGlyph(string glyph)
        {
            return this with { Glyph = glyph };
        }

        public CharacterEntry WithStrokeOrder(CharacterStrokeOrder? strokeOrder)
        {
            return this with { StrokeOrder = strokeOrder };
        }
    }

    private sealed record CharacterWordsPoemContent(
        IReadOnlyList<CharacterEntry> Characters,
        IReadOnlyList<string> Words,
        string PoemTitle,
        IReadOnlyList<string> PoemLines)
    {
        private static readonly string[] DefaultGlyphs = ["春", "冬", "风", "雪", "花"];
        private static readonly string[] DefaultWords = ["春天", "春风", "冬天", "秋冬", "大风", "刮风", "下雪", "雪花", "开花", "花生"];
        private static readonly string[] DefaultPoemLines =
        [
            "远上寒山石径斜，白云生处有人家。",
            "停车坐爱枫林晚，霜叶红于二月花。"
        ];

        public static CharacterWordsPoemContent Parse(IEnumerable<string> rawTokens, CharacterSheetSpec spec)
        {
            var tokens = rawTokens
                .Where(static token => !string.IsNullOrWhiteSpace(token))
                .Select(static token => token.Trim())
                .ToArray();
            var glyphs = ResolveGlyphs(tokens);
            var remaining = tokens.SkipWhile(token => glyphs.Contains(token)).ToArray();
            var words = remaining
                .Where(static token => !LooksLikePoemLine(token) && TextElementCount(token) is >= 2 and <= 4)
                .Take(12)
                .ToArray();
            if (words.Length == 0)
                words = glyphs.SelectMany(glyph => Defaults.TryGetValue(glyph, out var entry) ? entry.Words : Array.Empty<string>()).ToArray();
            if (words.Length == 0)
                words = DefaultWords;

            var poemLines = remaining
                .Where(LooksLikePoemLine)
                .SelectMany(SplitPoemLine)
                .Take(4)
                .ToArray();
            if (poemLines.Length == 0)
                poemLines = DefaultPoemLines;

            var entries = glyphs
                .Select(glyph => CreateEntry(glyph, spec, words))
                .ToArray();
            return new CharacterWordsPoemContent(entries, words, ResolvePoemTitle(spec, poemLines), poemLines);
        }

        private static string ResolvePoemTitle(CharacterSheetSpec spec, IReadOnlyList<string> poemLines)
        {
            var title = string.IsNullOrWhiteSpace(spec.Title) ||
                        string.Equals(spec.Title.Trim(), "生字组词古诗", StringComparison.Ordinal)
                ? null
                : spec.Title.Trim();
            if (title is null && poemLines.SequenceEqual(DefaultPoemLines))
                title = "山行";
            title ??= "同步古诗";

            var dynasty = string.IsNullOrWhiteSpace(spec.Dynasty) ? null : spec.Dynasty.Trim();
            var author = string.IsNullOrWhiteSpace(spec.Author) ? null : spec.Author.Trim();
            if (dynasty is null && author is null && poemLines.SequenceEqual(DefaultPoemLines))
                return $"{title} [唐] 杜牧";
            if (dynasty is null && author is null)
                return title;
            if (dynasty is null)
                return $"{title} {author}";
            if (author is null)
                return $"{title} [{dynasty}]";
            return $"{title} [{dynasty}] {author}";
        }

        private static CharacterEntry CreateEntry(string glyph, CharacterSheetSpec spec, IReadOnlyList<string> words)
        {
            var strokeOrder = spec.StrokeOrderByGlyph is not null &&
                              spec.StrokeOrderByGlyph.TryGetValue(glyph, out var order)
                ? order
                : null;

            if (Defaults.TryGetValue(glyph, out var entry))
                return entry.WithGlyph(glyph).WithStrokeOrder(strokeOrder);

            var glyphWords = words.Where(word => word.Contains(glyph, StringComparison.Ordinal)).Take(2).ToArray();
            if (glyphWords.Length == 0)
                glyphWords = [string.Empty, string.Empty];

            var pinyin = spec.PinyinByGlyph is not null &&
                         spec.PinyinByGlyph.TryGetValue(glyph, out var value)
                ? value
                : string.Empty;
            return new CharacterEntry(glyph, pinyin, "部首", "结构", [], glyphWords, strokeOrder);
        }

        private static string[] ResolveGlyphs(IReadOnlyList<string> tokens)
        {
            var singles = tokens
                .Where(static token => TextElementCount(token) == 1 && !LooksLikePoemLine(token))
                .Take(CharacterRows)
                .ToArray();
            if (singles.Length == CharacterRows) return singles;

            var source = string.Concat(tokens);
            var glyphs = TextElements(source)
                .Where(static value => value.Length > 0 && IsChinese(value[0]))
                .Distinct()
                .Take(CharacterRows)
                .ToArray();
            return glyphs.Length == CharacterRows
                ? glyphs
                : DefaultGlyphs;
        }

        private static IEnumerable<string> SplitPoemLine(string text)
        {
            var chunks = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var chunk in chunks)
                yield return chunk;
        }

        private static bool LooksLikePoemLine(string text)
        {
            return text.IndexOfAny(['，', '。', '；', '！', '？', ',', '.', ';', '!', '?']) >= 0 ||
                   TextElementCount(text) > 8;
        }

        private static int TextElementCount(string text)
        {
            return TextElements(text).Count();
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

        private static bool IsChinese(char value)
        {
            return value is >= '\u3400' and <= '\u9FFF';
        }
    }
}
