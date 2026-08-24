using Xunit;
using Zitie.Desktop.Services;

namespace Zitie.Desktop.Tests;

public sealed class PinyinCatalogTests
{
    [Fact]
    public void LoadsBracketPracticeTemplateGlyphPinyin()
    {
        var catalog = new PinyinCatalog();

        Assert.Equal("pō", catalog.PinyinByGlyph["坡"]);
        Assert.Equal("qiú", catalog.PinyinByGlyph["球"]);
        Assert.Equal("zhāo", catalog.PinyinByGlyph["招"]);
        Assert.Equal("hū", catalog.PinyinByGlyph["呼"]);
        Assert.Equal("jīng", catalog.PinyinByGlyph["晶"]);
    }

    [Fact]
    public void FallsBackToBuiltInPinyinTableForManualInputGlyphs()
    {
        var catalog = new PinyinCatalog();

        Assert.Equal("ài", catalog.PinyinByGlyph["爱"]);
        Assert.Equal("zhào", catalog.PinyinByGlyph["赵"]);
        Assert.Equal("sēn", catalog.PinyinByGlyph["森"]);
    }
}
