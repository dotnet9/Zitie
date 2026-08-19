using System.Globalization;
using Zitie.Core.Models;

namespace Zitie.Core.Layout;

/// <summary>
///     排版引擎：把 <see cref="CharacterSheetSpec" /> 分页为带毫米坐标的格子序列。
/// </summary>
public static class LayoutEngine
{
    /// <summary>毫米转 PDF 点（1 inch = 25.4mm = 72pt）。</summary>
    public const double MmToPt = 72.0 / 25.4;

    private const double TitleLineMm = 12;
    private const double AuthorLineMm = 9;
    private const double FieldsLineMm = 10;
    private const double FooterLineMm = 10;

    private static readonly char[] SentencePunctuation =
    {
        '，', '。', '；', '、', '！', '？', '：', '“', '”', '‘', '’', ',', '.', ';', '!', '?', ':'
    };

    /// <summary>
    ///     按练习模式展开一个字符的格子角色序列：范字在前，描红居中，空格收尾。
    /// </summary>
    public static IReadOnlyList<CellRole> BuildSlotRoles(PracticeMode mode, int repeatsPerChar, int traceSlotCount)
    {
        repeatsPerChar = Math.Max(1, repeatsPerChar);
        traceSlotCount = Math.Clamp(traceSlotCount, 0, repeatsPerChar - 1);

        var roles = new List<CellRole>(repeatsPerChar) { CellRole.Model };
        var traceCount = mode == PracticeMode.Trace ? traceSlotCount : 0;
        for (var i = 0; i < traceCount; i++) roles.Add(CellRole.Trace);
        while (roles.Count < repeatsPerChar) roles.Add(CellRole.Blank);
        return roles;
    }

    public static IReadOnlyList<SheetPage> Paginate(CharacterSheetSpec spec)
    {
        var pages = new List<SheetPage>();
        var vertical = spec.Orientation == SheetOrientation.Vertical;
        var pinyinGrid = spec.Grid == GridKind.Pinyin;
        var groups = pinyinGrid
            ? EnumeratePinyinGroups(spec.Text)
            : vertical
                ? EnumerateSentenceGlyphGroups(spec.Text)
                : spec.GroupByWord
                    ? EnumerateWordGlyphGroups(spec.Text)
                    : EnumerateGlyphGroups(spec.Text);
        if (groups.Count == 0) return pages;

        var roles = pinyinGrid
            ? new[] { CellRole.Model }
            : BuildSlotRoles(spec.Mode, spec.RepeatsPerChar, spec.TraceSlotCount);
        // 词/句模式：每个字符一格，角色统一；看拼音写词语时隐藏范字只留空格
        var wordRole = spec.PinyinOnly || spec.Mode == PracticeMode.Copy
            ? CellRole.Blank
            : CellRole.Trace;
        var gridSizeMm = ResolveGridSizeMm(spec, vertical);
        var pitch = gridSizeMm + Math.Max(0, spec.GridGapMm);
        var columns = ResolveColumnCount(spec, vertical, pitch);
        var blankLineCount = vertical ? 0 : Math.Clamp(spec.BlankLineCount, 0, 10);
        var firstPageRows = Math.Max(1,
            (int)Math.Floor((spec.Page.UsableHeightMm - HeaderHeightMm(spec) - FooterLineMm) / pitch));
        var otherPageRows = Math.Max(1,
            (int)Math.Floor((spec.Page.UsableHeightMm - FooterLineMm) / pitch));

        var pageIndex = 0;
        var groupIndex = 0;
        while (groupIndex < groups.Count)
        {
            var hasHeader = pageIndex == 0;
            var rows = hasHeader ? firstPageRows : otherPageRows;
            var capacity = columns * rows;
            var contentTop = spec.Page.MarginTopMm + (hasHeader ? HeaderHeightMm(spec) : 0);
            var cells = new List<CellSlot>();
            var cursor = 0;

            // 每个组（一个字、一个词、一句话或一个音节）占据连续格位，放不下时整组顺延到下一页。
            // 字符模式一组是 roles.Count 格（范字+描红+空格），词/句/音节模式一组是组内字符数。
            while (groupIndex < groups.Count)
            {
                var glyphs = groups[groupIndex];
                var groupSize = pinyinGrid
                    ? 1
                    : vertical || spec.GroupByWord
                        ? glyphs.Count
                        : roles.Count;

                // 竖排：句子必须完整放在一列内；当前列剩余空间不足时对齐到下一列首
                if (vertical)
                {
                    var columnSpaceLeft = rows - cursor % rows;
                    if (groupSize > columnSpaceLeft)
                    {
                        cursor = (cursor / rows + 1) * rows;
                        if (cursor + groupSize > capacity) break;
                    }
                }
                else
                {
                    cursor = AlignHorizontalCursor(cursor, groupSize, columns, blankLineCount);
                    if (cursor + groupSize > capacity) break;
                }

                if (!vertical && cursor + groupSize > capacity)
                {
                    break;
                }

                for (var slot = 0; slot < groupSize; slot++)
                {
                    var absolute = cursor + slot;
                    var glyph = pinyinGrid
                        ? glyphs[0]
                        : vertical || spec.GroupByWord
                            ? glyphs[slot]
                            : glyphs[0];
                    var role = pinyinGrid
                        ? roles[0]
                        : vertical || spec.GroupByWord
                            ? wordRole
                            : roles[slot];
                    var (x, y) = CellPosition(spec, vertical, absolute, columns, rows, contentTop, pitch);
                    cells.Add(new CellSlot(
                        x, y,
                        gridSizeMm,
                        glyph,
                        role,
                        groupIndex));
                }

                cursor = vertical
                    ? cursor + groupSize
                    : AdvanceCursor(cursor, groupSize, columns, blankLineCount);
                groupIndex++;
            }

            if (cells.Count == 0)
                // 单组超过整页容量（参数极端），放弃排版避免死循环
                break;

            pages.Add(new SheetPage(pageIndex, columns, rows, hasHeader, cells));
            pageIndex++;
        }

        return pages;
    }

    private static double ResolveGridSizeMm(CharacterSheetSpec spec, bool vertical)
    {
        if (vertical || spec.CharactersPerLine <= 0)
            return Math.Max(1, spec.GridSizeMm);

        var columns = Math.Clamp(spec.CharactersPerLine, 1, 64);
        var gap = Math.Max(0, spec.GridGapMm);
        var available = spec.Page.UsableWidthMm - gap * (columns - 1);
        return available > columns
            ? available / columns
            : Math.Max(1, spec.GridSizeMm);
    }

    private static int ResolveColumnCount(CharacterSheetSpec spec, bool vertical, double pitch)
    {
        if (!vertical && spec.CharactersPerLine > 0)
            return Math.Clamp(spec.CharactersPerLine, 1, 64);

        return Math.Max(1, (int)Math.Floor(spec.Page.UsableWidthMm / pitch));
    }

    private static int AdvanceCursor(int cursor, int groupSize, int columns, int blankLineCount)
    {
        var next = cursor + groupSize;
        if (blankLineCount <= 0 || columns <= 0 || next % columns != 0) return next;

        return next + columns * blankLineCount;
    }

    private static int AlignHorizontalCursor(int cursor, int groupSize, int columns, int blankLineCount)
    {
        if (columns <= 0 || groupSize > columns) return cursor;

        var column = cursor % columns;
        if (column == 0 || column + groupSize <= columns) return cursor;

        return NextContentRowCursor(cursor, columns, blankLineCount);
    }

    private static int NextContentRowCursor(int cursor, int columns, int blankLineCount)
    {
        var row = cursor / columns;
        return (row + 1 + Math.Max(0, blankLineCount)) * columns;
    }

    /// <summary>
    ///     把游标格位换算为页面坐标。
    ///     横排：行优先，先填满一行再换行；竖排：列从右到左、每列自上而下（传统帖式）。
    /// </summary>
    private static (double X, double Y) CellPosition(
        CharacterSheetSpec spec,
        bool vertical,
        int absolute,
        int columns,
        int rows,
        double contentTop,
        double pitch)
    {
        if (vertical)
        {
            var column = columns - 1 - absolute / rows;
            var row = absolute % rows;
            return (spec.Page.MarginLeftMm + column * pitch, contentTop + row * pitch);
        }

        return (spec.Page.MarginLeftMm + absolute % columns * pitch, contentTop + absolute / columns * pitch);
    }

    public static double HeaderHeightMm(CharacterSheetSpec spec)
    {
        var height = 0.0;
        if (spec.Title is not null) height += TitleLineMm;
        if (spec.ShowPoemHeader) height += AuthorLineMm;
        if (spec.ShowHeaderFields) height += FieldsLineMm;
        return height;
    }

    private static IReadOnlyList<IReadOnlyList<string>> EnumerateGlyphGroups(string text)
    {
        var groups = new List<IReadOnlyList<string>>();
        var enumerator = StringInfo.GetTextElementEnumerator(text);
        while (enumerator.MoveNext())
        {
            var element = enumerator.GetTextElement();
            if (!string.IsNullOrWhiteSpace(element)) groups.Add(new[] { element });
        }

        return groups;
    }

    /// <summary>按空白分词，每词内的文本元素（字/字母）为一组；标点附着在前一个词上。</summary>
    private static IReadOnlyList<IReadOnlyList<string>> EnumerateWordGlyphGroups(string text)
    {
        var groups = new List<IReadOnlyList<string>>();
        foreach (var word in text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            var glyphs = new List<string>();
            var enumerator = StringInfo.GetTextElementEnumerator(word);
            while (enumerator.MoveNext())
            {
                var element = enumerator.GetTextElement();
                if (!string.IsNullOrWhiteSpace(element)) glyphs.Add(element);
            }

            if (glyphs.Count > 0) groups.Add(glyphs);
        }

        return groups;
    }

    /// <summary>拼音四线格：按空白分音节，每个音节整体占据一个格子（如 chūn、tiān）。</summary>
    private static IReadOnlyList<IReadOnlyList<string>> EnumeratePinyinGroups(string text)
    {
        var groups = new List<IReadOnlyList<string>>();
        foreach (var syllable in text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            groups.Add(new[] { syllable });

        return groups;
    }

    /// <summary>按标点切句（竖排帖式：一句一列，句末标点保留在句内）。</summary>
    private static IReadOnlyList<IReadOnlyList<string>> EnumerateSentenceGlyphGroups(string text)
    {
        var groups = new List<IReadOnlyList<string>>();
        var current = new List<string>();
        var enumerator = StringInfo.GetTextElementEnumerator(text);
        while (enumerator.MoveNext())
        {
            var element = enumerator.GetTextElement();
            if (string.IsNullOrWhiteSpace(element)) continue;

            current.Add(element);
            if (element.Length > 0 && SentencePunctuation.Contains(element[0]))
            {
                groups.Add(current);
                current = new List<string>();
            }
        }

        if (current.Count > 0) groups.Add(current);
        return groups;
    }
}
