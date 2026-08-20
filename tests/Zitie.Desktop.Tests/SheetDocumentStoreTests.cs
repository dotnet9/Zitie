using System.Text.Json;
using Xunit;
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
    public void SaveTemplate_WritesModuleCompatibleDefaults()
    {
        var path = Path.Combine(Path.GetTempPath(), $"zitie-template-{Guid.NewGuid():N}.json");
        try
        {
            SheetDocumentStore.SaveTemplate(path, new CharacterSheetSpec
            {
                Text = "一二三",
                GridColor = "#123456",
                Page = PageSettings.A4 with { MarginTopMm = 22 }
            }, "我的模板");

            using var json = JsonDocument.Parse(File.ReadAllText(path));
            var defaults = json.RootElement.GetProperty("defaults");
            Assert.Equal("#123456", defaults.GetProperty("gridColor").GetString());
            Assert.Equal("a4Portrait", defaults.GetProperty("pageSize").GetString());
            Assert.Equal(22, defaults.GetProperty("pageMargin").GetDouble());
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
            Title = "大字帖",
            Grid = GridKind.HuiGong,
            Mode = PracticeMode.Copy,
            CharactersPerLine = 16,
            GridColor = "#123456",
            TextColor = "#654321",
            HeaderPreset = SheetHeaderPreset.Custom,
            HeaderTextTemplate = "姓名---日期",
            Orientation = SheetOrientation.Vertical,
            Page = PageSettings.A4 with { MarginTopMm = 19 }
        };

        var state = SheetEditorState.FromSpec(spec);

        Assert.Equal(spec.Text, state.InputText);
        Assert.Equal(spec.Grid, state.Grid);
        Assert.Equal(spec.Mode, state.Mode);
        Assert.Equal(16, state.CharactersPerLine);
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
