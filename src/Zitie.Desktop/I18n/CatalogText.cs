namespace Zitie.Desktop.I18n;

/// <summary>模板分类保留原始标识用于搜索和配方匹配，显示名通过资源随语言切换。</summary>
public static class CatalogText
{
    public static string DisplayCategory(string? value)
    {
        var key = value switch
        {
        "汉字" => Language.CategoryChinese,
        "拼音" => Language.CategoryPinyin,
        "数字" => Language.CategoryNumbers,
        "英文" => Language.CategoryEnglish,
        "有笔顺" => Language.CategoryStrokes,
        "组词" => Language.CategoryWords,
        "书法" => Language.CategoryCalligraphy,
        "生字" => Language.CategoryNewWords,
        "脱格" => Language.CategoryBorderless,
        "文章" => Language.CategoryArticles,
        "试卷" => Language.CategoryExams,
        "测试卡" => Language.CategoryTestCards,
        "名字" => Language.CategoryNames,
        "封面" => Language.CategoryCovers,
        "基础" => Language.CategoryBasics,
        "蒙学" => Language.CategoryClassics,
        "诗词" => Language.CategoryPoetry,
        "主题" => Language.CategoryThemes,
            _ => null
        };
        return key is not null ? AppText.Get(key) : value ?? AppText.Get(Language.Custom);
    }
}
