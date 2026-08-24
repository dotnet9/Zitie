using SkiaSharp;
using Xunit;
using Zitie.Avalonia.Export;
using Zitie.Core.Layout;
using Zitie.Core.Models;

namespace Zitie.Core.Tests;

public sealed class SheetExporterTests
{
    [Fact]
    public void ExportPng_WritesConfiguredBackgroundAndExpectedSize()
    {
        var directory = CreateTempDirectory();
        try
        {
            var spec = CreateSpec() with
            {
                Background = SheetBackground.RicePaper,
                BackgroundColor = "#FBF8EF"
            };
            var pages = LayoutEngine.Paginate(spec);
            var path = Path.Combine(directory, "sheet.png");

            SheetExporter.ExportPng(path, spec, pages[0], pages.Count, dpi: 72);

            using var bitmap = SKBitmap.Decode(path);
            Assert.NotNull(bitmap);
            Assert.Equal((int)Math.Ceiling(spec.Page.WidthMm / 25.4 * 72), bitmap.Width);
            Assert.Equal((int)Math.Ceiling(spec.Page.HeightMm / 25.4 * 72), bitmap.Height);
            var pixel = bitmap.GetPixel(0, 0);
            Assert.Equal(0xFB, pixel.Red);
            Assert.Equal(0xF8, pixel.Green);
            Assert.Equal(0xEF, pixel.Blue);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void ExportPng_RejectsInvalidDpi()
    {
        var spec = CreateSpec();
        var page = LayoutEngine.Paginate(spec)[0];

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SheetExporter.ExportPng("unused.png", spec, page, 1, dpi: 0));
    }

    [Fact]
    public void ExportPng_WritesSvgBackgroundArtwork()
    {
        var directory = CreateTempDirectory();
        try
        {
            var spec = CreateSpec() with
            {
                BackgroundArtworkSvg = """
                    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 210 297">
                      <rect width="210" height="297" fill="#D6F1E6" />
                    </svg>
                    """
            };
            var pages = LayoutEngine.Paginate(spec);
            var path = Path.Combine(directory, "sheet-artwork.png");

            SheetExporter.ExportPng(path, spec, pages[0], pages.Count, dpi: 72);

            using var bitmap = SKBitmap.Decode(path);
            Assert.NotNull(bitmap);
            var pixel = bitmap.GetPixel(1, 1);
            Assert.Equal(0xD6, pixel.Red);
            Assert.Equal(0xF1, pixel.Green);
            Assert.Equal(0xE6, pixel.Blue);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void ExportPng_PracticeLayoutDrawsVisibleBlankSlots()
    {
        var directory = CreateTempDirectory();
        try
        {
            var spec = CreateSpec() with
            {
                Text = "花",
                PracticeLayout = PracticeLayoutKind.BracketWordColumns,
                Grid = GridKind.None,
                GridColor = "#29A86C"
            };
            var pages = LayoutEngine.Paginate(spec);
            var path = Path.Combine(directory, "bracket-layout.png");

            SheetExporter.ExportPng(path, spec, pages[0], pages.Count, dpi: 96);

            using var bitmap = SKBitmap.Decode(path);
            Assert.NotNull(bitmap);
            var inkPixels = 0;
            for (var y = (int)(bitmap.Height * 0.55); y < (int)(bitmap.Height * 0.75); y++)
            for (var x = (int)(bitmap.Width * 0.08); x < (int)(bitmap.Width * 0.92); x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                if (pixel.Red < 245 || pixel.Green < 245 || pixel.Blue < 245)
                    inkPixels++;
            }

            Assert.True(inkPixels > 80);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void ExportPng_BracketPracticeLayoutUsesTemplateChromeColor()
    {
        var directory = CreateTempDirectory();
        try
        {
            var spec = CreateSpec() with
            {
                Text = "神州 中华 山川",
                PracticeLayout = PracticeLayoutKind.BracketWordRows,
                Grid = GridKind.None,
                GridColor = "#00A968",
                TextColor = "#111111",
                Title = "组词括号练习",
                ShowHeaderFields = true,
                FrameBorder = true
            };
            var pages = LayoutEngine.Paginate(spec);
            var path = Path.Combine(directory, "bracket-word-rows.png");

            SheetExporter.ExportPng(path, spec, pages[0], pages.Count, dpi: 96);

            using var bitmap = SKBitmap.Decode(path);
            Assert.NotNull(bitmap);
            Assert.True(CountPixels(bitmap, 0.34, 0.08, 0.66, 0.17, IsTemplateGreen) > 30);
            Assert.True(CountPixels(bitmap, 0.06, 0.18, 0.30, 0.35, IsTemplateGreen) > 20);
            Assert.True(CountPixels(bitmap, 0.08, 0.18, 0.26, 0.35, IsDarkInk) > 20);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void ExportPng_BracketWordColumnsDrawsVerticalWordGuides()
    {
        const double dpi = 96;
        var directory = CreateTempDirectory();
        try
        {
            var spec = CreateSpec() with
            {
                Text = "花",
                PracticeLayout = PracticeLayoutKind.BracketWordColumns,
                Grid = GridKind.None,
                GridColor = "#8FA8B3",
                TextColor = "#111111",
                Title = "组词训练",
                ShowHeaderFields = true,
                FrameBorder = true
            };
            var pages = LayoutEngine.Paginate(spec);
            var page = pages[0];
            var firstCell = page.Cells[0];
            var path = Path.Combine(directory, "bracket-word-columns.png");

            SheetExporter.ExportPng(path, spec, page, pages.Count, dpi);

            using var bitmap = SKBitmap.Decode(path);
            Assert.NotNull(bitmap);
            var itemWidth = spec.Page.UsableWidthMm / Math.Max(1, page.Columns);
            var wordWidth = Math.Min(11, itemWidth * 0.24);
            var blankX = firstCell.XMm + wordWidth + 1.5;
            var blankWidth = Math.Max(18, itemWidth - wordWidth - 5);
            var centerY = firstCell.YMm + firstCell.SizeMm / 2;
            var edgeInset = Math.Min(blankWidth * 0.22, Math.Clamp(firstCell.SizeMm * 0.28, 1.8, 3.2));
            var guideXs = new[]
            {
                blankX + edgeInset,
                blankX + blankWidth / 2,
                blankX + blankWidth - edgeInset
            };
            foreach (var guideX in guideXs)
            {
                var pixels = CountPixelsMm(
                    bitmap,
                    dpi,
                    guideX - 0.45,
                    centerY - firstCell.SizeMm * 0.36,
                    guideX + 0.45,
                    centerY + firstCell.SizeMm * 0.36,
                    IsGuideInk);
                Assert.True(pixels > 8);
            }

            var oldQuarterGuidePixels =
                CountPixelsMm(
                    bitmap,
                    dpi,
                    blankX + blankWidth * 0.25 - 0.45,
                    centerY - firstCell.SizeMm * 0.30,
                    blankX + blankWidth * 0.25 + 0.45,
                    centerY + firstCell.SizeMm * 0.30,
                    IsGuideInk) +
                CountPixelsMm(
                    bitmap,
                    dpi,
                    blankX + blankWidth * 0.75 - 0.45,
                    centerY - firstCell.SizeMm * 0.30,
                    blankX + blankWidth * 0.75 + 0.45,
                    centerY + firstCell.SizeMm * 0.30,
                    IsGuideInk);
            Assert.True(oldQuarterGuidePixels < 8);

            var horizontalPixels =
                CountPixelsMm(
                    bitmap,
                    dpi,
                    blankX + blankWidth * 0.34,
                    centerY - 0.35,
                    blankX + blankWidth * 0.46,
                    centerY + 0.35,
                    IsGuideInk) +
                CountPixelsMm(
                    bitmap,
                    dpi,
                    blankX + blankWidth * 0.54,
                    centerY - 0.35,
                    blankX + blankWidth * 0.66,
                    centerY + 0.35,
                    IsGuideInk);
            Assert.True(horizontalPixels < 10);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void ExportPng_BracketPinyinColumnsDrawsPinyinCellAndRaisedPinyinBlank()
    {
        const double dpi = 144;
        var directory = CreateTempDirectory();
        try
        {
            var spec = CreateSpec() with
            {
                Text = "坡",
                PracticeLayout = PracticeLayoutKind.BracketPinyinColumns,
                Grid = GridKind.None,
                GridColor = "#00A968",
                TextColor = "#111111",
                Title = "括号组词练习",
                ShowHeaderFields = true,
                ShowPinyin = true,
                PinyinByGlyph = new Dictionary<string, string> { ["坡"] = "pō" }
            };
            var pages = LayoutEngine.Paginate(spec);
            var page = pages[0];
            var firstCell = page.Cells[0];
            var path = Path.Combine(directory, "bracket-pinyin-columns.png");

            SheetExporter.ExportPng(path, spec, page, pages.Count, dpi);

            using var bitmap = SKBitmap.Decode(path);
            Assert.NotNull(bitmap);
            var itemWidth = spec.Page.UsableWidthMm / Math.Max(1, page.Columns);
            var boxX = firstCell.XMm + 2;
            var boxY = firstCell.YMm + 1;
            const double boxWidth = 10;
            const double boxHeight = 17;
            const double pinyinHeight = boxHeight * 0.32;
            var pinyinPixels = CountPixelsMm(
                bitmap,
                dpi,
                boxX + 1,
                boxY + 0.4,
                boxX + boxWidth - 1,
                boxY + pinyinHeight - 0.2,
                IsTextInk);
            Assert.True(pinyinPixels > 20);

            var pinyinGuidePixels =
                CountPinyinGuideLinePixels(bitmap, dpi, boxX, boxY + pinyinHeight / 3, boxWidth, IsGuideInk) +
                CountPinyinGuideLinePixels(bitmap, dpi, boxX, boxY + pinyinHeight * 2 / 3, boxWidth, IsGuideInk);
            Assert.True(pinyinGuidePixels > 12);

            var glyphPixels = CountPixelsMm(
                bitmap,
                dpi,
                boxX + 1,
                boxY + pinyinHeight + 0.8,
                boxX + boxWidth - 1,
                boxY + boxHeight - 0.8,
                IsDarkInk);
            Assert.True(glyphPixels > 40);

            var blankX = boxX + boxWidth + 1.5;
            var blankWidth = Math.Max(15, itemWidth - boxWidth - 5.5);
            var topBlankCenterY = boxY + pinyinHeight * 0.55;
            var pinyinBracketPixels = CountPixelsMm(
                bitmap,
                dpi,
                blankX,
                topBlankCenterY - 2.4,
                blankX + blankWidth,
                topBlankCenterY + 2.4,
                IsTemplateGreen);
            Assert.True(pinyinBracketPixels > 20);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void ExportPng_CharacterWordsPoemDrawsThreeTemplateSections()
    {
        var directory = CreateTempDirectory();
        try
        {
            var spec = CreateSpec() with
            {
                Text = "春 冬 风 雪 花 春天 春风 冬天 秋冬 大风 刮风 下雪 雪花 开花 花生 远上寒山石径斜，白云生处有人家。 停车坐爱枫林晚，霜叶红于二月花。",
                PracticeLayout = PracticeLayoutKind.CharacterWordsPoem,
                Grid = GridKind.None,
                GridColor = "#4CC58B",
                TextColor = "#1A1A1A",
                TraceColor = "#F2B4AA",
                ShowHeaderFields = false,
                FrameBorder = false,
                StrokeOrderByGlyph = new Dictionary<string, CharacterStrokeOrder>
                {
                    ["春"] = new([
                        "M 100 790 L 900 790 L 900 850 L 100 850 Z",
                        "M 460 120 L 540 120 L 540 850 L 460 850 Z"
                    ])
                }
            };
            var pages = LayoutEngine.Paginate(spec);
            var path = Path.Combine(directory, "character-words-poem.png");

            SheetExporter.ExportPng(path, spec, pages[0], pages.Count, dpi: 96);

            using var bitmap = SKBitmap.Decode(path);
            Assert.NotNull(bitmap);
            Assert.True(CountPixels(bitmap, 0.05, 0.05, 0.95, 0.47, IsTemplateGreen) > 1000);
            Assert.True(CountPixels(bitmap, 0.05, 0.49, 0.95, 0.66, IsTemplateGreen) > 600);
            Assert.True(CountPixels(bitmap, 0.05, 0.67, 0.95, 0.88, IsTemplateGreen) > 250);
            Assert.True(CountPixels(bitmap, 0.06, 0.68, 0.80, 0.82, IsDarkInk) > 180);
            Assert.True(CountPixelsMm(bitmap, 96, 60, 20, 105, 31, IsStrokeRed) > 10);
            Assert.True(CountPixelsMm(bitmap, 96, 51.2, 30, 52.1, 44, IsGuideGreen) > 18);
            Assert.True(CountPixelsMm(bitmap, 96, 44.2, 37, 59.1, 37.8, IsGuideGreen) > 18);
            Assert.True(CountPixelsMm(bitmap, 96, 16, 158, 22, 168, IsDarkInk) > 15);
            Assert.True(CountPixelsMm(bitmap, 96, 26, 158, 32, 168, IsDarkInk) > 15);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void ExportPng_StandardStrokeOrderHighlightsCurrentStroke()
    {
        var directory = CreateTempDirectory();
        try
        {
            var spec = CreateSpec() with
            {
                Text = "春",
                Mode = PracticeMode.Trace,
                RepeatsPerChar = 4,
                TraceSlotCount = 3,
                ShowStrokeOrder = true,
                GridColor = "#29A86C",
                TraceColor = "#C9D6D0",
                StrokeOrderByGlyph = new Dictionary<string, CharacterStrokeOrder>
                {
                    ["春"] = new([
                        "M 100 790 L 900 790 L 900 850 L 100 850 Z",
                        "M 460 120 L 540 120 L 540 850 L 460 850 Z"
                    ])
                }
            };
            var pages = LayoutEngine.Paginate(spec);
            var traceCell = pages[0].Cells[1];
            var path = Path.Combine(directory, "standard-stroke-order.png");

            SheetExporter.ExportPng(path, spec, pages[0], pages.Count, dpi: 96);

            using var bitmap = SKBitmap.Decode(path);
            Assert.NotNull(bitmap);
            Assert.True(CountPixelsMm(
                bitmap,
                96,
                traceCell.XMm,
                traceCell.YMm,
                traceCell.XMm + traceCell.SizeMm,
                traceCell.YMm + traceCell.SizeMm,
                IsStrokeRed) > 10);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void ExportPng_FiveCharacterPoemCalligraphyDrawsDynamicTitleAndPoemGrid()
    {
        var directory = CreateTempDirectory();
        try
        {
            var spec = CreateSpec() with
            {
                Text = "锄禾日当午，汗滴禾下土。谁知盘中餐，粒粒皆辛苦。",
                PracticeLayout = PracticeLayoutKind.FiveCharacterPoemCalligraphy,
                Grid = GridKind.None,
                Title = "悯农",
                Author = "李绅",
                Dynasty = "唐",
                HeaderPreset = SheetHeaderPreset.None,
                GridColor = "#63C28B",
                TextColor = "#363638",
                TraceColor = "#B9C5BE"
            };
            var pages = LayoutEngine.Paginate(spec);
            var path = Path.Combine(directory, "five-character-poem-calligraphy.png");

            SheetExporter.ExportPng(path, spec, pages[0], pages.Count, dpi: 96);

            using var bitmap = SKBitmap.Decode(path);
            Assert.NotNull(bitmap);
            Assert.True(CountPixelsMm(bitmap, 96, 68, 122, 142, 182, IsGuideGreen) > 220);
            Assert.True(CountPixelsMm(bitmap, 96, 82, 96, 128, 111, IsDarkInk) > 45);
            Assert.True(CountPixelsMm(bitmap, 96, 72, 126, 138, 180, IsGuideInk) > 180);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void ExportPng_FiveCharacterPoemCalligraphy_BlankTitleAndTextStayBlank()
    {
        var directory = CreateTempDirectory();
        try
        {
            var spec = CreateSpec() with
            {
                Text = string.Empty,
                PracticeLayout = PracticeLayoutKind.FiveCharacterPoemCalligraphy,
                Grid = GridKind.None,
                Title = null,
                Author = "李绅",
                Dynasty = "唐",
                HeaderPreset = SheetHeaderPreset.None,
                GridColor = "#63C28B",
                TextColor = "#363638",
                TraceColor = "#B9C5BE"
            };
            var pages = LayoutEngine.Paginate(spec);
            var path = Path.Combine(directory, "blank-five-character-poem-calligraphy.png");

            SheetExporter.ExportPng(path, spec, pages[0], pages.Count, dpi: 96);

            using var bitmap = SKBitmap.Decode(path);
            Assert.NotNull(bitmap);
            Assert.True(CountPixelsMm(bitmap, 96, 83, 96, 127, 111, IsDarkInk) < 20);
            Assert.True(CountPixelsMm(bitmap, 96, 72, 126, 138, 180, IsDarkInk) < 30);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void ExportPdf_WritesPdfForEveryPage()
    {
        var directory = CreateTempDirectory();
        try
        {
            var spec = CreateSpec() with { Text = new string('永', 80) };
            var pages = LayoutEngine.Paginate(spec);
            var path = Path.Combine(directory, "sheet.pdf");

            SheetExporter.ExportPdf(path, spec, pages);

            var bytes = File.ReadAllBytes(path);
            Assert.True(bytes.Length > 100);
            Assert.Equal((byte)'%', bytes[0]);
            Assert.Equal((byte)'P', bytes[1]);
            Assert.Equal((byte)'D', bytes[2]);
            Assert.Equal((byte)'F', bytes[3]);
            Assert.Contains("%%EOF", System.Text.Encoding.ASCII.GetString(bytes));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static CharacterSheetSpec CreateSpec()
    {
        return new CharacterSheetSpec
        {
            Text = "永",
            Grid = GridKind.Mi,
            Mode = PracticeMode.Copy,
            RepeatsPerChar = 1,
            Title = null,
            ShowHeaderFields = false
        };
    }

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "zitie-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static int CountPixels(
        SKBitmap bitmap,
        double leftRatio,
        double topRatio,
        double rightRatio,
        double bottomRatio,
        Func<SKColor, bool> predicate)
    {
        var count = 0;
        var left = Math.Clamp((int)(bitmap.Width * leftRatio), 0, bitmap.Width);
        var top = Math.Clamp((int)(bitmap.Height * topRatio), 0, bitmap.Height);
        var right = Math.Clamp((int)(bitmap.Width * rightRatio), left, bitmap.Width);
        var bottom = Math.Clamp((int)(bitmap.Height * bottomRatio), top, bitmap.Height);
        for (var y = top; y < bottom; y++)
        for (var x = left; x < right; x++)
            if (predicate(bitmap.GetPixel(x, y)))
                count++;

        return count;
    }

    private static int CountPinyinGuideLinePixels(
        SKBitmap bitmap,
        double dpi,
        double boxX,
        double lineY,
        double boxWidth,
        Func<SKColor, bool> predicate)
    {
        return CountPixelsMm(
                   bitmap,
                   dpi,
                   boxX + 0.3,
                   lineY - 0.25,
                   boxX + 2.4,
                   lineY + 0.25,
                   predicate) +
               CountPixelsMm(
                   bitmap,
                   dpi,
                   boxX + boxWidth - 2.4,
                   lineY - 0.25,
                   boxX + boxWidth - 0.3,
                   lineY + 0.25,
                   predicate);
    }

    private static bool IsTemplateGreen(SKColor pixel)
    {
        return pixel.Green > 110 &&
               pixel.Red < 90 &&
               pixel.Blue < 140;
    }

    private static bool IsDarkInk(SKColor pixel)
    {
        return pixel.Red < 80 &&
               pixel.Green < 80 &&
               pixel.Blue < 80;
    }

    private static bool IsStrokeRed(SKColor pixel)
    {
        return pixel.Red > 190 &&
               pixel.Green < 150 &&
               pixel.Blue < 145;
    }

    private static bool IsGuideGreen(SKColor pixel)
    {
        return pixel.Green > 150 &&
               pixel.Red < 230 &&
               pixel.Blue < 230 &&
               pixel.Green >= pixel.Red &&
               pixel.Green >= pixel.Blue;
    }

    private static bool IsTextInk(SKColor pixel)
    {
        return pixel.Red < 185 &&
               pixel.Green < 185 &&
               pixel.Blue < 185;
    }

    private static int CountPixelsMm(
        SKBitmap bitmap,
        double dpi,
        double leftMm,
        double topMm,
        double rightMm,
        double bottomMm,
        Func<SKColor, bool> predicate)
    {
        var pxPerMm = dpi / 25.4;
        var count = 0;
        var left = Math.Clamp((int)Math.Floor(leftMm * pxPerMm), 0, bitmap.Width);
        var top = Math.Clamp((int)Math.Floor(topMm * pxPerMm), 0, bitmap.Height);
        var right = Math.Clamp((int)Math.Ceiling(rightMm * pxPerMm), left, bitmap.Width);
        var bottom = Math.Clamp((int)Math.Ceiling(bottomMm * pxPerMm), top, bitmap.Height);
        for (var y = top; y < bottom; y++)
        for (var x = left; x < right; x++)
            if (predicate(bitmap.GetPixel(x, y)))
                count++;

        return count;
    }

    private static bool IsGuideInk(SKColor pixel)
    {
        return pixel.Red < 238 &&
               pixel.Green < 242 &&
               pixel.Blue < 246 &&
               pixel.Blue >= pixel.Red;
    }
}
