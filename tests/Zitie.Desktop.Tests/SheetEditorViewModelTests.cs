using Prism.Regions;
using Xunit;
using Zitie.Core.Models;
using Zitie.Desktop.Services;
using Zitie.Desktop.ViewModels;

namespace Zitie.Desktop.Tests;

public sealed class SheetEditorViewModelTests
{
    [Fact]
    public void SwitchTemplate_PreservesCurrentContent()
    {
        var catalog = new ModuleCatalog();
        var viewModel = new SheetEditorViewModel(
            catalog,
            new TextCatalog(),
            new PinyinCatalog(),
            new FontCatalog(),
            new StubNavigationJournal(),
            new StubDialogs());
        viewModel.InputText = "小朋友认真练字";
        viewModel.Title = "课堂练习";
        viewModel.Author = "小明";
        viewModel.Dynasty = "现代";
        viewModel.ShowPoemHeader = true;

        var template = Assert.Single(catalog.Modules, module => module.Id == "nqez-1013");

        viewModel.SwitchTemplate(template);

        Assert.Equal("小朋友认真练字", viewModel.InputText);
        Assert.Equal("课堂练习", viewModel.Title);
        Assert.Equal("小明", viewModel.Author);
        Assert.Equal("现代", viewModel.Dynasty);
        Assert.True(viewModel.ShowPoemHeader);
        Assert.Equal(GridKind.English, viewModel.Spec.Grid);
        Assert.True(viewModel.Spec.FillContentAreaWithBlankCells);
        Assert.Equal(viewModel.Pages[0].Columns * viewModel.Pages[0].Rows, viewModel.Pages[0].Cells.Count);
        Assert.Same(template, viewModel.SelectedModule);
        Assert.True(viewModel.IsDirty);
    }

    [Fact]
    public void ContentSelection_PreviewsUntilDrawerIsClosed()
    {
        var viewModel = new SheetEditorViewModel(
            new ModuleCatalog(),
            new TextCatalog(),
            new PinyinCatalog(),
            new FontCatalog(),
            new StubNavigationJournal(),
            new StubDialogs())
        {
            InputText = "原始练习内容",
            IsContentPickerOpen = true
        };
        var result = viewModel.ContentSelection.FilteredItems.First();

        viewModel.ContentSelection.SelectedResult = result;

        Assert.Equal(result.Entry.Body, viewModel.InputText);
        Assert.True(viewModel.IsContentPickerOpen);
        Assert.True(viewModel.ApplySelectedContentCommand.CanExecute());

        viewModel.CloseContentPickerCommand.Execute();

        Assert.Equal("原始练习内容", viewModel.InputText);
        Assert.False(viewModel.IsContentPickerOpen);
    }

    [Fact]
    public void ContentSelection_AppliesPreviewAfterConfirmation()
    {
        var viewModel = new SheetEditorViewModel(
            new ModuleCatalog(),
            new TextCatalog(),
            new PinyinCatalog(),
            new FontCatalog(),
            new StubNavigationJournal(),
            new StubDialogs())
        {
            InputText = "原始练习内容",
            IsContentPickerOpen = true
        };
        var result = viewModel.ContentSelection.FilteredItems.First();

        viewModel.ContentSelection.SelectedResult = result;

        viewModel.ApplySelectedContentCommand.Execute();

        Assert.Equal(result.Entry.Body, viewModel.InputText);
        Assert.Equal(result.Entry.Title, viewModel.Title);
        Assert.False(viewModel.IsContentPickerOpen);
    }

    [Fact]
    public void TemplateSelection_PreviewsUntilDrawerIsClosed()
    {
        var catalog = new ModuleCatalog();
        var viewModel = new SheetEditorViewModel(
            catalog,
            new TextCatalog(),
            new PinyinCatalog(),
            new FontCatalog(),
            new StubNavigationJournal(),
            new StubDialogs())
        {
            InputText = "小朋友认真练字",
            IsTemplatePickerOpen = true
        };
        var template = Assert.Single(catalog.Modules, module => module.Id == "nqez-1013");

        viewModel.PreviewTemplateModule = template;

        Assert.Equal("小朋友认真练字", viewModel.InputText);
        Assert.Equal(GridKind.English, viewModel.Spec.Grid);
        Assert.Same(template, viewModel.SelectedModule);

        viewModel.CloseTemplatePickerCommand.Execute();

        Assert.Equal("小朋友认真练字", viewModel.InputText);
        Assert.Equal(GridKind.Mi, viewModel.Spec.Grid);
        Assert.Null(viewModel.SelectedModule);
        Assert.False(viewModel.IsTemplatePickerOpen);
    }

    [Fact]
    public void TemplateSelection_AppliesPreviewAfterConfirmation()
    {
        var catalog = new ModuleCatalog();
        var viewModel = new SheetEditorViewModel(
            catalog,
            new TextCatalog(),
            new PinyinCatalog(),
            new FontCatalog(),
            new StubNavigationJournal(),
            new StubDialogs())
        {
            IsTemplatePickerOpen = true
        };
        var template = Assert.Single(catalog.Modules, module => module.Id == "nqez-1013");

        viewModel.PreviewTemplateModule = template;
        viewModel.ApplySelectedTemplateCommand.Execute();

        Assert.Equal(GridKind.English, viewModel.Spec.Grid);
        Assert.Same(template, viewModel.SelectedModule);
        Assert.True(viewModel.IsDirty);
        Assert.False(viewModel.IsTemplatePickerOpen);
    }

    [Fact]
    public void BlankLayoutTemplate_DisablesContentSelectionAndLeavesBodyBlank()
    {
        var catalog = new ModuleCatalog();
        var viewModel = new SheetEditorViewModel(
            catalog,
            new TextCatalog(),
            new PinyinCatalog(),
            new FontCatalog(),
            new StubNavigationJournal(),
            new StubDialogs())
        {
            InputText = "已有正文"
        };
        var template = Assert.Single(catalog.Modules, module => module.Id == "nqez-960");

        viewModel.SwitchTemplate(template);

        Assert.False(viewModel.IsContentEditingEnabled);
        Assert.Equal(string.Empty, viewModel.InputText);
        Assert.True(viewModel.Spec.BlankContentLayout);
        Assert.NotEmpty(viewModel.Pages.Single().Cells);

        viewModel.IsContentPickerOpen = true;

        Assert.False(viewModel.IsContentPickerOpen);
    }

    [Fact]
    public void SwitchingFromBlankLayoutToPracticeTemplate_ReenablesContent()
    {
        var catalog = new ModuleCatalog();
        var viewModel = new SheetEditorViewModel(
            catalog,
            new TextCatalog(),
            new PinyinCatalog(),
            new FontCatalog(),
            new StubNavigationJournal(),
            new StubDialogs());
        var blankTemplate = Assert.Single(catalog.Modules, module => module.Id == "nqez-960");
        var practiceTemplate = Assert.Single(catalog.Modules, module => module.Id == "nqez-945");

        viewModel.SwitchTemplate(blankTemplate);
        viewModel.SwitchTemplate(practiceTemplate);

        Assert.True(viewModel.IsContentEditingEnabled);
        Assert.False(viewModel.Spec.BlankContentLayout);
    }

    [Fact]
    public void SpecialPracticeTemplate_DisablesStandardLayoutOptions()
    {
        var catalog = new ModuleCatalog();
        var viewModel = new SheetEditorViewModel(
            catalog,
            new TextCatalog(),
            new PinyinCatalog(),
            new FontCatalog(),
            new StubNavigationJournal(),
            new StubDialogs());
        var template = Assert.Single(catalog.Modules, module => module.Id == "nqez-948");

        viewModel.SwitchTemplate(template);

        Assert.True(viewModel.IsSpecialPracticeLayout);
        Assert.False(viewModel.IsStandardPracticeLayout);
        Assert.False(viewModel.IsPoemHeaderFieldsEnabled);
        Assert.Equal(PracticeLayoutKind.BracketWordRows, viewModel.Spec.PracticeLayout);
    }

    [Fact]
    public void CharacterWordsPoemTemplate_ResetsPreviousPaperBackground()
    {
        var catalog = new ModuleCatalog();
        var viewModel = new SheetEditorViewModel(
            catalog,
            new TextCatalog(),
            new PinyinCatalog(),
            new FontCatalog(),
            new StubNavigationJournal(),
            new StubDialogs());
        var template = Assert.Single(catalog.Modules, module => module.Id == "nqez-951");
        viewModel.BackgroundIndex = (int)SheetBackground.RicePaper;
        viewModel.InputText = "春冬风雪花";

        viewModel.SwitchTemplate(template);

        Assert.Equal(SheetBackground.Plain, viewModel.Spec.Background);
        Assert.Equal("#FFFFFF", viewModel.Spec.BackgroundColor);
        Assert.Equal(PracticeLayoutKind.CharacterWordsPoem, viewModel.Spec.PracticeLayout);
        Assert.NotNull(viewModel.Spec.StrokeOrderByGlyph);
        Assert.True(viewModel.Spec.StrokeOrderByGlyph!.ContainsKey("春"));
    }

    [Fact]
    public void CharacterWordsPoemTemplate_KeepsResourceTitleAndTitleStrokeOrders()
    {
        var catalog = new ModuleCatalog();
        var viewModel = new SheetEditorViewModel(
            catalog,
            new TextCatalog(),
            new PinyinCatalog(),
            new FontCatalog(),
            new StubNavigationJournal(),
            new StubDialogs());
        var template = Assert.Single(catalog.Modules, module => module.Id == "nqez-951");

        viewModel.SwitchTemplate(template);
        viewModel.InputText = string.Empty;
        viewModel.Title = "爱莲说";
        viewModel.Dynasty = "宋";
        viewModel.Author = "周敦颐";

        var path = Path.Combine(Path.GetTempPath(), $"zitie-character-words-poem-{Guid.NewGuid():N}.zitie.json");
        try
        {
            viewModel.SaveDocumentTo(path);

            Assert.Equal("爱莲说", viewModel.Spec.Title);
            Assert.NotNull(viewModel.Spec.StrokeOrderByGlyph);
            Assert.True(viewModel.Spec.StrokeOrderByGlyph!.ContainsKey("爱"));
            Assert.True(viewModel.Spec.StrokeOrderByGlyph.ContainsKey("春"));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void FiveCharacterPoemCalligraphyTemplate_AllowsBlankTitleAndText()
    {
        var catalog = new ModuleCatalog();
        var viewModel = new SheetEditorViewModel(
            catalog,
            new TextCatalog(),
            new PinyinCatalog(),
            new FontCatalog(),
            new StubNavigationJournal(),
            new StubDialogs());
        var template = Assert.Single(catalog.Modules, module => module.Id == "nqez-953");

        viewModel.SwitchTemplate(template);
        viewModel.Title = string.Empty;
        viewModel.InputText = string.Empty;

        var path = Path.Combine(Path.GetTempPath(), $"zitie-five-poem-blank-{Guid.NewGuid():N}.zitie.json");
        try
        {
            viewModel.SaveDocumentTo(path);

            Assert.Null(viewModel.Spec.Title);
            Assert.Equal(string.Empty, viewModel.Spec.Text);
            Assert.Equal(20, viewModel.Pages[0].Cells.Count);
            Assert.All(viewModel.Pages[0].Cells, cell => Assert.Equal(string.Empty, cell.Glyph));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void StrokeOrderTemplate_LoadsStrokeOrdersForInputText()
    {
        var catalog = new ModuleCatalog();
        var viewModel = new SheetEditorViewModel(
            catalog,
            new TextCatalog(),
            new PinyinCatalog(),
            new FontCatalog(),
            new StubNavigationJournal(),
            new StubDialogs());
        var template = Assert.Single(catalog.Modules, module => module.Id == "nqez-19279");

        viewModel.SwitchTemplate(template);
        viewModel.InputText = "春";

        var path = Path.Combine(Path.GetTempPath(), $"zitie-stroke-order-{Guid.NewGuid():N}.zitie.json");
        try
        {
            viewModel.SaveDocumentTo(path);

            Assert.True(viewModel.Spec.ShowStrokeOrder);
            Assert.NotNull(viewModel.Spec.StrokeOrderByGlyph);
            Assert.True(viewModel.Spec.StrokeOrderByGlyph!.ContainsKey("春"));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    private sealed class StubNavigationJournal : IRegionNavigationJournal
    {
        public bool CanGoBack => false;

        public bool CanGoForward => false;

        public IRegionNavigationJournalEntry? CurrentEntry => null;

        public INavigateAsync? NavigationTarget { get; set; }

        public void GoBack()
        {
        }

        public void GoForward()
        {
        }

        public void RecordNavigation(IRegionNavigationJournalEntry entry, bool persistInHistory = true)
        {
        }

        public void Clear()
        {
        }
    }

    private sealed class StubDialogs : ISystemDialogs
    {
        public Task<string?> PickOpenFileAsync(
            string typeName,
            IReadOnlyList<string> patterns,
            string title = "打开文件")
        {
            return Task.FromResult<string?>(null);
        }

        public Task<string?> PickSaveFileAsync(string typeName, string extension, string suggestedName)
        {
            return Task.FromResult<string?>(null);
        }

        public Task<string?> PickFolderAsync(string title)
        {
            return Task.FromResult<string?>(null);
        }

        public void RevealFile(string fileFullName)
        {
        }

        public void OpenFolder(string folderFullName)
        {
        }
    }
}
