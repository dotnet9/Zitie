using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Zitie.Core.Models;

namespace Zitie.Desktop.Services;

/// <summary>可移植的字帖文档格式：保存编辑器规格，不保存运行时拼音缓存。</summary>
public sealed record SheetDocument
{
    public int Version { get; init; } = 1;

    public string? ModuleId { get; init; }

    public CharacterSheetSpec Spec { get; init; } = new();
}

/// <summary>字帖文档与用户模板的 JSON 读写。</summary>
public static class SheetDocumentStore
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.General)
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    static SheetDocumentStore()
    {
        Options.Converters.Add(new JsonStringEnumConverter<GridKind>(JsonNamingPolicy.CamelCase));
        Options.Converters.Add(new JsonStringEnumConverter<PracticeMode>(JsonNamingPolicy.CamelCase));
        Options.Converters.Add(new JsonStringEnumConverter<SheetHeaderPreset>(JsonNamingPolicy.CamelCase));
        Options.Converters.Add(new JsonStringEnumConverter<SheetBackground>(JsonNamingPolicy.CamelCase));
        Options.Converters.Add(new JsonStringEnumConverter<TraceIntensity>(JsonNamingPolicy.CamelCase));
        Options.Converters.Add(new JsonStringEnumConverter<SheetOrientation>(JsonNamingPolicy.CamelCase));
    }

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
        var document = JsonSerializer.Deserialize(File.ReadAllText(path), ZitieJsonContext.Default.SheetDocument);
        if (document is null || document.Spec is null)
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
        var defaults = new Dictionary<string, object?>
        {
            ["grid"] = spec.Grid.ToString().ToLowerInvariant(),
            ["mode"] = spec.Mode.ToString().ToLowerInvariant(),
            ["repeats"] = spec.RepeatsPerChar,
            ["traceCount"] = spec.TraceSlotCount,
            ["gridSize"] = spec.GridSizeMm,
            ["gridGap"] = spec.GridGapMm,
            ["title"] = spec.Title,
            ["headerPreset"] = spec.HeaderPreset.ToString().ToLowerInvariant(),
            ["headerText"] = spec.HeaderTextTemplate,
            ["charactersPerLine"] = spec.CharactersPerLine,
            ["blankLineCount"] = spec.BlankLineCount,
            ["vertical"] = spec.Orientation == SheetOrientation.Vertical,
            ["frameBorder"] = spec.FrameBorder,
            ["background"] = spec.Background.ToString().ToLowerInvariant(),
            ["backgroundColor"] = spec.BackgroundColor,
            ["backgroundLineColor"] = spec.BackgroundLineColor,
            ["backgroundLineSpacing"] = spec.BackgroundLineSpacingMm,
            ["author"] = spec.Author,
            ["dynasty"] = spec.Dynasty,
            ["groupByWord"] = spec.GroupByWord,
            ["showPinyin"] = spec.ShowPinyin,
            ["pinyinOnly"] = spec.PinyinOnly,
            ["hollowGlyph"] = spec.HollowGlyph,
            ["traceIntensity"] = spec.TraceIntensity.ToString().ToLowerInvariant(),
            ["traceColor"] = spec.TraceColor,
            ["gridColor"] = spec.GridColor,
            ["textColor"] = spec.TextColor,
            ["fontFamily"] = spec.FontFamilyName,
            ["pageSize"] = PageSizeName(spec.Page),
            ["pageMargin"] = spec.Page.MarginTopMm,
            ["text"] = spec.Text
        };

        var defaultsNode = new JsonObject();
        foreach (var (key, value) in defaults)
            defaultsNode[key] = value switch
            {
                null => null,
                string text => JsonValue.Create(text),
                int number => JsonValue.Create(number),
                double number => JsonValue.Create(number),
                bool flag => JsonValue.Create(flag),
                _ => JsonValue.Create(value.ToString())
            };

        var template = new JsonObject
        {
            ["id"] = $"user-{Guid.NewGuid():N}",
            ["name"] = templateName,
            ["description"] = "用户保存的字帖模板",
            ["kind"] = "customText",
            ["enabled"] = true,
            ["defaults"] = defaultsNode
        };
        File.WriteAllText(path, template.ToJsonString(Options));
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
