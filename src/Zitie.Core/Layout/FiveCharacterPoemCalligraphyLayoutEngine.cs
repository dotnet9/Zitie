using System.Globalization;
using Zitie.Core.Models;

namespace Zitie.Core.Layout;

public sealed class FiveCharacterPoemCalligraphyLayoutEngine : IPracticeLayoutEngine
{
    private const int Capacity = 20;

    public PracticeLayoutKind Kind => PracticeLayoutKind.FiveCharacterPoemCalligraphy;

    public IReadOnlyList<SheetPage> Paginate(CharacterSheetSpec spec)
    {
        var glyphs = ExtractPoemGlyphs(spec.Text).ToArray();
        var pages = new List<SheetPage>();
        var pageIndex = 0;
        var tokenIndex = 0;

        do
        {
            var cells = new List<CellSlot>(Capacity);
            for (var slot = 0; slot < Capacity; slot++)
            {
                var glyph = tokenIndex < glyphs.Length ? glyphs[tokenIndex] : string.Empty;
                cells.Add(new CellSlot(
                    spec.Page.MarginLeftMm,
                    spec.Page.MarginTopMm,
                    1,
                    glyph,
                    string.IsNullOrWhiteSpace(glyph) ? CellRole.Blank : CellRole.Model,
                    string.IsNullOrWhiteSpace(glyph) ? -1 : tokenIndex));
                tokenIndex++;
            }

            pages.Add(new SheetPage(pageIndex, 5, 4, pageIndex == 0, cells));
            pageIndex++;
        } while (tokenIndex < glyphs.Length);

        return pages;
    }

    private static IEnumerable<string> ExtractPoemGlyphs(string text)
    {
        var enumerator = StringInfo.GetTextElementEnumerator(text);
        while (enumerator.MoveNext())
        {
            var element = enumerator.GetTextElement();
            if (string.IsNullOrWhiteSpace(element)) continue;
            if (element.Length > 0 && IsPoemGlyph(element[0]))
                yield return element;
        }
    }

    private static bool IsPoemGlyph(char value)
    {
        return value is >= '\u3400' and <= '\u9FFF' ||
               value is >= '\uF900' and <= '\uFAFF';
    }
}
