using Xunit;
using System.IO.Compression;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Zitie.Core.Models;
using Zitie.Desktop.Models;
using Zitie.Desktop.Services;

namespace Zitie.Desktop.Tests;

public sealed class SheetDocumentStoreTests
{
    [Fact]
    public void SaveAndLoad_PreservesVisualAndPageSettings()
    {
        var path = Path.Combine(Path.GetTempPath(), $"zitie-test-{Guid.NewGuid():N}.zitie.json");
        try
        {
            var spec = new CharacterSheetSpec
            {
                Text = "春眠不觉晓",
                BlankContentLayout = true,
                FillContentAreaWithBlankCells = true,
                LayoutColumns = 5,
                LayoutRows = 4,
                Grid = GridKind.Nine,
                Mode = PracticeMode.Copy,
                GridColor = "#123456",
                TextColor = "#654321",
                FontFamilyName = "Test Font",
                Page = new PageSettings
                {
                    WidthMm = 297,
                    HeightMm = 210,
                    MarginTopMm = 12,
                    MarginBottomMm = 12,
                    MarginLeftMm = 18,
                    MarginRightMm = 18
                },
                PinyinByGlyph = new Dictionary<string, string> { ["春"] = "chun1" }
            };

            SheetDocumentStore.Save(path, spec, "poem-wuyan");
            var json = File.ReadAllText(path);
            var loaded = SheetDocumentStore.Load(path);

            Assert.DoesNotContain("pinyinByGlyph", json, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("poem-wuyan", loaded.ModuleId);
            Assert.Equal(spec.Text, loaded.Spec.Text);
            Assert.True(loaded.Spec.BlankContentLayout);
            Assert.True(loaded.Spec.FillContentAreaWithBlankCells);
            Assert.Equal(5, loaded.Spec.LayoutColumns);
            Assert.Equal(4, loaded.Spec.LayoutRows);
            Assert.Equal(spec.GridColor, loaded.Spec.GridColor);
            Assert.Equal(spec.TextColor, loaded.Spec.TextColor);
            Assert.Equal(spec.Page, loaded.Spec.Page);
            Assert.Null(loaded.Spec.PinyinByGlyph);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void SaveTemplate_WritesZiPackageWithModuleAndArtwork()
    {
        var path = Path.Combine(Path.GetTempPath(), $"zitie-template-{Guid.NewGuid():N}.zi");
        try
        {
            SheetDocumentStore.SaveTemplate(path, new CharacterSheetSpec
            {
                Text = "一二三",
                BlankContentLayout = true,
                FillContentAreaWithBlankCells = true,
                LayoutColumns = 3,
                LayoutRows = 2,
                GridColor = "#123456",
                GroupGapMm = 7,
                BackgroundArtworkSvg = "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 1 1\" />",
                Page = PageSettings.A4 with { MarginTopMm = 22 }
            }, "我的模板");

            using var archive = ZipFile.OpenRead(path);
            var moduleEntry = archive.GetEntry("module.yml");
            Assert.NotNull(moduleEntry);
            Assert.NotNull(archive.GetEntry("assets/background.svg"));
            using var reader = new StreamReader(
                moduleEntry!.Open(),
                encoding: Encoding.UTF8,
                detectEncodingFromByteOrderMarks: true);
            var moduleText = reader.ReadToEnd();
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            var module = deserializer.Deserialize<ModuleDefinition>(moduleText);
            Assert.Equal("我的模板", module.Name);
            Assert.Equal("#123456", module.Defaults.GridColor);
            Assert.True(module.Defaults.BlankContentLayout);
            Assert.True(module.Defaults.FillContentAreaWithBlankCells);
            Assert.Equal(3, module.Defaults.LayoutColumns);
            Assert.Equal(2, module.Defaults.LayoutRows);
            Assert.Equal(7d, module.Defaults.GroupGap);
            Assert.Equal("assets/background.svg", module.Defaults.BackgroundArtwork);
            Assert.Equal("a4Portrait", module.Defaults.PageSize);
            Assert.Equal(22d, module.Defaults.PageMargin);
            Assert.Equal(22d, module.Defaults.PageMarginTop);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void EditorState_FromSpec_PreservesEditorValues()
    {
        var spec = new CharacterSheetSpec
        {
            Text = "永",
            BlankContentLayout = true,
            FillContentAreaWithBlankCells = true,
            LayoutColumns = 1,
            LayoutRows = 1,
            Title = "大字帖",
            Grid = GridKind.HuiGong,
            Mode = PracticeMode.Copy,
                CellsPerLine = 16,
                GroupGapMm = 6,
                GridColor = "#123456",
            TextColor = "#654321",
            HeaderPreset = SheetHeaderPreset.Custom,
            HeaderTextTemplate = "姓名---日期",
            Orientation = SheetOrientation.Vertical,
            Page = PageSettings.A4 with { MarginTopMm = 19 }
        };

        var state = SheetEditorState.FromSpec(spec);

        Assert.Equal(string.Empty, state.InputText);
        Assert.True(state.BlankContentLayout);
        Assert.True(state.FillContentAreaWithBlankCells);
        Assert.Equal(1, state.LayoutColumns);
        Assert.Equal(1, state.LayoutRows);
        Assert.Equal(spec.Grid, state.Grid);
        Assert.Equal(spec.Mode, state.Mode);
        Assert.Equal(16, state.CellsPerLine);
        Assert.Equal(6, state.GroupGapMm);
        Assert.Equal("#123456", state.GridColor);
        Assert.Equal(SheetHeaderPreset.Custom, state.HeaderPreset);
        Assert.Equal(SheetOrientation.Vertical, state.Orientation);
        Assert.Equal(19, state.Page.MarginTopMm);
    }

    [Fact]
    public void Load_RejectsDocumentWithoutSpec()
    {
        var path = Path.Combine(Path.GetTempPath(), $"zitie-invalid-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, "{\"version\":1}");
            Assert.Throws<InvalidDataException>(() => SheetDocumentStore.Load(path));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
