using System.Text.Json;
using Zitie.Core.Models;

namespace Zitie.Desktop.Services;

/// <summary>离线汉字笔顺库，数据来自 hanzi-writer-data 的 strokes 字段。</summary>
public sealed class StrokeOrderCatalog
{
    private const string FileName = "hanzi-writer-strokes.jsonl";
    private readonly Lazy<IReadOnlyDictionary<string, CharacterStrokeOrder>> _orders;

    public StrokeOrderCatalog()
    {
        _orders = new Lazy<IReadOnlyDictionary<string, CharacterStrokeOrder>>(
            Load,
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public IReadOnlyDictionary<string, CharacterStrokeOrder> StrokeOrderByGlyph => _orders.Value;

    public IReadOnlyDictionary<string, CharacterStrokeOrder> FindForGlyphs(IEnumerable<string> glyphs)
    {
        var requested = glyphs
            .Where(static glyph => !string.IsNullOrWhiteSpace(glyph))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (requested.Length == 0) return new Dictionary<string, CharacterStrokeOrder>();

        var orders = StrokeOrderByGlyph;
        var result = new Dictionary<string, CharacterStrokeOrder>(requested.Length);
        foreach (var glyph in requested)
            if (orders.TryGetValue(glyph, out var order))
                result[glyph] = order;

        return result;
    }

    private static IReadOnlyDictionary<string, CharacterStrokeOrder> Load()
    {
        var path = Path.Combine(ResourcePaths.Strokes, FileName);
        var map = new Dictionary<string, CharacterStrokeOrder>(StringComparer.Ordinal);
        if (!File.Exists(path))
        {
            ZitieLogging.Warn($"笔顺库不存在，已跳过：{path}");
            return map;
        }

        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                using var document = JsonDocument.Parse(line);
                var root = document.RootElement;
                if (!root.TryGetProperty("glyph", out var glyphElement) ||
                    !root.TryGetProperty("strokes", out var strokesElement) ||
                    glyphElement.ValueKind != JsonValueKind.String ||
                    strokesElement.ValueKind != JsonValueKind.Array)
                    continue;

                var glyph = glyphElement.GetString();
                if (string.IsNullOrWhiteSpace(glyph)) continue;

                var strokes = new List<string>();
                foreach (var strokeElement in strokesElement.EnumerateArray())
                    if (strokeElement.ValueKind == JsonValueKind.String &&
                        strokeElement.GetString() is { Length: > 0 } stroke)
                        strokes.Add(stroke);

                if (strokes.Count > 0)
                    map[glyph] = new CharacterStrokeOrder(strokes);
            }
            catch (JsonException exception)
            {
                ZitieLogging.Warn("笔顺库行解析失败，已跳过。", exception);
            }
        }

        ZitieLogging.Info($"笔顺库加载完成：{map.Count} 字（{path}）");
        return map;
    }
}
