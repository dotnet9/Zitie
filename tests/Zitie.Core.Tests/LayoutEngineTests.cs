using Zitie.Core.Layout;
using Zitie.Core.Models;
using Xunit;

namespace Zitie.Core.Tests;

public class LayoutEngineTests
{
    private static CharacterSheetSpec MakeSpec(
        string text,
        GridKind grid = GridKind.Mi,
        PracticeMode mode = PracticeMode.Trace,
        int repeats = 5,
        int traceCount = 2,
        string? title = "练习")
    {
        return new CharacterSheetSpec
        {
            Text = text,
            Grid = grid,
            Mode = mode,
            RepeatsPerChar = repeats,
            TraceSlotCount = traceCount,
            Title = title,
            ShowHeaderFields = true
        };
    }

    [Fact]
    public void BuildSlotRoles_Trace_ModeHasModelTraceBlank()
    {
        var roles = LayoutEngine.BuildSlotRoles(PracticeMode.Trace, 5, 2);

        Assert.Equal(
        [
            CellRole.Model, CellRole.Trace, CellRole.Trace, CellRole.Blank, CellRole.Blank
        ], roles);
    }

    [Fact]
    public void BuildSlotRoles_Copy_ModeHasNoTraceSlots()
    {
        var roles = LayoutEngine.BuildSlotRoles(PracticeMode.Copy, 4, 2);

        Assert.Equal([CellRole.Model, CellRole.Blank, CellRole.Blank, CellRole.Blank], roles);
    }

    [Fact]
    public void BuildSlotRoles_ClampsInvalidArguments()
    {
        var roles = LayoutEngine.BuildSlotRoles(PracticeMode.Trace, 1, 5);

        Assert.Single(roles);
        Assert.Equal(CellRole.Model, roles[0]);
    }

    [Fact]
    public void Paginate_EmptyText_ReturnsNoPages()
    {
        Assert.Empty(LayoutEngine.Paginate(MakeSpec("  \n\t ")));
    }

    [Fact]
    public void Paginate_EveryCharExpandsToRepeatSlots()
    {
        var pages = LayoutEngine.Paginate(MakeSpec("一二三"));

        var slots = pages.SelectMany(page => page.Cells).ToList();
        Assert.Equal(15, slots.Count);
        for (var group = 0; group < 3; group++)
        {
            var groupSlots = slots.Where(slot => slot.GroupIndex == group).ToList();
            Assert.Equal(5, groupSlots.Count);
            Assert.Equal(CellRole.Model, groupSlots[0].Role);
            Assert.Equal("一二三"[group].ToString(), groupSlots[0].Glyph);
        }
    }

    [Fact]
    public void Paginate_SlotsStayInsidePageBounds()
    {
        var spec = MakeSpec(new string('永', 100));
        var pages = LayoutEngine.Paginate(spec);

        Assert.NotEmpty(pages);
        foreach (var slot in pages.SelectMany(page => page.Cells))
        {
            Assert.InRange(slot.XMm,
                spec.Page.MarginLeftMm - 0.01,
                spec.Page.WidthMm - spec.Page.MarginRightMm - slot.SizeMm + 0.01);
            Assert.InRange(slot.YMm,
                spec.Page.MarginTopMm - 0.01,
                spec.Page.HeightMm - spec.Page.MarginBottomMm - slot.SizeMm + 0.01);
        }
    }

    [Fact]
    public void Paginate_NoTwoSlotsShareTheSameCell()
    {
        var pages = LayoutEngine.Paginate(MakeSpec(new string('永', 60)));

        foreach (var page in pages)
        {
            var positions = page.Cells
                .Select(slot => (slot.XMm, slot.YMm))
                .ToHashSet();
            Assert.Equal(page.Cells.Count, positions.Count);
        }
    }

    [Fact]
    public void Paginate_OnlyFirstPageHasHeader()
    {
        var pages = LayoutEngine.Paginate(MakeSpec(new string('永', 100)));

        Assert.True(pages[0].HasHeader);
        Assert.All(pages.Skip(1), page => Assert.False(page.HasHeader));
    }

    [Fact]
    public void Paginate_CharacterGroupIsNeverSplitAcrossPages()
    {
        var pages = LayoutEngine.Paginate(MakeSpec(new string('永', 100)));

        var groupsPerPage = pages
            .Select(page => page.Cells.Select(slot => slot.GroupIndex).ToHashSet())
            .ToList();
        for (var i = 1; i < groupsPerPage.Count; i++)
            Assert.Empty(groupsPerPage[0].Intersect(groupsPerPage[i]));
    }

    [Fact]
    public void Paginate_SurrogatePairCountsAsSingleGlyph()
    {
        var pages = LayoutEngine.Paginate(MakeSpec("𠀀", repeats: 3, traceCount: 1));

        var slots = pages.SelectMany(page => page.Cells).ToList();
        Assert.Equal(3, slots.Count);
        Assert.All(slots, slot => Assert.Equal("𠀀", slot.Glyph));
    }

    [Fact]
    public void Paginate_GroupByWord_EachWordGetsConsecutiveSlots()
    {
        var spec = MakeSpec("春眠 不觉晓", mode: PracticeMode.Copy, repeats: 1)
            with { GroupByWord = true };
        var pages = LayoutEngine.Paginate(spec);

        var slots = pages.SelectMany(page => page.Cells).ToList();
        Assert.Equal(5, slots.Count); // 春眠 不觉晓 = 2 + 3 字，repeats=1
        Assert.Equal(["春", "眠", "不", "觉", "晓"], slots.Select(slot => slot.Glyph));
        // 两个词分属不同组
        Assert.Equal(0, slots[1].GroupIndex);
        Assert.Equal(1, slots[2].GroupIndex);
    }

    [Fact]
    public void Paginate_PinyinOnly_HidesModelGlyph()
    {
        var spec = MakeSpec("春眠", mode: PracticeMode.Copy, repeats: 1)
            with { GroupByWord = true, PinyinOnly = true };
        var pages = LayoutEngine.Paginate(spec);

        var slots = pages.SelectMany(page => page.Cells).ToList();
        Assert.Equal(2, slots.Count);
        Assert.All(slots, slot => Assert.Equal(CellRole.Blank, slot.Role));
    }

    [Fact]
    public void Paginate_GroupByWord_WordNeverSplitAcrossPages()
    {
        // 一页只能放少量格子的场景：小页宽迫使换页，验证词不被拆开
        var spec = new CharacterSheetSpec
        {
            Text = "一二三 四五六 七八九 十",
            Grid = GridKind.Tian,
            Mode = PracticeMode.Copy,
            RepeatsPerChar = 1,
            GroupByWord = true,
            Title = null,
            ShowHeaderFields = false,
            Page = PageSettings.A4 with { MarginLeftMm = 20, MarginRightMm = 20, MarginTopMm = 20, MarginBottomMm = 20 }
        };
        var pages = LayoutEngine.Paginate(spec);

        var groupsPerPage = pages
            .Select(page => page.Cells.Select(slot => slot.GroupIndex).ToHashSet())
            .ToList();
        for (var i = 1; i < groupsPerPage.Count; i++)
            Assert.Empty(groupsPerPage[0].Intersect(groupsPerPage[i]));
    }

    [Fact]
    public void Paginate_EnglishGrid_ProducesSlots()
    {
        var spec = MakeSpec("cat dog", grid: GridKind.English, mode: PracticeMode.Copy, repeats: 1)
            with { GroupByWord = true };
        var pages = LayoutEngine.Paginate(spec);

        var slots = pages.SelectMany(page => page.Cells).ToList();
        Assert.Equal(6, slots.Count);
        Assert.Equal("cat", string.Concat(slots.Take(3).Select(slot => slot.Glyph)));
    }

    [Fact]
    public void Paginate_Vertical_FillsRightColumnFirst()
    {
        // 小页面每列 2 格，一句 4 字恰好占满一列：第一句在最右列，第二句在左一列
        var spec = new CharacterSheetSpec
        {
            Text = "一二三，四五六。",
            Grid = GridKind.Mi,
            Mode = PracticeMode.Copy,
            RepeatsPerChar = 1,
            Orientation = SheetOrientation.Vertical,
            Title = null,
            ShowHeaderFields = false,
            Page = PageSettings.A4 with { HeightMm = 80 }
        };
        var pages = LayoutEngine.Paginate(spec);

        var slots = pages.SelectMany(page => page.Cells).ToList();
        // 两句各 4 字（含标点）
        Assert.Equal(8, slots.Count);
        // 同一列内的字符 x 相同（列对齐）
        Assert.Equal(slots[0].XMm, slots[1].XMm, 3);
        // 第一句字符在上（y 小），第二句整体在左侧（x 小）
        Assert.True(slots[4].XMm < slots[0].XMm, "第二句应在第一句左侧");
    }

    [Fact]
    public void Paginate_Vertical_SentenceNeverSplitAcrossColumns()
    {
        var spec = new CharacterSheetSpec
        {
            Text = "床前明月光，疑是地上霜。举头望明月，低头思故乡。",
            Grid = GridKind.Plain,
            Mode = PracticeMode.Copy,
            RepeatsPerChar = 1,
            Orientation = SheetOrientation.Vertical,
            Title = null,
            ShowHeaderFields = false
        };
        var pages = LayoutEngine.Paginate(spec);

        Assert.NotEmpty(pages);
        // 每组句子都在同一列（x 相同），不被拆到不同列
        foreach (var page in pages)
        {
            var groupXs = page.Cells.GroupBy(slot => slot.GroupIndex)
                .Select(g => g.Select(slot => slot.XMm).Distinct().Count());
            Assert.All(groupXs, distinct => Assert.Equal(1, distinct));
        }
    }

    [Fact]
    public void Paginate_NineGrid_UsesLargerGridSize()
    {
        var spec = MakeSpec("永字八法", grid: GridKind.Nine, mode: PracticeMode.Copy, repeats: 1)
            with { GridSizeMm = 30 };
        var pages = LayoutEngine.Paginate(spec);

        var slots = pages.SelectMany(page => page.Cells).ToList();
        Assert.Equal(4, slots.Count);
        Assert.All(slots, slot => Assert.Equal(30, slot.SizeMm));
        // 30mm 格子 + 2mm 间距：一行 5 个、一页 7 行（含题头），4 字占第一行
        Assert.Equal(5, pages[0].Columns);
        Assert.Equal(7, pages[0].Rows);
    }

    [Fact]
    public void Paginate_HollowGlyph_HasModelRoleForCopyMode()
    {
        // 空心双钩字属于范字，role 保持 Model（不是 Blank）
        var spec = MakeSpec("永", grid: GridKind.Tian, mode: PracticeMode.Copy, repeats: 2)
            with { HollowGlyph = true };
        var pages = LayoutEngine.Paginate(spec);

        var slots = pages.SelectMany(page => page.Cells).ToList();
        Assert.Equal(2, slots.Count);
        Assert.Equal(CellRole.Model, slots[0].Role);
        Assert.Equal(CellRole.Blank, slots[1].Role);
    }

    [Fact]
    public void Paginate_PinyinGrid_OneSyllablePerCell()
    {
        var spec = MakeSpec("chūn tiān huā", grid: GridKind.Pinyin, mode: PracticeMode.Copy, repeats: 1)
            with { GridSizeMm = 20 };
        var pages = LayoutEngine.Paginate(spec);

        var slots = pages.SelectMany(page => page.Cells).ToList();
        Assert.Equal(3, slots.Count);
        Assert.Equal(["chūn", "tiān", "huā"], slots.Select(slot => slot.Glyph));
        Assert.All(slots, slot => Assert.Equal(CellRole.Model, slot.Role));
    }

    [Fact]
    public void Paginate_TraceColor_DoesNotChangeLayout()
    {
        // 描红颜色属于渲染外观，不影响排版：两种颜色应产出完全相同的格子
        var baseline = LayoutEngine.Paginate(MakeSpec("山水", repeats: 4));
        var colored = LayoutEngine.Paginate(MakeSpec("山水", repeats: 4) with { TraceColor = "#FF0000" });
        var invalid = LayoutEngine.Paginate(MakeSpec("山水", repeats: 4) with { TraceColor = "not-a-color" });

        Assert.Equal(baseline.Count, colored.Count);
        Assert.Equal(baseline.Count, invalid.Count);
        Assert.Equal(
            baseline.SelectMany(page => page.Cells).Select(cell => cell.Role),
            colored.SelectMany(page => page.Cells).Select(cell => cell.Role));
    }
}
