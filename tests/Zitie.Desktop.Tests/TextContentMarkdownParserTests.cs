using Xunit;
using Zitie.Desktop.Services;

namespace Zitie.Desktop.Tests;

public sealed class TextContentMarkdownParserTests
{
    [Fact]
    public void Parse_MapsHeadingHierarchyMetadataAndBody()
    {
        const string markdown = """
                                # 语文
                                ## 三年级
                                ### 上册
                                #### 第一单元
                                ##### 山行

                                - 类型：课本诗词
                                - 教材：义务教育教科书
                                - 版本：统编版
                                - 朝代：唐
                                - 作者：杜牧
                                - 来源：《全唐诗》
                                - 版权：公共领域

                                远上寒山石径斜，白云生处有人家。  
                                停车坐爱枫林晚，霜叶红于二月花。
                                """;

        var result = new TextContentMarkdownParser().Parse(markdown, "test.md");

        var entry = Assert.Single(result.Entries);
        Assert.Empty(result.Diagnostics);
        Assert.Equal("语文", entry.Subject);
        Assert.Equal("三年级", entry.Grade);
        Assert.Equal("上册", entry.Semester);
        Assert.Equal("第一单元", entry.Unit);
        Assert.Equal("山行", entry.Title);
        Assert.Equal("课本诗词", entry.ResourceType);
        Assert.Equal("统编版", entry.Edition);
        Assert.Equal("杜牧", entry.Author);
        Assert.Contains("停车坐爱枫林晚", entry.Body);
    }

    [Fact]
    public void Parse_ReportsBrokenHierarchyAndSkipsEmptyEntry()
    {
        const string markdown = """
                                # 语文
                                ### 上册
                                ##### 空条目
                                """;

        var result = new TextContentMarkdownParser().Parse(markdown, "broken.md");

        Assert.Empty(result.Entries);
        Assert.Contains(result.Diagnostics, item =>
            item.Severity == TextContentDiagnosticSeverity.Error && item.Message.Contains("缺少 H2"));
        Assert.Contains(result.Diagnostics, item =>
            item.Severity == TextContentDiagnosticSeverity.Error && item.Message.Contains("缺少 H4"));
    }

    [Fact]
    public void Catalog_SkipsDuplicateEntriesAcrossFiles()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"zitie-content-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            const string markdown = """
                                    # 名言警句
                                    ## 通用
                                    ### 课外
                                    #### 励志
                                    ##### 劝学

                                    - 类型：名言警句

                                    业精于勤，荒于嬉。
                                    """;
            File.WriteAllText(Path.Combine(directory, "a.md"), markdown);
            File.WriteAllText(Path.Combine(directory, "b.md"), markdown);

            var catalog = new TextCatalog(directory);

            Assert.Single(catalog.Entries);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }
}
