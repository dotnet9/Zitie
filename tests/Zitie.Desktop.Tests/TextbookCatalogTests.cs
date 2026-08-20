using Xunit;
using Zitie.Desktop.Services;

namespace Zitie.Desktop.Tests;

public sealed class TextbookCatalogTests
{
    [Fact]
    public void BundledCatalog_CoversPrimaryJuniorAndSeniorGrades()
    {
        var catalog = new TextbookCatalog();

        Assert.True(catalog.Items.Count >= 1900);
        var grades = catalog.Items.SelectMany(static item => item.Grades).ToHashSet(StringComparer.Ordinal);
        foreach (var grade in new[]
                 {
                     "一年级", "二年级", "三年级", "四年级", "五年级", "六年级",
                     "初一", "初二", "初三",
                     "高一", "高二", "高三"
                 })
            Assert.Contains(grade, grades);
    }

    [Fact]
    public void BundledCatalog_ExposesEditionChoicesBySubjectAndGrade()
    {
        var catalog = new TextbookCatalog();

        Assert.Contains(catalog.Items, item =>
            item.Subject == "语文" &&
            item.Edition == "统编版" &&
            item.Grades.Contains("一年级", StringComparer.Ordinal));
        Assert.Contains(catalog.EditionsFor("英语", "三年级"), static edition => !string.IsNullOrWhiteSpace(edition));
    }
}
