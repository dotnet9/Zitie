using Xunit;
using Zitie.Desktop.Services;

namespace Zitie.Desktop.Tests;

public sealed class StrokeOrderCatalogTests
{
    [Fact]
    public void FindForGlyphs_LoadsOfflineStrokePaths()
    {
        var catalog = new StrokeOrderCatalog();

        var orders = catalog.FindForGlyphs(["春", "冬", "风"]);

        Assert.True(orders["春"].Strokes.Count >= 9);
        Assert.True(orders["冬"].Strokes.Count >= 5);
        Assert.True(orders["风"].Strokes.Count >= 4);
        Assert.StartsWith("M ", orders["春"].Strokes[0], StringComparison.Ordinal);
    }
}
