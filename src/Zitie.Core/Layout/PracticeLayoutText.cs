using System.Globalization;

namespace Zitie.Core.Layout;

internal static class PracticeLayoutText
{
    private static readonly char[] SentencePunctuation =
    {
        '，', '。', '；', '、', '！', '？', '：', '“', '”', '‘', '’', ',', '.', ';', '!', '?', ':'
    };

    public static IReadOnlyList<string> Tokenize(string text)
    {
        var tokens = text
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(static token => token.Length > 0)
            .ToArray();
        if (tokens.Length > 1) return tokens;

        var result = new List<string>();
        var source = tokens.Length == 1 ? tokens[0] : text;
        var enumerator = StringInfo.GetTextElementEnumerator(source);
        while (enumerator.MoveNext())
        {
            var element = enumerator.GetTextElement();
            if (!string.IsNullOrWhiteSpace(element) &&
                (element.Length == 0 || !SentencePunctuation.Contains(element[0])))
                result.Add(element);
        }

        return result;
    }
}
