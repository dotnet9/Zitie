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
    private const double FieldsLineMm = 10;
    private const double FooterLineMm = 10;

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
        var groups = spec.GroupByWord
            ? EnumerateWordGlyphGroups(spec.Text)
            : EnumerateGlyphGroups(spec.Text);
        if (groups.Count == 0) return pages;

        var roles = BuildSlotRoles(spec.Mode, spec.RepeatsPerChar, spec.TraceSlotCount);
        // 词模式：每个字符一格，角色统一；看拼音写词语时隐藏范字只留空格
        var wordRole = spec.PinyinOnly || spec.Mode == PracticeMode.Copy
            ? CellRole.Blank
            : CellRole.Trace;
        var pitch = spec.GridSizeMm + spec.GridGapMm;
        var columns = Math.Max(1, (int)Math.Floor(spec.Page.UsableWidthMm / pitch));
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

            // 每个组（一个字或一个词）占据连续格位（行优先），放不下时整组顺延到下一页。
            // 字符模式一组是 roles.Count 格（范字+描红+空格），词模式一组是词内字符数。
            while (groupIndex < groups.Count)
            {
                var glyphs = groups[groupIndex];
                var groupSize = spec.GroupByWord ? glyphs.Count : roles.Count;
                if (cursor + groupSize > capacity) break;

                for (var slot = 0; slot < groupSize; slot++)
                {
                    var absolute = cursor + slot;
                    var glyph = spec.GroupByWord ? glyphs[slot] : glyphs[0];
                    cells.Add(new CellSlot(
                        spec.Page.MarginLeftMm + absolute % columns * pitch,
                        contentTop + absolute / columns * pitch,
                        spec.GridSizeMm,
                        glyph,
                        spec.GroupByWord ? wordRole : roles[slot],
                        groupIndex));
                }

                cursor += groupSize;
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

    public static double HeaderHeightMm(CharacterSheetSpec spec)
    {
        var height = 0.0;
        if (spec.Title is not null) height += TitleLineMm;
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
}
