using System.Text.Json;
using System.IO.Compression;
using System.Text;
using Zitie.Core.Models;

namespace Zitie.Desktop.Services;

/// <summary>可移植的字帖文档格式：保存编辑器规格，不保存运行时拼音缓存。</summary>
public sealed record SheetDocument
{
    public int Version { get; init; } = 1;

    public string? ModuleId { get; init; }

    public CharacterSheetSpec Spec { get; init; } = new();
}

/// <summary>字帖文档使用 JSON，用户模板导出为 .zi 压缩包（内含 module.yml 与素材）。</summary>
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

        if (!string.Equals(Path.GetExtension(path), ".zi", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("字帖模板只能保存为 .zi 模板包。");

        var hasArtwork = !string.IsNullOrWhiteSpace(spec.BackgroundArtworkSvg);
        var module = CreateTemplateModule(spec, name, Path.GetFileNameWithoutExtension(path), hasArtwork);

        if (File.Exists(path)) File.Delete(path);
        using var stream = File.Create(path);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create);
        WriteEntryText(archive, "module.yml", YamlResourceSerializer.Serialize(module));
        if (hasArtwork)
            WriteEntryText(archive, "assets/background.svg", spec.BackgroundArtworkSvg!);
    }

    private static ModuleDefinition CreateTemplateModule(
        CharacterSheetSpec spec,
        string? name,
        string fallbackName,
        bool hasArtwork)
    {
        var templateName = string.IsNullOrWhiteSpace(name)
            ? fallbackName
            : name.Trim();
        var defaults = new ModuleDefaults
        {
            Grid = spec.Grid.ToString().ToLowerInvariant(),
            Mode = spec.Mode.ToString().ToLowerInvariant(),
            Repeats = spec.RepeatsPerChar,
            TraceCount = spec.TraceSlotCount,
            GridSize = spec.GridSizeMm,
            GridGap = spec.GridGapMm,
            GroupGap = spec.GroupGapMm,
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
            BackgroundArtwork = hasArtwork ? "assets/background.svg" : null,
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
            PageMarginTop = spec.Page.MarginTopMm,
            PageMarginBottom = spec.Page.MarginBottomMm,
            PageMarginLeft = spec.Page.MarginLeftMm,
            PageMarginRight = spec.Page.MarginRightMm,
            Text = spec.Text
        };

        return new ModuleDefinition
        {
            Id = $"user-{Guid.NewGuid():N}",
            Name = templateName,
            Description = "用户保存的字帖模板",
            Kind = "customText",
            Enabled = true,
            Defaults = defaults
        };
    }

    private static void WriteEntryText(ZipArchive archive, string path, string text)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, Encoding.UTF8);
        writer.Write(text);
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
