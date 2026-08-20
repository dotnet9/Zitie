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
}
