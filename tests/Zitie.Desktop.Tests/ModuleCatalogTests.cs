using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using Xunit;
using Zitie.Desktop.Services;

namespace Zitie.Desktop.Tests;

public sealed class ModuleCatalogTests
{
    [Fact]
    public void BuiltInCatalog_LoadsZiTemplatePackagesWithArtwork()
    {
        var catalog = new ModuleCatalog();

        Assert.True(catalog.Modules.Count >= 30);
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
    public void Reload_LoadsOnlyZiPackagesAndCachesAssetsInMemory()
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
            Assert.Equal("<svg xmlns=\"http://www.w3.org/2000/svg\" />", module.ReadTextAsset("assets/background.svg"));
            Assert.Null(catalog.Find("loose"));
        }
        finally
        {
            Directory.Delete(builtInDirectory, recursive: true);
            Directory.Delete(userDirectory, recursive: true);
        }
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
