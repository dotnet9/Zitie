using Xunit;
using Zitie.Desktop.Models;

namespace Zitie.Desktop.Tests;

public sealed class TextContentSelectionTests
{
    private static readonly TextEntry[] Entries =
    [
        new()
        {
            Subject = "语文", Grade = "三年级", Semester = "上册", Unit = "第一单元",
            Edition = "统编版", Title = "晨读", Body = "认真晨读"
        },
        new()
        {
            Subject = "语文", Grade = "五年级", Semester = "下册", Unit = "第二单元",
            Edition = "统编版", Title = "名著", Body = "阅读名著"
        },
        new()
        {
            Subject = "英语", Grade = "三年级", Semester = "上册", Unit = "Unit 1",
            Edition = "人教版", Title = "Hello", Body = "Hello!"
        },
        new()
        {
            Subject = "语文", Grade = "高一", Semester = "上册", Unit = "思辨",
            Edition = "通用版", Title = "文学鉴赏", Body = "文字越细读，意味越深长"
        }
    ];

    [Fact]
    public void Filters_CascadeFromSubjectToUnit()
    {
        var selection = new TextContentSelection(Entries)
        {
            SelectedSubject = "语文",
            SelectedEdition = "统编版",
            SelectedGrade = "三年级",
            SelectedSemester = "上册",
            SelectedUnit = "第一单元"
        };

        var entry = Assert.Single(selection.FilteredEntries);
        Assert.Equal("晨读", entry.Title);
        Assert.Equal(["全部", "统编版", "通用版"], selection.Editions);
        Assert.Equal(["全部", "三年级", "五年级"], selection.Grades);
        Assert.Equal(["全部", "上册"], selection.Semesters);
        Assert.Equal("1 条内容", selection.ResultSummary);
    }

    [Fact]
    public void ChangingParentFilter_ResetsChildSelections()
    {
        var selection = new TextContentSelection(Entries)
        {
            SelectedSubject = "语文",
            SelectedEdition = "统编版",
            SelectedGrade = "五年级",
            SelectedSemester = "下册"
        };

        selection.SelectedSubject = "英语";

        Assert.Equal(TextContentSelection.All, selection.SelectedEdition);
        Assert.Equal(TextContentSelection.All, selection.SelectedGrade);
        Assert.Equal(TextContentSelection.All, selection.SelectedSemester);
        Assert.All(selection.FilteredEntries, entry => Assert.Equal("英语", entry.Subject));
    }

    [Fact]
    public void SelectingEntry_RaisesDomainEvent()
    {
        var selection = new TextContentSelection(Entries);
        TextEntry? selected = null;
        selection.EntrySelected += (_, entry) => selected = entry;

        selection.SelectedEntry = Entries[0];

        Assert.Same(Entries[0], selected);
        Assert.Equal("晨读", selected?.ToString());
    }

    [Fact]
    public void SelectingChildFilter_DoesNotReplaceUnchangedParentChoices()
    {
        var selection = new TextContentSelection(Entries) { SelectedSubject = "语文", SelectedEdition = "统编版" };
        var changedProperties = new List<string?>();
        selection.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

        selection.SelectedGrade = "三年级";

        Assert.DoesNotContain(nameof(TextContentSelection.Grades), changedProperties);
        Assert.Equal("三年级", selection.SelectedGrade);
    }

    [Fact]
    public void EditionFilter_NarrowsAvailableGrades()
    {
        var selection = new TextContentSelection(Entries) { SelectedSubject = "英语" };

        selection.SelectedEdition = "人教版";

        Assert.Equal(["全部", "三年级"], selection.Grades);
        Assert.All(selection.FilteredEntries, entry => Assert.Equal("人教版", entry.Edition));
    }

    [Fact]
    public void SearchText_FiltersAcrossTitleAndBody()
    {
        var selection = new TextContentSelection(Entries)
        {
            SearchText = "深长"
        };

        var entry = Assert.Single(selection.FilteredEntries);
        Assert.Equal("文学鉴赏", entry.Title);

        selection.SearchText = "hello";

        entry = Assert.Single(selection.FilteredEntries);
        Assert.Equal("Hello", entry.Title);
    }
}
