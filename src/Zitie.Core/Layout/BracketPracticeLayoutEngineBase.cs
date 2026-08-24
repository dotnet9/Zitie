using Zitie.Core.Models;

namespace Zitie.Core.Layout;

public abstract class BracketPracticeLayoutEngineBase : IPracticeLayoutEngine
{
    public abstract PracticeLayoutKind Kind { get; }

    protected abstract int DefaultColumns { get; }

    protected abstract double ItemHeightMm { get; }

    protected abstract double RowPitchMm { get; }

    protected virtual double FirstPageHeaderHeightMm => 33;

    public IReadOnlyList<SheetPage> Paginate(CharacterSheetSpec spec)
    {
        var tokens = PracticeLayoutText.Tokenize(spec.Text);
        var pages = new List<SheetPage>();
        var tokenIndex = 0;
        var pageIndex = 0;

        do
        {
            var hasHeader = pageIndex == 0;
            var metrics = ResolveMetrics(spec, hasHeader);
            var capacity = metrics.Columns * metrics.Rows;
            var cells = new List<CellSlot>(capacity);

            for (var slot = 0; slot < capacity; slot++)
            {
                var token = tokenIndex < tokens.Count ? tokens[tokenIndex] : string.Empty;
                var column = metrics.Columns <= 1 ? 0 : slot % metrics.Columns;
                var row = metrics.Columns <= 1 ? slot : slot / metrics.Columns;
                cells.Add(new CellSlot(
                    metrics.LeftMm + column * metrics.ColumnPitchMm,
                    metrics.TopMm + row * metrics.RowPitchMm,
                    metrics.ItemHeightMm,
                    token,
                    string.IsNullOrEmpty(token) ? CellRole.Blank : CellRole.Model,
                    string.IsNullOrEmpty(token) ? -1 : tokenIndex));
                tokenIndex++;
            }

            pages.Add(new SheetPage(pageIndex, metrics.Columns, metrics.Rows, hasHeader, cells));
            pageIndex++;
        } while (tokenIndex < tokens.Count);

        return pages;
    }

    private PracticeLayoutMetrics ResolveMetrics(CharacterSheetSpec spec, bool hasHeader)
    {
        var contentTop = spec.Page.MarginTopMm + (hasHeader ? FirstPageHeaderHeightMm : 0);
        var availableHeight = Math.Max(1, spec.Page.HeightMm - contentTop - spec.Page.MarginBottomMm);
        var availableWidth = Math.Max(1, spec.Page.UsableWidthMm);
        var columns = Math.Clamp(spec.LayoutColumns > 0 ? spec.LayoutColumns : DefaultColumns, 1, 12);
        var rows = spec.LayoutRows > 0
            ? Math.Clamp(spec.LayoutRows, 1, 128)
            : CountThatFits(availableHeight, ItemHeightMm, RowPitchMm);

        return new PracticeLayoutMetrics(
            columns,
            rows,
            spec.Page.MarginLeftMm,
            contentTop,
            availableWidth / columns,
            RowPitchMm,
            ItemHeightMm);
    }

    private static int CountThatFits(double availableMm, double itemSizeMm, double pitchMm)
    {
        if (availableMm <= 0 || itemSizeMm <= 0 || pitchMm <= 0) return 1;
        if (availableMm <= itemSizeMm) return 1;

        return Math.Max(1, (int)Math.Floor((availableMm - itemSizeMm) / pitchMm) + 1);
    }

    private sealed record PracticeLayoutMetrics(
        int Columns,
        int Rows,
        double LeftMm,
        double TopMm,
        double ColumnPitchMm,
        double RowPitchMm,
        double ItemHeightMm);
}
