using JiebaNet.Segmenter;

namespace Zitie.Desktop.Services;

public static class ModuleSearch
{
    private static readonly Lazy<JiebaSegmenter> Segmenter = new(CreateSegmenter);

    public static bool Matches(ModuleDefinition module, string? query)
    {
        var keyword = query?.Trim();
        if (string.IsNullOrWhiteSpace(keyword)) return true;

        var terms = ParseTerms(query);
        if (terms.Count == 0) return true;

        var searchableText = terms.Count > 1
            ? PrimarySearchableText(module)
            : SearchableText(module);
        if (searchableText.Contains(keyword, StringComparison.CurrentCultureIgnoreCase))
            return true;

        return terms.All(term => searchableText.Contains(term, StringComparison.CurrentCultureIgnoreCase));
    }

    public static IReadOnlyList<string> ParseTerms(string? query)
    {
        var keyword = query?.Trim();
        if (string.IsNullOrWhiteSpace(keyword)) return Array.Empty<string>();

        return SplitQuery(keyword)
            .SelectMany(Segment)
            .Select(static term => term.Trim())
            .Where(IsSearchTerm)
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    private static JiebaSegmenter CreateSegmenter()
    {
        // Jieba 内部使用默认反序列化器，显式保留模型字典的原生转换器，避免 AOT 退回整句搜索。
        _ = JiebaModelJsonContext.Default.GetTypeInfo(typeof(IDictionary<char, IDictionary<char, double>>));
        var direct = Path.Combine(AppContext.BaseDirectory, "Resources");
        var bundled = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "Resources", "Resources"));
        ConfigManager.ConfigFileBaseDir = Directory.Exists(direct) ? direct : bundled;
        return new JiebaSegmenter();
    }

    private static IEnumerable<string> Segment(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) yield break;

        IEnumerable<string> terms;
        try
        {
            terms = Segmenter.Value.CutForSearch(text, hmm: true).ToArray();
        }
        catch (Exception exception)
        {
            ZitieLogging.Warn($"中文分词失败，已退回原始关键词：{text}", exception);
            terms = [text];
        }

        foreach (var term in terms)
            yield return term;
    }

    private static IEnumerable<string> SplitQuery(string text)
    {
        var start = -1;
        for (var i = 0; i < text.Length; i++)
        {
            if (IsQuerySeparator(text[i]))
            {
                if (start >= 0)
                    yield return text[start..i];
                start = -1;
            }
            else if (start < 0)
            {
                start = i;
            }
        }

        if (start >= 0)
            yield return text[start..];
    }

    private static bool IsQuerySeparator(char value)
    {
        return char.IsWhiteSpace(value) ||
               char.IsPunctuation(value) ||
               char.IsSeparator(value) ||
               char.IsSymbol(value);
    }

    private static bool IsSearchTerm(string term)
    {
        return term.Length > 0 && term.Any(char.IsLetterOrDigit);
    }

    private static string SearchableText(ModuleDefinition module)
    {
        return string.Join(' ',
            PrimarySearchableText(module),
            module.Description,
            string.Join(' ', module.Categories));
    }

    private static string PrimarySearchableText(ModuleDefinition module)
    {
        return string.Join(' ',
            module.Id,
            module.SourceTemplateId,
            module.Name,
            Path.GetFileNameWithoutExtension(module.PreviewImagePath));
    }
}
