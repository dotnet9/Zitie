using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using SkiaSharp;
using Svg.Skia;
using Xunit;
using Zitie.Desktop.Services;

namespace Zitie.Desktop.Tests;

public sealed class ModuleCatalogTests
{
    [Fact]
    public void BuiltInCatalog_LoadsTemplateDirectoriesWithArtwork()
    {
        var catalog = new ModuleCatalog();

        Assert.True(catalog.Modules.Count >= 100);
        Assert.All(catalog.Modules, module => Assert.True(Directory.Exists(module.SourcePath)));
        var module = Assert.Single(catalog.Modules, item => item.Id == "poem-wuyan-spring-scene");
        Assert.Equal("assets/background.svg", module.Defaults.BackgroundArtwork);
        Assert.Contains("<svg", module.ReadTextAsset(module.Defaults.BackgroundArtwork));
    }

    [Fact]
    public void BuiltInArtworkTemplates_HaveReadableSvgAssets()
    {
        var catalog = new ModuleCatalog();
        var modules = catalog.Modules
            .Where(module => !string.IsNullOrWhiteSpace(module.Defaults.BackgroundArtwork))
            .ToArray();

        Assert.True(modules.Length >= 10);
        foreach (var module in modules)
        {
            var svg = module.ReadTextAsset(module.Defaults.BackgroundArtwork);
            Assert.False(string.IsNullOrWhiteSpace(svg));
            XDocument.Parse(svg);
            Assert.NotEmpty(ModulePreviewFactory.Create(module).Pages);
        }
    }

    [Fact]
    public void Reload_ExtractsZiPackageAndLoadsTheCreatedDirectory()
    {
        var builtInDirectory = CreateTempDirectory();
        var userDirectory = CreateTempDirectory();
        try
        {
            File.WriteAllText(Path.Combine(builtInDirectory, "loose.yml"), """
                id: "loose"
                name: "散落配置"
                description: "不应被加载"
                """);
            WritePackage(Path.Combine(builtInDirectory, "packaged.zi"), """
                id: "packaged"
                name: "模板包"
                description: "应该被加载"
                defaults:
                  backgroundArtwork: "assets/background.svg"
                """, "<svg xmlns=\"http://www.w3.org/2000/svg\" />");

            var catalog = new ModuleCatalog(builtInDirectory, userDirectory);
            var module = Assert.Single(catalog.Modules);

            Assert.Equal("packaged", module.Id);
            Assert.Equal(
                Path.Combine(builtInDirectory, "packaged"),
                module.SourcePath);
            Assert.Equal("<svg xmlns=\"http://www.w3.org/2000/svg\" />", module.ReadTextAsset("assets/background.svg"));
            Assert.True(File.Exists(Path.Combine(builtInDirectory, "packaged", "module.yml")));
            Assert.True(File.Exists(Path.Combine(builtInDirectory, "packaged", "assets", "background.svg")));
            Assert.Null(catalog.Find("loose"));
        }
        finally
        {
            Directory.Delete(builtInDirectory, recursive: true);
            Directory.Delete(userDirectory, recursive: true);
        }
    }

    [Fact]
    public void BuiltInTemplateDirectories_AllHaveValidModuleAndPreview()
    {
        var catalog = new ModuleCatalog();

        Assert.True(catalog.Modules.Count >= 100);
        foreach (var module in catalog.Modules)
        {
            Assert.False(string.IsNullOrWhiteSpace(module.Name));
            Assert.False(string.IsNullOrWhiteSpace(module.Description));
            Assert.NotEmpty(ModulePreviewFactory.Create(module).Pages);

            foreach (var asset in module.Assets)
            {
                if (!asset.Key.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)) continue;
                XDocument.Parse(Encoding.UTF8.GetString(asset.Value));
            }
        }
    }

    [Fact]
    public void BuiltInSvgAssets_CanBeLoadedAndScaledBySvgSkia()
    {
        var catalog = new ModuleCatalog();
        var svgAssets = catalog.Modules
            .SelectMany(module => module.Assets
                .Where(asset => asset.Key.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                .Select(asset => Encoding.UTF8.GetString(asset.Value)))
            .ToArray();

        Assert.True(svgAssets.Length >= 80);
        foreach (var svgText in svgAssets)
        {
            using var svg = new SKSvg();
            using var reader = System.Xml.XmlReader.Create(new StringReader(svgText));
            svg.Load(reader);
            Assert.NotNull(svg.Picture);
            Assert.True(svg.Picture!.CullRect.Width > 0);

            using var bitmap = new SKBitmap(420, 594);
            using var canvas = new SKCanvas(bitmap);
            var source = svg.Picture.CullRect;
            canvas.Scale(420f / source.Width, 594f / source.Height);
            canvas.Translate(-source.Left, -source.Top);
            canvas.DrawPicture(svg.Picture);
        }
    }

    [Fact]
    public void Reload_UsesExistingSameNameDirectoryWithoutReadingZiPackage()
    {
        var builtInDirectory = CreateTempDirectory();
        var userDirectory = CreateTempDirectory();
        try
        {
            File.WriteAllText(Path.Combine(builtInDirectory, "editable.zi"), "not a zip package");
            var moduleDirectory = Path.Combine(builtInDirectory, "editable");
            Directory.CreateDirectory(Path.Combine(moduleDirectory, "assets"));
            File.WriteAllText(Path.Combine(moduleDirectory, "module.yml"), """
                id: "editable"
                name: "可编辑目录模板"
                description: "应优先读取目录"
                defaults:
                  backgroundArtwork: "assets/background.svg"
                """);
            File.WriteAllText(
                Path.Combine(moduleDirectory, "assets", "background.svg"),
                "<svg xmlns=\"http://www.w3.org/2000/svg\"><!-- directory --></svg>");

            var catalog = new ModuleCatalog(builtInDirectory, userDirectory);
            var module = Assert.Single(catalog.Modules);

            Assert.Equal("editable", module.Id);
            Assert.Equal(moduleDirectory, module.SourcePath);
            Assert.Contains("directory", module.ReadTextAsset("assets/background.svg"));
        }
        finally
        {
            Directory.Delete(builtInDirectory, recursive: true);
            Directory.Delete(userDirectory, recursive: true);
        }
    }

    [Fact]
    public void Reload_LoadsStandaloneTemplateDirectoryWithoutZiPackage()
    {
        var builtInDirectory = CreateTempDirectory();
        var userDirectory = CreateTempDirectory();
        try
        {
            var moduleDirectory = Path.Combine(builtInDirectory, "source-template");
            Directory.CreateDirectory(moduleDirectory);
            File.WriteAllText(Path.Combine(moduleDirectory, "module.yml"), """
                id: "source-template"
                name: "源码目录模板"
                description: "调试时无需 zi 文件"
                """);

            var catalog = new ModuleCatalog(builtInDirectory, userDirectory);
            var module = Assert.Single(catalog.Modules);

            Assert.Equal("source-template", module.Id);
            Assert.Equal(moduleDirectory, module.SourcePath);
        }
        finally
        {
            Directory.Delete(builtInDirectory, recursive: true);
            Directory.Delete(userDirectory, recursive: true);
        }
    }

    [Fact]
    public void ModulePreview_UsesExplicitTemplateCategory()
    {
        var module = new ModuleDefinition
        {
            Id = "categorized",
            Name = "分类模板",
            Category = "诗词",
            Defaults = new ModuleDefaults { Text = "春风" }
        };

        Assert.Equal("诗词", ModulePreviewFactory.Create(module).Category);
    }

    private static void WritePackage(string path, string moduleText, string svg)
    {
        using var stream = File.Create(path);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create);
        WriteTextEntry(archive, "module.yml", moduleText);
        WriteTextEntry(archive, "assets/background.svg", svg);
    }

    private static void WriteTextEntry(ZipArchive archive, string path, string text)
    {
        var entry = archive.CreateEntry(path);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        writer.Write(text);
    }

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "zitie-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}
