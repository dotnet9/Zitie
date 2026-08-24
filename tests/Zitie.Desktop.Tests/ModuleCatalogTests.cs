using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using SkiaSharp;
using Svg.Skia;
using Xunit;
using Zitie.Core.Models;
using Zitie.Desktop.Services;

namespace Zitie.Desktop.Tests;

public sealed class ModuleCatalogTests
{
    [Fact]
    public void DeferredCatalog_LoadsOnlyWhenRequested()
    {
        var catalog = new ModuleCatalog(loadImmediately: false);

        Assert.False(catalog.IsLoaded);
        Assert.Empty(catalog.Modules);

        catalog.EnsureLoaded();
        var loadedModules = catalog.Modules;
        catalog.EnsureLoaded();

        Assert.True(catalog.IsLoaded);
        Assert.Equal(326, loadedModules.Count);
        Assert.Same(loadedModules, catalog.Modules);
    }

    [Fact]
    public void BuiltInCatalog_LoadsNqezStyleTemplatesWithImages()
    {
        var catalog = new ModuleCatalog();

        Assert.Equal(326, catalog.Modules.Count);
        var module = Assert.Single(catalog.Modules, item => item.Id == "nqez-945");
        Assert.Equal("加宽拼音格帖", module.Name);
        Assert.Equal("945", module.SourceTemplateId);
        Assert.Contains("汉字", module.Categories);
        Assert.Contains("拼音", module.Categories);
        Assert.DoesNotContain("高级VIP", module.Categories);
        Assert.True(File.Exists(module.PreviewImagePath));
        Assert.False(module.Defaults.BlankContentLayout);
        Assert.True(module.Defaults.FillContentAreaWithBlankCells);
        Assert.True(module.Defaults.ShowPinyin);
    }

    [Fact]
    public void BuiltInCatalog_LoadsTemplateDirectoriesWithArtwork()
    {
        var catalog = new ModuleCatalog();
        var directoryModules = catalog.Modules
            .Where(module => string.IsNullOrEmpty(module.SourceTemplateId))
            .ToArray();

        Assert.Equal(103, directoryModules.Length);
        Assert.All(directoryModules, module => Assert.True(Directory.Exists(module.SourcePath)));
        var module = Assert.Single(directoryModules, item => item.Id == "poem-wuyan-spring-scene");
        Assert.Equal("assets/background.svg", module.Defaults.BackgroundArtwork);
        Assert.Contains("<svg", module.ReadTextAsset(module.Defaults.BackgroundArtwork));
    }

    [Fact]
    public void BuiltInCatalog_MapsVipTagToRegularCategoriesOnly()
    {
        var catalog = new ModuleCatalog();
        var module = Assert.Single(catalog.Modules, item => item.Id == "nqez-6380");

        Assert.Contains("试卷", module.Categories);
        Assert.DoesNotContain("高级VIP", module.Categories);
        Assert.True(module.Defaults.BlankContentLayout);
        Assert.False(module.Defaults.FillContentAreaWithBlankCells);
        Assert.Equal("a4Landscape", module.Defaults.PageSize);
    }

    [Fact]
    public void BuiltInBlankLayoutTemplates_RenderBlankPracticeCells()
    {
        var catalog = new ModuleCatalog();
        var module = Assert.Single(catalog.Modules, item => item.Id == "nqez-960");

        Assert.True(module.Defaults.BlankContentLayout);
        Assert.Null(module.Defaults.Text);

        var preview = ModulePreviewFactory.Create(module);
        var page = Assert.Single(preview.Pages);
        Assert.NotEmpty(page.Cells);
        Assert.All(page.Cells, cell => Assert.Equal(CellRole.Blank, cell.Role));
    }

    [Fact]
    public void BuiltInFillTemplates_InferEditablePracticeDefaults()
    {
        var catalog = new ModuleCatalog();
        var english = Assert.Single(catalog.Modules, item => item.Id == "nqez-1013");
        var preview = ModulePreviewFactory.Create(english);

        Assert.Contains("英文", english.Categories);
        Assert.Equal(GridKind.English, preview.Spec.Grid);
        Assert.False(preview.Spec.BlankContentLayout);
        Assert.NotEmpty(preview.Spec.Text);
    }

    [Fact]
    public void PoetryTemplates_DoNotUseGenericBlankCellFill()
    {
        var catalog = new ModuleCatalog();
        var poem = Assert.Single(catalog.Modules, item => item.Id == "nqez-953");

        Assert.Contains("书法", poem.Categories);
        Assert.False(poem.Defaults.FillContentAreaWithBlankCells);
    }

    [Fact]
    public void ModuleSearch_SplitsBracketGroupWords()
    {
        var catalog = new ModuleCatalog();

        Assert.Equal(["括号", "组词"], ModuleSearch.ParseTerms("括号组词"));

        var matches = catalog.Modules
            .Where(module => ModuleSearch.Matches(module, "括号组词"))
            .Select(module => module.Id)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["nqez-1002", "nqez-43174", "nqez-948", "nqez-949"], matches);
    }

    [Fact]
    public void BracketGroupTemplates_UseDistinctExampleImages()
    {
        var catalog = new ModuleCatalog();
        var module = Assert.Single(catalog.Modules, item => item.Id == "nqez-43174");

        Assert.EndsWith("括号组词练习-43174.png", module.PreviewImagePath);
        Assert.True(File.Exists(module.PreviewImagePath));
    }

    [Fact]
    public void BracketGroupTemplates_DistinguishGridAndNonGridLayouts()
    {
        var catalog = new ModuleCatalog();

        var gridTemplate = ModulePreviewFactory.Create(Assert.Single(catalog.Modules, item => item.Id == "nqez-1002"));
        var nonGridTemplates = new[] { "nqez-948", "nqez-949", "nqez-43174" }
            .Select(id => ModulePreviewFactory.Create(Assert.Single(catalog.Modules, item => item.Id == id)))
            .ToArray();

        Assert.Equal(GridKind.Plain, gridTemplate.Spec.Grid);
        Assert.Equal(PracticeLayoutKind.BracketGridWords, gridTemplate.Spec.PracticeLayout);
        Assert.All(nonGridTemplates, preview => Assert.Equal(GridKind.None, preview.Spec.Grid));
        Assert.Equal(
            [
                PracticeLayoutKind.BracketWordRows,
                PracticeLayoutKind.BracketWordColumns,
                PracticeLayoutKind.BracketPinyinColumns
            ],
            nonGridTemplates.Select(preview => preview.Spec.PracticeLayout));
    }

    [Fact]
    public void CharacterWordsPoemTemplate_UsesDedicatedLayout()
    {
        var catalog = new ModuleCatalog();
        var module = Assert.Single(catalog.Modules, item => item.Id == "nqez-951");
        var preview = ModulePreviewFactory.Create(module);

        Assert.Equal("生字组词古诗", module.Name);
        Assert.Equal(PracticeLayoutKind.CharacterWordsPoem, preview.Spec.PracticeLayout);
        Assert.Equal(GridKind.None, preview.Spec.Grid);
        Assert.Equal("#84CC9B", preview.Spec.GridColor);
        Assert.Equal("#514E51", preview.Spec.TextColor);
        Assert.Equal("#FED9D3", preview.Spec.TraceColor);
        Assert.Contains("远上寒山石径斜", preview.Spec.Text);
        Assert.False(preview.Spec.ShowHeaderFields);
    }

    [Fact]
    public void FiveCharacterPoemCalligraphyTemplate_UsesDedicatedLayout()
    {
        var catalog = new ModuleCatalog();
        var module = Assert.Single(catalog.Modules, item => item.Id == "nqez-953");
        var preview = ModulePreviewFactory.Create(module);

        Assert.Equal("五言古诗书法", module.Name);
        Assert.Equal("953", module.SourceTemplateId);
        Assert.True(Directory.Exists(module.SourcePath));
        Assert.Equal("assets/background.svg", module.Defaults.BackgroundArtwork);
        Assert.Contains("<svg", module.ReadTextAsset(module.Defaults.BackgroundArtwork));
        Assert.Contains("汉字", module.Categories);
        Assert.Contains("书法", module.Categories);
        Assert.True(File.Exists(module.PreviewImagePath));
        Assert.False(module.Defaults.BlankContentLayout);
        Assert.False(module.Defaults.FillContentAreaWithBlankCells);
        Assert.True(module.Defaults.Vertical);
        Assert.Equal(PracticeLayoutKind.FiveCharacterPoemCalligraphy, preview.Spec.PracticeLayout);
        Assert.Equal(GridKind.None, preview.Spec.Grid);
        Assert.Equal("悯农", preview.Spec.Title);
        Assert.Equal("李绅", preview.Spec.Author);
        Assert.Equal("唐", preview.Spec.Dynasty);
        Assert.Contains("锄禾日当午", preview.Spec.Text);
        Assert.False(preview.Spec.ShowHeaderFields);
        Assert.Equal(5, preview.Pages[0].Columns);
        Assert.Equal(4, preview.Pages[0].Rows);
    }

    [Theory]
    [InlineData("nqez-957", "957", 4, 7, true, false)]
    [InlineData("nqez-3769", "3769", 5, 8, false, false)]
    [InlineData("nqez-17534", "17534", 4, 7, true, false)]
    [InlineData("nqez-19388", "19388", 7, 4, false, false)]
    [InlineData("nqez-23691", "23691", 5, 4, false, true)]
    [InlineData("nqez-24938", "24938", 4, 7, true, false)]
    [InlineData("nqez-35157", "35157", 4, 5, true, false)]
    public void DecoratedPoemTemplates_UseDirectorySvgAndExplicitGridLayout(
        string id,
        string sourceTemplateId,
        int columns,
        int rows,
        bool vertical,
        bool showPinyin)
    {
        var catalog = new ModuleCatalog();
        var module = Assert.Single(catalog.Modules, item => item.Id == id);
        var preview = ModulePreviewFactory.Create(module);

        Assert.Equal(sourceTemplateId, module.SourceTemplateId);
        Assert.True(Directory.Exists(module.SourcePath));
        Assert.Equal("assets/background.svg", module.Defaults.BackgroundArtwork);
        Assert.Contains("<svg", module.ReadTextAsset(module.Defaults.BackgroundArtwork));
        Assert.True(File.Exists(module.PreviewImagePath));
        Assert.Equal(columns, module.Defaults.LayoutColumns);
        Assert.Equal(rows, module.Defaults.LayoutRows);
        Assert.Equal(vertical, module.Defaults.Vertical);
        Assert.Equal(showPinyin, module.Defaults.ShowPinyin);

        var page = Assert.Single(preview.Pages);
        Assert.Equal(columns, page.Columns);
        Assert.Equal(rows, page.Rows);
        Assert.Equal(columns * rows, page.Cells.Count);
        Assert.False(string.IsNullOrWhiteSpace(preview.Spec.BackgroundArtworkSvg));
    }

    [Fact]
    public void BuiltInTemplateArtwork_IsPureSvgWithoutRasterDependencies()
    {
        var catalog = new ModuleCatalog();
        var rasterExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".bmp", ".gif", ".jpeg", ".jpg", ".png", ".webp"
        };

        var rasterAssets = Directory.EnumerateFiles(catalog.BuiltInDirectory, "*", SearchOption.AllDirectories)
            .Where(path => rasterExtensions.Contains(Path.GetExtension(path)))
            .ToArray();
        Assert.Empty(rasterAssets);

        foreach (var svgPath in Directory.EnumerateFiles(
                     catalog.BuiltInDirectory,
                     "*.svg",
                     SearchOption.AllDirectories))
        {
            var svgText = File.ReadAllText(svgPath);
            Assert.DoesNotContain("<image", svgText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("data:image", svgText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotMatch("(?i)href\\s*=\\s*[\"'][^\"']+\\.(?:bmp|gif|jpe?g|png|webp)", svgText);

            using var svg = new SKSvg();
            using var reader = System.Xml.XmlReader.Create(new StringReader(svgText));
            svg.Load(reader);
            Assert.NotNull(svg.Picture);
        }

        var builtInRoot = Path.GetFullPath(catalog.BuiltInDirectory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var modulesWithArtwork = catalog.Modules.Where(module =>
            !string.IsNullOrWhiteSpace(module.SourcePath) &&
            Path.GetFullPath(module.SourcePath).StartsWith(builtInRoot, StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(module.Defaults.BackgroundArtwork));
        Assert.All(modulesWithArtwork, module =>
            Assert.EndsWith(".svg", module.Defaults.BackgroundArtwork, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void BuiltInStrokeOrderTemplates_EnableStrokeOrderDefaults()
    {
        var catalog = new ModuleCatalog();
        var module = Assert.Single(catalog.Modules, item => item.Id == "nqez-19279");
        var preview = ModulePreviewFactory.Create(module);

        Assert.Equal("汉字笔顺练习", module.Name);
        Assert.Contains("有笔顺", module.Categories);
        Assert.True(module.Defaults.ShowStrokeOrder);
        Assert.True(preview.Spec.ShowStrokeOrder);
        Assert.Equal(8, preview.Spec.RepeatsPerChar);
        Assert.Equal(7, preview.Spec.TraceSlotCount);
        Assert.Equal(8, preview.Spec.CellsPerLine);
    }

    [Fact]
    public void Reload_LoadsOnlyStyleCatalogItems()
    {
        var styleDirectory = CreateTempDirectory();
        var imageDirectory = Path.Combine(styleDirectory, "images");
        Directory.CreateDirectory(imageDirectory);
        try
        {
            File.WriteAllText(Path.Combine(styleDirectory, "loose.yml"), """
                id: "loose"
                name: "散落配置"
                """);
            File.WriteAllText(Path.Combine(styleDirectory, "styles.json"), """
                {
                  "sourceUrl": "test",
                  "retrievedDate": "2026-08-22",
                  "itemCount": 1,
                  "items": [
                    {
                      "id": "1",
                      "title": "英文单词练习",
                      "imageFileName": "missing.png",
                      "url": "test",
                      "tags": [4]
                    }
                  ]
                }
                """);

            var catalog = new ModuleCatalog(styleDirectory, imageDirectory);
            var module = Assert.Single(catalog.Modules);

            Assert.Equal("nqez-1", module.Id);
            Assert.Equal("英文单词练习", module.Name);
            Assert.Contains("英文", module.Categories);
            Assert.Null(catalog.Find("loose"));
        }
        finally
        {
            Directory.Delete(styleDirectory, recursive: true);
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

            var catalog = CreateDirectoryCatalog(builtInDirectory, userDirectory);
            var module = Assert.Single(catalog.Modules);

            Assert.Equal("packaged", module.Id);
            Assert.Equal(Path.Combine(builtInDirectory, "packaged"), module.SourcePath);
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

            var catalog = CreateDirectoryCatalog(builtInDirectory, userDirectory);
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
                category: "基础"
                """);

            var catalog = CreateDirectoryCatalog(builtInDirectory, userDirectory);
            var module = Assert.Single(catalog.Modules);

            Assert.Equal("source-template", module.Id);
            Assert.Equal(moduleDirectory, module.SourcePath);
            Assert.Equal("基础", module.Category);
            Assert.Equal(["基础"], module.Categories);
        }
        finally
        {
            Directory.Delete(builtInDirectory, recursive: true);
            Directory.Delete(userDirectory, recursive: true);
        }
    }

    [Fact]
    public void Reload_UserTemplateOverridesBuiltInTemplateWithSameId()
    {
        var builtInDirectory = CreateTempDirectory();
        var userDirectory = CreateTempDirectory();
        try
        {
            WriteModuleDirectory(builtInDirectory, "built-in", "same-id", "内置模板");
            WriteModuleDirectory(userDirectory, "user", "same-id", "用户模板");

            var catalog = CreateDirectoryCatalog(builtInDirectory, userDirectory);
            var module = Assert.Single(catalog.Modules);

            Assert.Equal("用户模板", module.Name);
            Assert.Equal(Path.Combine(userDirectory, "user"), module.SourcePath);
            Assert.Equal(["自定义"], module.Categories);
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

    private static void WriteModuleDirectory(string root, string directoryName, string id, string name)
    {
        var moduleDirectory = Path.Combine(root, directoryName);
        Directory.CreateDirectory(moduleDirectory);
        File.WriteAllText(Path.Combine(moduleDirectory, "module.yml"), $"""
            id: "{id}"
            name: "{name}"
            description: "测试模板"
            """);
    }

    private static ModuleCatalog CreateDirectoryCatalog(string builtInDirectory, string userDirectory)
    {
        var missingStyleDirectory = Path.Combine(builtInDirectory, ".missing-styles");
        return new ModuleCatalog(
            missingStyleDirectory,
            Path.Combine(missingStyleDirectory, "images"),
            builtInDirectory,
            userDirectory);
    }

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "zitie-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}
