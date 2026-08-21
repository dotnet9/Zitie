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
        var blankContentLayout = spec.BlankContentLayout;
        var groups = blankContentLayout
            ? Array.Empty<IReadOnlyList<string>>()
            : pinyinGrid
            ? EnumeratePinyinGroups(spec)
            : vertical
                ? EnumerateSentenceGlyphGroups(spec.Text)
                : spec.GroupByWord
                    ? EnumerateWordGlyphGroups(spec.Text)
                    : EnumerateGlyphGroups(spec.Text);

        var roles = pinyinGrid
            ? new[] { CellRole.Model }
            : BuildSlotRoles(spec.Mode, spec.RepeatsPerChar, spec.TraceSlotCount);
        var repeatedCharacterMode = !pinyinGrid && !vertical && !spec.GroupByWord;
        var repeatedGroupSize = repeatedCharacterMode ? roles.Count : 0;
        // 词/句模式：每个字符一格，角色统一；看拼音写词语时隐藏范字只留空格
        var wordRole = spec.PinyinOnly || spec.Mode == PracticeMode.Copy
            ? CellRole.Blank
            : CellRole.Trace;
        var groupGapMm = Math.Clamp(spec.GroupGapMm, 0, 10);
        var gridSizeMm = ResolveGridSizeMm(spec, vertical, repeatedGroupSize, groupGapMm);
        var cellPitch = gridSizeMm + Math.Max(0, spec.GridGapMm);
        var rowPitch = vertical ? cellPitch : cellPitch + groupGapMm;
        var columnPitch = vertical ? cellPitch + groupGapMm : cellPitch;
        var columns = ResolveColumnCount(spec, vertical, repeatedGroupSize, gridSizeMm, cellPitch, columnPitch);
        var blankCellLineCount = Math.Clamp(spec.BlankCellLineCount, 0, 10);
        var firstPageRows = ResolveRowCount(
            spec,
            spec.Page.UsableHeightMm - HeaderHeightMm(spec) - FooterLineMm,
            gridSizeMm,
            rowPitch);
        var otherPageRows = ResolveRowCount(
            spec,
            spec.Page.UsableHeightMm - FooterLineMm,
            gridSizeMm,
            rowPitch);

        if (groups.Count == 0)
        {
            if (blankContentLayout)
            {
                pages.Add(spec.LayoutColumns <= 0 && spec.LayoutRows <= 0
                    ? new SheetPage(0, 0, 0, true, Array.Empty<CellSlot>())
                    : CreateBlankLayoutPage(
                        spec,
                        columns,
                        firstPageRows,
                        hasHeader: true,
                        gridSizeMm,
                        cellPitch,
                        rowPitch,
                        columnPitch,
                        groupGapMm,
                        repeatedGroupSize));
            }

            return pages;
        }

        // 一个词或句子理论上应保持连续，但不能因为它超过单页容量而静默丢失。
        // 极端长词/长句按页容量拆成连续分组，普通内容仍保持原有“不拆组”行为。
        if (!pinyinGrid && (vertical || spec.GroupByWord))
        {
            var smallestGroupCapacity = vertical
                ? Math.Max(1, Math.Min(firstPageRows, otherPageRows))
                : columns * Math.Max(1, Math.Min(firstPageRows, otherPageRows));
            groups = SplitOversizedGroups(groups, smallestGroupCapacity);
        }

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
                    var alignedCursor = AlignVerticalCursor(cursor, groupSize, rows, blankCellLineCount);
                    if (blankCellLineCount > 0 && alignedCursor > cursor)
                    {
                        var blankStart = (cursor / rows + 1) * rows;
                        AddBlankSlots(
                            cells,
                            spec,
                            vertical,
                            blankStart,
                            alignedCursor,
                            columns,
                            rows,
                            contentTop,
                            cellPitch,
                            rowPitch,
                            columnPitch,
                            groupGapMm,
                            repeatedGroupSize,
                            gridSizeMm);
                    }

                    cursor = alignedCursor;
                    if (cursor + groupSize > capacity) break;
                }
                else
                {
                    var alignedCursor = AlignHorizontalCursor(cursor, groupSize, columns, blankCellLineCount);
                    if (blankCellLineCount > 0 && alignedCursor > cursor)
                    {
                        var blankStart = (cursor / columns + 1) * columns;
                        AddBlankSlots(
                            cells,
                            spec,
                            vertical,
                            blankStart,
                            alignedCursor,
                            columns,
                            rows,
                            contentTop,
                            cellPitch,
                            rowPitch,
                            columnPitch,
                            groupGapMm,
                            repeatedGroupSize,
                            gridSizeMm);
                    }

                    cursor = alignedCursor;
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
                    var (x, y) = CellPosition(
                        spec,
                        vertical,
                        absolute,
                        columns,
                        rows,
                        contentTop,
                        cellPitch,
                        rowPitch,
                        columnPitch,
                        groupGapMm,
                        repeatedGroupSize);
                    cells.Add(new CellSlot(
                        x, y,
                        gridSizeMm,
                        glyph,
                        role,
                        groupIndex));
                }

                if (vertical)
                {
                    var contentEnd = cursor + groupSize;
                    var nextCursor = AdvanceVerticalCursor(cursor, groupSize, rows, blankCellLineCount);
                    if (nextCursor > contentEnd)
                    {
                        var blankStart = blankCellLineCount > 0
                            ? (cursor / rows + 1) * rows
                            : contentEnd;
                        AddBlankSlots(
                            cells,
                            spec,
                            vertical,
                            blankStart,
                            nextCursor,
                            columns,
                            rows,
                            contentTop,
                            cellPitch,
                            rowPitch,
                            columnPitch,
                            groupGapMm,
                            repeatedGroupSize,
                            gridSizeMm);
                    }

                    cursor = nextCursor;
                }
                else
                {
                    var contentEnd = cursor + groupSize;
                    var nextCursor = AdvanceHorizontalCursor(cursor, groupSize, columns, blankCellLineCount);
                    if (nextCursor > contentEnd)
                        AddBlankSlots(
                            cells,
                            spec,
                            vertical,
                            contentEnd,
                            nextCursor,
                            columns,
                            rows,
                            contentTop,
                            cellPitch,
                            rowPitch,
                            columnPitch,
                            groupGapMm,
                            repeatedGroupSize,
                            gridSizeMm);

                    cursor = nextCursor;
                }
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

    private static double ResolveGridSizeMm(
        CharacterSheetSpec spec,
        bool vertical,
        int repeatedGroupSize,
        double groupGapMm)
    {
        if (vertical || spec.CellsPerLine <= 0 && spec.LayoutColumns <= 0)
            return Math.Max(1, spec.GridSizeMm);

        var requestedColumns = spec.LayoutColumns > 0 ? spec.LayoutColumns : spec.CellsPerLine;
        var columns = EffectiveHorizontalColumns(Math.Clamp(requestedColumns, 1, 64), repeatedGroupSize);
        var gap = Math.Max(0, spec.GridGapMm);
        var groupGapCount = repeatedGroupSize > 0
            ? Math.Max(0, columns / repeatedGroupSize - 1)
            : 0;
        var available = spec.Page.UsableWidthMm - gap * (columns - 1) - groupGapMm * groupGapCount;
        return available > columns
            ? available / columns
            : Math.Max(1, spec.GridSizeMm);
    }

    private static int ResolveColumnCount(
        CharacterSheetSpec spec,
        bool vertical,
        int repeatedGroupSize,
        double gridSizeMm,
        double cellPitch,
        double columnPitch)
    {
        if (spec.LayoutColumns > 0)
            return EffectiveHorizontalColumns(Math.Clamp(spec.LayoutColumns, 1, 64), vertical ? 0 : repeatedGroupSize);

        if (!vertical && spec.CellsPerLine > 0)
            return EffectiveHorizontalColumns(Math.Clamp(spec.CellsPerLine, 1, 64), repeatedGroupSize);

        var count = CountThatFits(spec.Page.UsableWidthMm, gridSizeMm, columnPitch);
        return vertical
            ? Math.Max(1, count)
            : EffectiveHorizontalColumns(Math.Max(1, count), repeatedGroupSize);
    }

    private static int ResolveRowCount(
        CharacterSheetSpec spec,
        double availableHeightMm,
        double gridSizeMm,
        double rowPitch)
    {
        return spec.LayoutRows > 0
            ? Math.Clamp(spec.LayoutRows, 1, 128)
            : Math.Max(1, CountThatFits(availableHeightMm, gridSizeMm, rowPitch));
    }

    private static int CountThatFits(double availableMm, double itemSizeMm, double pitchMm)
    {
        if (availableMm <= 0 || itemSizeMm <= 0 || pitchMm <= 0) return 1;
        if (availableMm <= itemSizeMm) return 1;

        return Math.Max(1, (int)Math.Floor((availableMm - itemSizeMm) / pitchMm) + 1);
    }

    private static int EffectiveHorizontalColumns(int requestedColumns, int repeatedGroupSize)
    {
        requestedColumns = Math.Clamp(requestedColumns, 1, 64);
        if (repeatedGroupSize <= 1 || requestedColumns <= repeatedGroupSize)
            return requestedColumns;

        return Math.Max(repeatedGroupSize, requestedColumns / repeatedGroupSize * repeatedGroupSize);
    }

    private static int AdvanceHorizontalCursor(int cursor, int groupSize, int columns, int blankCellLineCount)
    {
        var next = cursor + groupSize;
        if (blankCellLineCount <= 0 || columns <= 0 || next % columns != 0) return next;

        return next + columns * blankCellLineCount;
    }

    private static int AdvanceVerticalCursor(int cursor, int groupSize, int rows, int blankCellLineCount)
    {
        var next = cursor + groupSize;
        if (blankCellLineCount <= 0 || rows <= 0) return next;

        var column = cursor / rows;
        return (column + 1 + blankCellLineCount) * rows;
    }

    private static int AlignHorizontalCursor(int cursor, int groupSize, int columns, int blankCellLineCount)
    {
        if (columns <= 0 || groupSize > columns) return cursor;

        var column = cursor % columns;
        if (column == 0 || column + groupSize <= columns) return cursor;

        return NextContentRowCursor(cursor, columns, blankCellLineCount);
    }

    private static int AlignVerticalCursor(int cursor, int groupSize, int rows, int blankCellLineCount)
    {
        if (rows <= 0 || groupSize > rows) return cursor;

        var row = cursor % rows;
        if (row == 0 || row + groupSize <= rows) return cursor;

        return NextContentColumnCursor(cursor, rows, blankCellLineCount);
    }

    private static int NextContentRowCursor(int cursor, int columns, int blankCellLineCount)
    {
        var row = cursor / columns;
        return (row + 1 + Math.Max(0, blankCellLineCount)) * columns;
    }

    private static int NextContentColumnCursor(int cursor, int rows, int blankCellLineCount)
    {
        var column = cursor / rows;
        return (column + 1 + Math.Max(0, blankCellLineCount)) * rows;
    }

    private static SheetPage CreateBlankLayoutPage(
        CharacterSheetSpec spec,
        int columns,
        int rows,
        bool hasHeader,
        double gridSizeMm,
        double cellPitch,
        double rowPitch,
        double columnPitch,
        double groupGapMm,
        int repeatedGroupSize)
    {
        var contentTop = spec.Page.MarginTopMm + (hasHeader ? HeaderHeightMm(spec) : 0);
        var cells = new List<CellSlot>();
        var capacity = Math.Max(0, columns) * Math.Max(0, rows);
        AddBlankSlots(
            cells,
            spec,
            spec.Orientation == SheetOrientation.Vertical,
            0,
            capacity,
            columns,
            rows,
            contentTop,
            cellPitch,
            rowPitch,
            columnPitch,
            groupGapMm,
            repeatedGroupSize,
            gridSizeMm);

        return new SheetPage(0, columns, rows, hasHeader, cells);
    }

    private static void AddBlankSlots(
        List<CellSlot> cells,
        CharacterSheetSpec spec,
        bool vertical,
        int start,
        int end,
        int columns,
        int rows,
        double contentTop,
        double cellPitch,
        double rowPitch,
        double columnPitch,
        double groupGapMm,
        int repeatedGroupSize,
        double gridSizeMm)
    {
        var capacity = columns * rows;
        for (var absolute = Math.Max(0, start); absolute < Math.Min(end, capacity); absolute++)
        {
            var (x, y) = CellPosition(
                spec,
                vertical,
                absolute,
                columns,
                rows,
                contentTop,
                cellPitch,
                rowPitch,
                columnPitch,
                groupGapMm,
                repeatedGroupSize);
            cells.Add(new CellSlot(
                x,
                y,
                gridSizeMm,
                string.Empty,
                CellRole.Blank,
                -1));
        }
    }

    private static IReadOnlyList<IReadOnlyList<string>> SplitOversizedGroups(
        IReadOnlyList<IReadOnlyList<string>> groups,
        int maxGroupSize)
    {
        maxGroupSize = Math.Max(1, maxGroupSize);
        var result = new List<IReadOnlyList<string>>(groups.Count);

        foreach (var group in groups)
        {
            if (group.Count <= maxGroupSize)
            {
                result.Add(group);
                continue;
            }

            for (var offset = 0; offset < group.Count; offset += maxGroupSize)
                result.Add(group.Skip(offset).Take(maxGroupSize).ToArray());
        }

        return result;
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
        double cellPitch,
        double rowPitch,
        double columnPitch,
        double groupGapMm,
        int repeatedGroupSize)
    {
        if (vertical)
        {
            var column = columns - 1 - absolute / rows;
            var row = absolute % rows;
            return (spec.Page.MarginLeftMm + column * columnPitch, contentTop + row * rowPitch);
        }

        var rowIndex = absolute / columns;
        var columnIndex = absolute % columns;
        var groupGap = repeatedGroupSize > 0
            ? columnIndex / repeatedGroupSize * groupGapMm
            : 0;
        return (
            spec.Page.MarginLeftMm + columnIndex * cellPitch + groupGap,
            contentTop + rowIndex * rowPitch);
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
    private static IReadOnlyList<IReadOnlyList<string>> EnumeratePinyinGroups(CharacterSheetSpec spec)
    {
        var groups = new List<IReadOnlyList<string>>();
        foreach (var token in spec.Text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            if (!ContainsCjk(token))
            {
                groups.Add(new[] { token });
                continue;
            }

            var enumerator = StringInfo.GetTextElementEnumerator(token);
            while (enumerator.MoveNext())
            {
                var element = enumerator.GetTextElement();
                if (string.IsNullOrWhiteSpace(element) ||
                    element.Length > 0 && SentencePunctuation.Contains(element[0]))
                    continue;

                groups.Add(new[]
                {
                    spec.PinyinByGlyph is not null &&
                    spec.PinyinByGlyph.TryGetValue(element, out var pinyin)
                        ? pinyin
                        : element
                });
            }
        }

        return groups;
    }

    private static bool ContainsCjk(string text)
    {
        return text.Any(character =>
            character is >= '\u3400' and <= '\u9FFF' ||
            character is >= '\uF900' and <= '\uFAFF');
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
