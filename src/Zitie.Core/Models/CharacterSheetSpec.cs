namespace Zitie.Core.Models;

/// <summary>
///     生字字帖规格：一段文本按格子排布练习。
/// </summary>
public sealed record CharacterSheetSpec
{
    /// <summary>练习文本，逐个非空白字符生成练习组。</summary>
    public string Text { get; init; } = string.Empty;

    public GridKind Grid { get; init; } = GridKind.Mi;

    public PracticeMode Mode { get; init; } = PracticeMode.Trace;

    /// <summary>每个字的总格数（含范字与描红格）。</summary>
    public int RepeatsPerChar { get; init; } = 5;

    /// <summary>描红模式下浅色描红字的数量。</summary>
    public int TraceSlotCount { get; init; } = 2;

    /// <summary>格子边长（毫米）。</summary>
    public double GridSizeMm { get; init; } = 14;

    /// <summary>格子间距（毫米）。</summary>
    public double GridGapMm { get; init; } = 2;

    /// <summary>页眉标题；null 时不显示标题行。</summary>
    public string? Title { get; init; }

    /// <summary>首页是否显示“班级 / 姓名 / 日期”填写栏。</summary>
    public bool ShowHeaderFields { get; init; } = true;

    /// <summary>
    ///     按词分组排版：true 时以空白分隔的词为排版单位，一个词占据连续格位；
    ///     false 时每个字符一组（描红/临摹角色序列）。
    /// </summary>
    public bool GroupByWord { get; init; }

    /// <summary>在格子顶部标注拼音（需配合 <see cref="PinyinByGlyph" />）。</summary>
    public bool ShowPinyin { get; init; }

    /// <summary>只显示拼音不显示范字，用于“看拼音写词语”。</summary>
    public bool PinyinOnly { get; init; }

    /// <summary>字符 → 拼音（带声调），缺失时该格不标注。</summary>
    public IReadOnlyDictionary<string, string>? PinyinByGlyph { get; init; }

    public PageSettings Page { get; init; } = PageSettings.A4;
}
