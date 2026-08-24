using Zitie.Core.Models;

namespace Zitie.Core.Layout;

public sealed class CharacterWordsPoemLayoutEngine : IPracticeLayoutEngine
{
    private const int Capacity = 21;

    public PracticeLayoutKind Kind => PracticeLayoutKind.CharacterWordsPoem;

    public IReadOnlyList<SheetPage> Paginate(CharacterSheetSpec spec)
    {
        var tokens = PracticeLayoutText.Tokenize(spec.Text);
        var pages = new List<SheetPage>();
        var pageIndex = 0;
        var tokenIndex = 0;

        do
        {
            var cells = new List<CellSlot>(Capacity);
            for (var slot = 0; slot < Capacity; slot++)
            {
                var token = tokenIndex < tokens.Count ? tokens[tokenIndex] : string.Empty;
                cells.Add(new CellSlot(
                    spec.Page.MarginLeftMm,
                    spec.Page.MarginTopMm,
                    1,
                    token,
                    string.IsNullOrWhiteSpace(token) ? CellRole.Blank : CellRole.Model,
                    string.IsNullOrWhiteSpace(token) ? -1 : tokenIndex));
                tokenIndex++;
            }

            pages.Add(new SheetPage(pageIndex, 1, Capacity, pageIndex == 0, cells));
            pageIndex++;
        } while (tokenIndex < tokens.Count);

        return pages;
    }
}
