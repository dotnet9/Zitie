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
}
