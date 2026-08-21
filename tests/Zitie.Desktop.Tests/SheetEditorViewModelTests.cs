using Prism.Regions;
using Xunit;
using Zitie.Core.Models;
using Zitie.Desktop.Services;
using Zitie.Desktop.ViewModels;

namespace Zitie.Desktop.Tests;

public sealed class SheetEditorViewModelTests(AvaloniaHeadlessFixture fixture)
    : IClassFixture<AvaloniaHeadlessFixture>
{
    [Fact]
    public void SwitchTemplate_PreservesCurrentContent()
    {
        _ = fixture;
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

        var template = Assert.Single(catalog.Modules, module => module.Id == "english-four-line");

        viewModel.SwitchTemplate(template);

        Assert.Equal("小朋友认真练字", viewModel.InputText);
        Assert.Equal("课堂练习", viewModel.Title);
        Assert.Equal("小明", viewModel.Author);
        Assert.Equal("现代", viewModel.Dynasty);
        Assert.True(viewModel.ShowPoemHeader);
        Assert.Equal(GridKind.English, viewModel.Spec.Grid);
        Assert.Same(template, viewModel.SelectedModule);
        Assert.True(viewModel.IsDirty);
    }

    [Fact]
    public void ContentSelection_PreviewsUntilDrawerIsClosed()
    {
        _ = fixture;
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
        _ = fixture;
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
        _ = fixture;
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
        var template = Assert.Single(catalog.Modules, module => module.Id == "english-four-line");

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
        _ = fixture;
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
        var template = Assert.Single(catalog.Modules, module => module.Id == "english-four-line");

        viewModel.PreviewTemplateModule = template;
        viewModel.ApplySelectedTemplateCommand.Execute();

        Assert.Equal(GridKind.English, viewModel.Spec.Grid);
        Assert.Same(template, viewModel.SelectedModule);
        Assert.True(viewModel.IsDirty);
        Assert.False(viewModel.IsTemplatePickerOpen);
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
