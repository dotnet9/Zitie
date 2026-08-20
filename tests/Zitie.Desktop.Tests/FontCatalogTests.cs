using Xunit;
using Zitie.Desktop.Models;
using Zitie.Desktop.Services;

namespace Zitie.Desktop.Tests;

public sealed class FontCatalogTests
{
    [Theory]
    [InlineData("KaiTi", SheetFontCategory.StandardKai, FontStrokeStyle.Clear)]
    [InlineData("方正楷体", SheetFontCategory.StandardKai, FontStrokeStyle.Clear)]
    [InlineData("LXGW WenKai", SheetFontCategory.Handwriting, FontStrokeStyle.Natural)]
    [InlineData("Microsoft YaHei", SheetFontCategory.Print, FontStrokeStyle.NotEmphasized)]
    [InlineData("SimSun", SheetFontCategory.Print, FontStrokeStyle.NotEmphasized)]
    [InlineData("Unknown Font", SheetFontCategory.Other, FontStrokeStyle.Unknown)]
    public void Classify_DescribesPracticeSuitability(
        string name,
        SheetFontCategory expectedCategory,
        FontStrokeStyle expectedStrokeStyle)
    {
        var option = FontCatalog.Classify(name);

        Assert.Equal(expectedCategory, option.Category);
        Assert.Equal(expectedStrokeStyle, option.StrokeStyle);
        Assert.Equal(option.DisplayName, option.ToString());
    }

    [Fact]
    public void Catalog_AlwaysProvidesChineseFallback()
    {
        var catalog = new FontCatalog();
        var fallback = catalog.Find("LXGW WenKai");

        Assert.NotNull(fallback);
        Assert.True(fallback.IsBundled);
        Assert.False(string.IsNullOrWhiteSpace(catalog.DefaultFontFamily));
    }
}
