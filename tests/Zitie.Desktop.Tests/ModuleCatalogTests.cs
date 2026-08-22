using Xunit;
using Zitie.Core.Models;
using Zitie.Desktop.Services;

namespace Zitie.Desktop.Tests;

public sealed class ModuleCatalogTests
{
    [Fact]
    public void BuiltInCatalog_LoadsNqezStyleTemplatesWithImages()
    {
        var catalog = new ModuleCatalog();

        Assert.Equal(223, catalog.Modules.Count);
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

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "zitie-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}
