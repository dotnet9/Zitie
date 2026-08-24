using Avalonia.Media.Imaging;
using Prism.Commands;
using Xunit;
using Zitie.Desktop.Services;
using Zitie.Desktop.ViewModels;

namespace Zitie.Desktop.Tests;

public sealed class ModuleGalleryPerformanceTests(AvaloniaHeadlessFixture fixture)
    : IClassFixture<AvaloniaHeadlessFixture>
{
    [Fact]
    public void CardConstruction_DoesNotCreatePreviewsOrDecodeImages()
    {
        var catalog = new ModuleCatalog();
        var cards = catalog.Modules
            .Select(module => new ModuleCardViewModel(module, new DelegateCommand(() => { })))
            .ToArray();

        try
        {
            Assert.Equal(326, cards.Length);
            Assert.All(cards, card => Assert.False(card.IsPreviewMaterialized));
            Assert.All(cards, card => Assert.False(card.IsThumbnailLoadStarted));
        }
        finally
        {
            foreach (var card in cards) card.Dispose();
        }
    }

    [Fact]
    public void ScreenshotThumbnail_IsDecodedToBoundedHeight()
    {
        _ = fixture;
        var catalog = new ModuleCatalog();
        var module = Assert.Single(catalog.Modules, item => item.Id == "nqez-945");

        using var image = ModuleCardViewModel.DecodeThumbnail(module.PreviewImagePath);

        Assert.InRange(image.PixelSize.Height, 1, ModuleCardViewModel.ThumbnailDecodeHeight);
    }

    [Fact]
    public void DirectoryTemplate_CreatesGeneratedPreviewOnlyWhenRequested()
    {
        var catalog = new ModuleCatalog();
        var module = Assert.Single(catalog.Modules, item => item.Id == "poem-wuyan-spring-scene");
        using var card = new ModuleCardViewModel(module, new DelegateCommand(() => { }));

        Assert.False(card.HasPreviewImage);
        Assert.False(card.IsPreviewMaterialized);

        Assert.NotNull(card.PreviewSpec);
        Assert.NotEmpty(Assert.IsAssignableFrom<IReadOnlyList<Zitie.Core.Layout.SheetPage>>(card.PreviewPages));
        Assert.True(card.IsPreviewMaterialized);
    }

    [Theory]
    [InlineData(1, 1, 1)]
    [InlineData(260, 1, 1)]
    [InlineData(1100, 1, 4)]
    [InlineData(1100, 1.6, 2)]
    [InlineData(100000, 1, 12)]
    public void GalleryColumnCount_TracksWidthAndZoom(double width, double zoom, int expected)
    {
        Assert.Equal(expected, ModuleGalleryViewModel.CalculateGalleryColumnCount(width, zoom));
    }
}
