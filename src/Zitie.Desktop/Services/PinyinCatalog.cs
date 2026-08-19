using System.IO;
using System.Text.Json;

namespace Zitie.Desktop.Services;

/// <summary>拼音词条。</summary>
public sealed record PinyinWord
{
    public string Word { get; init; } = string.Empty;

    public string Pinyin { get; init; } = string.Empty;
}

/// <summary>拼音分类词表（pinyin/words.json）。</summary>
public sealed record PinyinCategory
{
    public string Category { get; init; } = string.Empty;

    public IReadOnlyList<PinyinWord> Words { get; init; } = Array.Empty<PinyinWord>();
}

/// <summary>
///     扫描输出目录 pinyin/*.json 加载词表，并提供 字符 → 拼音 映射（“看拼音写词语”用）。
/// </summary>
public sealed class PinyinCatalog
{
    public PinyinCatalog()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "pinyin");
        var categories = new List<PinyinCategory>();
        var map = new Dictionary<string, string>();

        if (Directory.Exists(directory))
            foreach (var file in Directory.EnumerateFiles(directory, "*.json"))
                try
                {
                    var items = JsonSerializer.Deserialize(
                        File.ReadAllText(file), ZitieJsonContext.Default.ListPinyinCategory);
                    if (items is null) continue;
                    foreach (var category in items)
                    {
                        var valid = new List<PinyinWord>();
                        foreach (var word in category.Words)
                            if (!string.IsNullOrWhiteSpace(word.Word) && !string.IsNullOrWhiteSpace(word.Pinyin))
                            {
                                valid.Add(word);
                                foreach (var ch in word.Word)
                                    if (!char.IsWhiteSpace(ch) && !map.ContainsKey(ch.ToString()))
                                        map[ch.ToString()] = SplitSyllable(word.Pinyin, word.Word, ch);
                            }

                        if (valid.Count == 0) continue;
                        categories.Add(category with { Words = valid });
                    }
                }
                catch (Exception exception)
                {
                    ZitieLogging.Warn($"拼音词表解析失败，已跳过：{file}", exception);
                }

        Categories = categories;
        PinyinByGlyph = map;
        ZitieLogging.Info($"拼音词表加载完成：{categories.Sum(c => c.Words.Count)} 词 / {map.Count} 字（{directory}）");
    }

    public IReadOnlyList<PinyinCategory> Categories { get; }

    /// <summary>字符 → 带声调拼音；多音字取首次出现。</summary>
    public IReadOnlyDictionary<string, string> PinyinByGlyph { get; }

    private static string SplitSyllable(string pinyin, string word, char ch)
    {
        // pinyin 为空格分隔的音节序列；对多字词按字符顺序取对应音节
        var syllables = pinyin.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var index = word.IndexOf(ch);
        return index >= 0 && index < syllables.Length ? syllables[index] : pinyin;
    }
}
