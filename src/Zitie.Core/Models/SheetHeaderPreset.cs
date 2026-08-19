namespace Zitie.Core.Models;

/// <summary>首页页头信息显示方式。</summary>
public enum SheetHeaderPreset
{
    /// <summary>不显示页头。</summary>
    None,

    /// <summary>姓名、班级、日期填写栏。</summary>
    Fields,

    /// <summary>标题加填写栏。</summary>
    TitleAndFields,

    /// <summary>诗词题头：标题、朝代、作者。</summary>
    Poem,

    /// <summary>自定义页头文本。</summary>
    Custom
}
