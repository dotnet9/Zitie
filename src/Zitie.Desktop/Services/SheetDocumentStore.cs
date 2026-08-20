using System.Text.Json;
using Zitie.Core.Models;

namespace Zitie.Desktop.Services;

/// <summary>可移植的字帖文档格式：保存编辑器规格，不保存运行时拼音缓存。</summary>
public sealed record SheetDocument
{
    public int Version { get; init; } = 1;

    public string? ModuleId { get; init; }

    public CharacterSheetSpec Spec { get; init; } = new();
}

/// <summary>字帖文档使用 JSON，用户模板使用 YAML。</summary>
public static class SheetDocumentStore
{
    public static void Save(string path, CharacterSheetSpec spec, string? moduleId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(spec);

        var document = new SheetDocument
        {
            ModuleId = moduleId,
            Spec = spec with { PinyinByGlyph = null }
        };
        File.WriteAllText(path, JsonSerializer.Serialize(document, ZitieJsonContext.Default.SheetDocument));
    }

    public static SheetDocument Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var json = File.ReadAllText(path);
        using var root = JsonDocument.Parse(json);
        if (root.RootElement.ValueKind != JsonValueKind.Object ||
            !root.RootElement.TryGetProperty("spec", out var spec) ||
            spec.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("字帖文档为空或格式无效。");

        var document = JsonSerializer.Deserialize(json, ZitieJsonContext.Default.SheetDocument);
        if (document is null)
            throw new InvalidDataException("字帖文档为空或格式无效。");

        return document;
    }

    public static void SaveTemplate(string path, CharacterSheetSpec spec, string? name = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(spec);

        var templateName = string.IsNullOrWhiteSpace(name)
            ? Path.GetFileNameWithoutExtension(path)
            : name.Trim();
        var defaults = new ModuleDefaults
        {
            Grid = spec.Grid.ToString().ToLowerInvariant(),
            Mode = spec.Mode.ToString().ToLowerInvariant(),
            Repeats = spec.RepeatsPerChar,
            TraceCount = spec.TraceSlotCount,
            GridSize = spec.GridSizeMm,
            GridGap = spec.GridGapMm,
            Title = spec.Title,
            HeaderPreset = spec.HeaderPreset.ToString().ToLowerInvariant(),
            HeaderText = spec.HeaderTextTemplate,
            CellsPerLine = spec.CellsPerLine,
            BlankCellLineCount = spec.BlankCellLineCount,
            Vertical = spec.Orientation == SheetOrientation.Vertical,
            FrameBorder = spec.FrameBorder,
            Background = spec.Background.ToString().ToLowerInvariant(),
            BackgroundColor = spec.BackgroundColor,
            BackgroundLineColor = spec.BackgroundLineColor,
            BackgroundLineSpacing = spec.BackgroundLineSpacingMm,
            Author = spec.Author,
            Dynasty = spec.Dynasty,
            GroupByWord = spec.GroupByWord,
            ShowPinyin = spec.ShowPinyin,
            PinyinOnly = spec.PinyinOnly,
            HollowGlyph = spec.HollowGlyph,
            TraceIntensity = spec.TraceIntensity.ToString().ToLowerInvariant(),
            TraceColor = spec.TraceColor,
            GridColor = spec.GridColor,
            TextColor = spec.TextColor,
            FontFamily = spec.FontFamilyName,
            PageSize = PageSizeName(spec.Page),
            PageMargin = spec.Page.MarginTopMm,
            Text = spec.Text
        };

        var module = new ModuleDefinition
        {
            Id = $"user-{Guid.NewGuid():N}",
            Name = templateName,
            Description = "用户保存的字帖模板",
            Kind = "customText",
            Enabled = true,
            Defaults = defaults
        };
        YamlResourceSerializer.SerializeFile(path, module);
    }

    private static string PageSizeName(PageSettings page)
    {
        if (Math.Abs(page.WidthMm - 297) < 0.1 && Math.Abs(page.HeightMm - 210) < 0.1)
            return "a4Landscape";
        if (Math.Abs(page.WidthMm - 297) < 0.1 && Math.Abs(page.HeightMm - 420) < 0.1)
            return "a3Portrait";
        if (Math.Abs(page.WidthMm - 216) < 0.1 && Math.Abs(page.HeightMm - 279) < 0.1)
            return "letter";
        return "a4Portrait";
    }
}
