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

    public PageSettings Page { get; init; } = PageSettings.A4;
}
