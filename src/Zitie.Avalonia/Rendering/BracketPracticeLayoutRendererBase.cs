using System.Globalization;
using Avalonia.Media;
using SkiaSharp;
using Zitie.Core.Layout;
using Zitie.Core.Models;

namespace Zitie.Avalonia.Rendering;

internal abstract class BracketPracticeLayoutRendererBase : IPracticeLayoutRenderer
{
    private static readonly float[] DashPattern = { 3f, 2.5f };
    protected const double PinyinWordBoxPinyinHeightRatio = 0.32;

    public abstract PracticeLayoutKind Kind { get; }

    public void Render(SKCanvas canvas, CharacterSheetSpec spec, SheetPage page, SheetRenderTheme theme)
    {
        using var solidPaint = new SKPaint
        {
            Color = ResolveColor(spec.GridColor, theme.GridSolidColor).ToSKColor(),
            StrokeWidth = (float)theme.GridSolidStrokePt,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke
        };
        using var dashPaint = new SKPaint
        {
            Color = GridDashColor(spec, theme).ToSKColor(),
            StrokeWidth = (float)theme.GridDashStrokePt,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            PathEffect = SKPathEffect.CreateDash(DashPattern, 0)
        };
        using var glyphPaint = new SKPaint
        {
            Color = ResolveColor(spec.TextColor, theme.ModelGlyphColor).ToSKColor(),
            IsAntialias = true
        };

        var chromeColor = ResolveChromeColor(solidPaint, glyphPaint);
        DrawTemplateChrome(canvas, spec, page, solidPaint, chromeColor);
        Draw(canvas, spec, page, solidPaint, dashPaint, glyphPaint);
        DrawTemplateFooter(canvas, spec, page, chromeColor);
    }

    protected abstract void Draw(
        SKCanvas canvas,
        CharacterSheetSpec spec,
        SheetPage page,
        SKPaint solidPaint,
        SKPaint dashPaint,
        SKPaint glyphPaint);

    protected virtual SKColor ResolveChromeColor(SKPaint solidPaint, SKPaint glyphPaint)
    {
        return solidPaint.Color;
    }

    protected virtual void DrawTemplateFooter(
        SKCanvas canvas,
        CharacterSheetSpec spec,
        SheetPage page,
        SKColor color)
    {
    }

    protected static void DrawParenthesizedText(
        SKCanvas canvas,
        CharacterSheetSpec spec,
        string text,
        double centerXMm,
        double centerYMm,
        double maxWidthMm,
        double heightMm,
        SKColor textColor,
        SKColor bracketColor)
    {
        var center = new SKPoint(Mm(centerXMm), Mm(centerYMm));
        var fontSize = Mm(heightMm * 0.68);
        if (string.IsNullOrWhiteSpace(text))
        {
            DrawCenteredText(canvas, spec, "(", fontSize,
                new SKPoint(Mm(centerXMm - maxWidthMm * 0.4), center.Y),
                bracketColor);
            DrawCenteredText(canvas, spec, ")", fontSize,
                new SKPoint(Mm(centerXMm + maxWidthMm * 0.4), center.Y),
                bracketColor);
            return;
        }

        var fittedSize = FitTextSize(spec, text, fontSize, Mm(maxWidthMm * 0.64));
        var textWidth = MeasureTextWidth(spec, text, fittedSize);
        var bracketGap = Mm(1.6);
        DrawCenteredText(canvas, spec, "(", fittedSize,
            new SKPoint(center.X - textWidth / 2 - bracketGap, center.Y),
            bracketColor);
        DrawFittedText(canvas, spec, text, fittedSize,
            center,
            SKTextAlign.Center,
            textColor,
            Mm(maxWidthMm * 0.64));
        DrawCenteredText(canvas, spec, ")", fittedSize,
            new SKPoint(center.X + textWidth / 2 + bracketGap, center.Y),
            bracketColor);
    }

    protected static void DrawBracketBlank(
        SKCanvas canvas,
        CharacterSheetSpec spec,
        double xMm,
        double centerYMm,
        double widthMm,
        double heightMm,
        SKPaint bracketPaint,
        SKPaint dashPaint,
        bool centerDash,
        int verticalGuideCount = 0)
    {
        var fontSize = Mm(Math.Min(8.5, Math.Max(5, heightMm * 0.82)));
        DrawCenteredText(canvas, spec, "(", fontSize,
            new SKPoint(Mm(xMm + 1.2), Mm(centerYMm)),
            bracketPaint.Color);
        DrawCenteredText(canvas, spec, ")", fontSize,
            new SKPoint(Mm(xMm + widthMm - 1.2), Mm(centerYMm)),
            bracketPaint.Color);

        if (verticalGuideCount > 0)
        {
            var guideTop = centerYMm - heightMm * 0.36;
            var guideBottom = centerYMm + heightMm * 0.36;
            var guideXs = ResolveVerticalGuideXs(xMm, widthMm, heightMm, verticalGuideCount);
            foreach (var guideX in guideXs)
            {
                canvas.DrawLine(
                    Mm(guideX),
                    Mm(guideTop),
                    Mm(guideX),
                    Mm(guideBottom),
                    dashPaint);
            }

            return;
        }

        if (!centerDash) return;

        canvas.DrawLine(
            Mm(xMm + widthMm * 0.34),
            Mm(centerYMm),
            Mm(xMm + widthMm * 0.66),
            Mm(centerYMm),
            dashPaint);
    }

    private static IEnumerable<double> ResolveVerticalGuideXs(
        double xMm,
        double widthMm,
        double heightMm,
        int verticalGuideCount)
    {
        if (verticalGuideCount == 3)
        {
            var edgeInset = Math.Min(widthMm * 0.22, Math.Clamp(heightMm * 0.28, 1.8, 3.2));
            yield return xMm + edgeInset;
            yield return xMm + widthMm / 2;
            yield return xMm + widthMm - edgeInset;
            yield break;
        }

        for (var index = 1; index <= verticalGuideCount; index++)
            yield return xMm + widthMm * index / (verticalGuideCount + 1);
    }

    protected static void DrawTwoByTwoWordGrid(
        SKCanvas canvas,
        CharacterSheetSpec spec,
        string word,
        double xMm,
        double yMm,
        double sizeMm,
        SKPaint paint,
        SKPaint glyphPaint)
    {
        var x = Mm(xMm);
        var y = Mm(yMm);
        var size = Mm(sizeMm);
        canvas.DrawRect(x, y, size, size, paint);
        canvas.DrawLine(x + size / 2, y, x + size / 2, y + size, paint);
        canvas.DrawLine(x, y + size / 2, x + size, y + size / 2, paint);

        var glyphs = TextElements(word).Take(2).ToArray();
        var fontSize = size * 0.34f;
        for (var index = 0; index < glyphs.Length; index++)
        {
            var center = new SKPoint(
                x + size * (index == 0 ? 0.25f : 0.75f),
                y + size * 0.25f);
            DrawCenteredGlyph(canvas, spec, glyphs[index], fontSize, center, glyphPaint);
        }
    }

    protected static void DrawPinyinWordBox(
        SKCanvas canvas,
        CharacterSheetSpec spec,
        string glyph,
        double xMm,
        double yMm,
        double widthMm,
        double heightMm,
        SKPaint paint,
        SKPaint glyphPaint,
        SKPaint? pinyinGuidePaint = null)
    {
        var x = Mm(xMm);
        var y = Mm(yMm);
        var width = Mm(widthMm);
        var height = Mm(heightMm);
        var pinyinHeight = height * (float)PinyinWordBoxPinyinHeightRatio;
        canvas.DrawRect(x, y, width, height, paint);
        canvas.DrawLine(x, y + pinyinHeight, x + width, y + pinyinHeight, paint);
        canvas.DrawLine(
            x,
            y + pinyinHeight / 3,
            x + width,
            y + pinyinHeight / 3,
            pinyinGuidePaint ?? paint);
        canvas.DrawLine(
            x,
            y + pinyinHeight * 2 / 3,
            x + width,
            y + pinyinHeight * 2 / 3,
            pinyinGuidePaint ?? paint);

        var firstGlyph = TextElements(glyph).FirstOrDefault() ?? string.Empty;
        if (firstGlyph.Length == 0) return;

        var pinyin = spec.PinyinByGlyph is not null &&
                     spec.PinyinByGlyph.TryGetValue(firstGlyph, out var value)
            ? value
            : string.Empty;
        if (pinyin.Length > 0)
            DrawPinyinText(canvas, pinyin, Math.Max(5.4f, pinyinHeight * 0.5f),
                new SKPoint(x + width / 2, y + pinyinHeight * 0.5f),
                glyphPaint.Color,
                width * 0.92f,
                pinyinHeight * 0.82f);

        DrawCenteredGlyph(canvas, spec, firstGlyph, height * 0.46f,
            new SKPoint(x + width / 2, y + pinyinHeight + (height - pinyinHeight) / 2),
            glyphPaint);
    }

    private static void DrawPinyinText(
        SKCanvas canvas,
        string text,
        float preferredSizePt,
        SKPoint center,
        SKColor color,
        float maxWidthPt,
        float clearHeightPt)
    {
        var fontSize = FitLatinTextSize(text, preferredSizePt, maxWidthPt);
        using var font = new SKFont(ResolvePinyinTypeface(), fontSize);
        font.Edging = SKFontEdging.SubpixelAntialias;
        font.Subpixel = true;
        var textWidth = font.MeasureText(text);
        using var backgroundPaint = new SKPaint
        {
            Color = SKColors.White,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        canvas.DrawRoundRect(
            new SKRect(
                center.X - textWidth / 2 - Mm(0.45),
                center.Y - clearHeightPt / 2,
                center.X + textWidth / 2 + Mm(0.45),
                center.Y + clearHeightPt / 2),
            Mm(0.35),
            Mm(0.35),
            backgroundPaint);
        using var paint = new SKPaint
        {
            Color = color,
            IsAntialias = true
        };
        var baselineY = center.Y - (font.Metrics.Descent - font.Metrics.Ascent) / 2 - font.Metrics.Ascent;
        canvas.DrawText(text, center.X, baselineY, SKTextAlign.Center, font, paint);
    }

    private static float FitLatinTextSize(string text, float preferredSizePt, float maxWidthPt)
    {
        using var font = new SKFont(ResolvePinyinTypeface(), preferredSizePt);
        var width = font.MeasureText(text);
        if (width <= maxWidthPt) return preferredSizePt;

        return Math.Max(5.2f, preferredSizePt * maxWidthPt / Math.Max(1, width));
    }

    protected static void DrawFittedText(
        SKCanvas canvas,
        CharacterSheetSpec spec,
        string text,
        float preferredSizePt,
        SKPoint point,
        SKTextAlign align,
        SKColor color,
        float maxWidthPt)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        var fontSize = FitTextSize(spec, text, preferredSizePt, maxWidthPt);
        using var font = CreateFont(spec, text, fontSize);
        using var paint = new SKPaint { Color = color, IsAntialias = true };
        var baselineY = point.Y - (font.Metrics.Descent - font.Metrics.Ascent) / 2 - font.Metrics.Ascent;
        canvas.DrawText(text, point.X, baselineY, align, font, paint);
    }

    protected static float Mm(double value)
    {
        return (float)(value * LayoutEngine.MmToPt);
    }

    private static void DrawCenteredGlyph(
        SKCanvas canvas,
        CharacterSheetSpec spec,
        string glyph,
        float sizePt,
        SKPoint center,
        SKPaint paint)
    {
        using var font = CreateFont(spec, glyph, sizePt);
        var baselineY = center.Y - (font.Metrics.Descent - font.Metrics.Ascent) / 2 - font.Metrics.Ascent;
        canvas.DrawText(glyph, center.X, baselineY, SKTextAlign.Center, font, paint);
    }

    protected static void DrawCenteredText(
        SKCanvas canvas,
        CharacterSheetSpec spec,
        string text,
        float sizePt,
        SKPoint center,
        SKColor color)
    {
        using var font = CreateFont(spec, text, sizePt);
        using var paint = new SKPaint { Color = color, IsAntialias = true };
        var baselineY = center.Y - (font.Metrics.Descent - font.Metrics.Ascent) / 2 - font.Metrics.Ascent;
        canvas.DrawText(text, center.X, baselineY, SKTextAlign.Center, font, paint);
    }

    protected static float FitTextSize(
        CharacterSheetSpec spec,
        string text,
        float preferredSizePt,
        float maxWidthPt)
    {
        if (string.IsNullOrEmpty(text) || maxWidthPt <= 0) return preferredSizePt;

        using var font = CreateFont(spec, text, preferredSizePt);
        var width = font.MeasureText(text);
        if (width <= maxWidthPt) return preferredSizePt;

        return Math.Max(5.5f, preferredSizePt * maxWidthPt / Math.Max(1, width));
    }

    private static float MeasureTextWidth(CharacterSheetSpec spec, string text, float sizePt)
    {
        using var font = CreateFont(spec, text, sizePt);
        return font.MeasureText(text);
    }

    private void DrawTemplateChrome(
        SKCanvas canvas,
        CharacterSheetSpec spec,
        SheetPage page,
        SKPaint layoutPaint,
        SKColor chromeColor)
    {
        if (page.HasHeader)
            DrawTemplateHeader(canvas, spec, page, chromeColor);

        if (!spec.FrameBorder || page.Cells.Count == 0) return;

        var left = Mm(spec.Page.MarginLeftMm);
        var topMm = Math.Max(spec.Page.MarginTopMm, page.Cells.Min(static cell => cell.YMm) - 5.5);
        var top = Mm(topMm);
        var right = Mm(spec.Page.WidthMm - spec.Page.MarginRightMm);
        var bottom = Mm(spec.Page.HeightMm - spec.Page.MarginBottomMm - 2);
        if (bottom <= top || right <= left) return;

        using var framePaint = layoutPaint.Clone();
        framePaint.Style = SKPaintStyle.Stroke;
        framePaint.Color = chromeColor;
        framePaint.StrokeWidth = Math.Max(framePaint.StrokeWidth, 1.2f);
        canvas.DrawRoundRect(
            new SKRect(left, top, right, bottom),
            Mm(1.4),
            Mm(1.4),
            framePaint);
    }

    protected virtual void DrawTemplateHeader(
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
        DrawFittedText(canvas, spec, "年      月      日", 9.5f,
            new SKPoint(Mm(spec.Page.WidthMm - spec.Page.MarginRightMm - 3), Mm(fieldY)),
            SKTextAlign.Right,
            color,
            Mm(44));
    }

    private static IEnumerable<string> TextElements(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) yield break;

        var enumerator = StringInfo.GetTextElementEnumerator(text);
        while (enumerator.MoveNext())
        {
            var element = enumerator.GetTextElement();
            if (!string.IsNullOrWhiteSpace(element))
                yield return element;
        }
    }

    private static Color ResolveColor(string? value, Color fallback)
    {
        return !string.IsNullOrWhiteSpace(value) && Color.TryParse(value, out var color)
            ? color
            : fallback;
    }

    private static Color GridDashColor(CharacterSheetSpec spec, SheetRenderTheme theme)
    {
        if (string.IsNullOrWhiteSpace(spec.GridColor) || !Color.TryParse(spec.GridColor, out var color))
            return theme.GridDashColor;

        return Color.FromArgb((byte)Math.Max(80, color.A * 0.55), color.R, color.G, color.B);
    }

    private static SKFont CreateFont(CharacterSheetSpec spec, string text, float sizePt)
    {
        return new SKFont(ResolveTypeface(spec, text), sizePt);
    }

    private static SKTypeface ResolveTypeface(CharacterSheetSpec spec, string text)
    {
        if (string.IsNullOrWhiteSpace(spec.FontFamilyName)) return ZitieFonts.WenKai;

        var family = spec.FontFamilyName.Trim();
        if (string.Equals(family, ZitieFonts.WenKaiFamilyName, StringComparison.OrdinalIgnoreCase))
            return ZitieFonts.WenKai;

        var character = text.FirstOrDefault(value => value > 127);
        return character == default
            ? SKTypeface.FromFamilyName(family) ?? ZitieFonts.WenKai
            : SKFontManager.Default.MatchCharacter(family, character) ??
              SKTypeface.FromFamilyName(family) ??
              ZitieFonts.WenKai;
    }

    private static SKTypeface ResolvePinyinTypeface()
    {
        return SKTypeface.FromFamilyName("Segoe UI") ??
               SKTypeface.FromFamilyName("Arial") ??
               ZitieFonts.WenKai;
    }
}
