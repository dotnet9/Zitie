namespace Zitie.Core.Models;

/// <summary>
///     生字字帖规格：一段文本按格子排布练习。
/// </summary>
public sealed record CharacterSheetSpec
{
    /// <summary>练习文本，逐个非空白字符生成练习组。</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>特殊题型版式；Standard 使用普通字帖排版。</summary>
    public PracticeLayoutKind PracticeLayout { get; init; }

    /// <summary>正文区域作为固定空白版式输出，不根据练习文本生成范字或描红字。</summary>
    public bool BlankContentLayout { get; init; }

    /// <summary>练习内容不足一页时，用空白格补满正文区域。</summary>
    public bool FillContentAreaWithBlankCells { get; init; }

    public GridKind Grid { get; init; } = GridKind.Mi;

    public PracticeMode Mode { get; init; } = PracticeMode.Trace;

    /// <summary>
    ///     每行格数。0 表示按格子大小自动计算；桌面端默认提供 12 / 16 两档。
    /// </summary>
    public int CellsPerLine { get; init; }

    /// <summary>固定版式列数；0 表示由页面宽度或每行格数自动决定。</summary>
    public int LayoutColumns { get; init; }

    /// <summary>固定版式行数；0 表示由页面高度自动决定。</summary>
    public int LayoutRows { get; init; }

    /// <summary>横排练习行后、竖排练习列后生成的空格子行/列数，范围 0-10；格线保留，格子角色为 Blank。</summary>
    public int BlankCellLineCount { get; init; }

    /// <summary>每个字的总格数（含范字与描红格）。</summary>
    public int RepeatsPerChar { get; init; } = 5;

    /// <summary>描红模式下浅色描红字的数量。</summary>
    public int TraceSlotCount { get; init; } = 2;

    /// <summary>格子边长（毫米）。</summary>
    public double GridSizeMm { get; init; } = 14;

    /// <summary>格子间距（毫米）。</summary>
    public double GridGapMm { get; init; } = 2;

    /// <summary>练习组之间、横排各行之间的额外间距（毫米）。</summary>
    public double GroupGapMm { get; init; } = 2;

    /// <summary>页眉标题；null 时不显示标题行。</summary>
    public string? Title { get; init; }

    /// <summary>页头预设，用于区分普通生字帖、诗词帖和自定义页头。</summary>
    public SheetHeaderPreset HeaderPreset { get; init; } = SheetHeaderPreset.TitleAndFields;

    /// <summary>自定义页头文本；为空时使用默认“班级/姓名/日期”填写栏。</summary>
    public string? HeaderTextTemplate { get; init; }

    /// <summary>首页是否显示“班级 / 姓名 / 日期”填写栏。</summary>
    public bool ShowHeaderFields { get; init; } = true;

    /// <summary>排布方向：横排或竖排（传统书法帖式）。</summary>
    public SheetOrientation Orientation { get; init; } = SheetOrientation.Horizontal;

    /// <summary>题头显示作者行：标题下方追加“朝代 · 作者”。</summary>
    public bool ShowPoemHeader { get; init; }

    /// <summary>页面四周绘制装饰边框（双线）。</summary>
    public bool FrameBorder { get; init; }

    /// <summary>页面背景纸张模板（白底 / 红格纸 / 信纸 / 宣纸）。</summary>
    public SheetBackground Background { get; init; } = SheetBackground.Plain;

    /// <summary>页面背景底色；为空时由背景模板决定。</summary>
    public string? BackgroundColor { get; init; }

    /// <summary>页面底纹线颜色；为空时由背景模板决定。</summary>
    public string? BackgroundLineColor { get; init; }

    /// <summary>页面底纹行距（毫米）；为空时由背景模板决定。</summary>
    public double? BackgroundLineSpacingMm { get; init; }

    /// <summary>页面背景插画在模板包内的逻辑路径，用于保存模板包。</summary>
    public string? BackgroundArtwork { get; init; }

    /// <summary>页面背景插画 SVG 内容；渲染层直接从内存读取，不依赖展开文件。</summary>
    public string? BackgroundArtworkSvg { get; init; }

    /// <summary>作者名（题头用）。</summary>
    public string? Author { get; init; }

    /// <summary>朝代（题头用）。</summary>
    public string? Dynasty { get; init; }

    /// <summary>
    ///     按词分组排版：true 时以空白分隔的词为排版单位，一个词占据连续格位；
    ///     false 时每个字符一组（描红/临摹角色序列）。
    ///     竖排时忽略此值，按句分组（一句一列）。
    /// </summary>
    public bool GroupByWord { get; init; }

    /// <summary>在格子顶部标注拼音（需配合 <see cref="PinyinByGlyph" />）。</summary>
    public bool ShowPinyin { get; init; }

    /// <summary>只显示拼音不显示范字，用于“看拼音写词语”。</summary>
    public bool PinyinOnly { get; init; }

    /// <summary>
     ///     描红字颜色（十六进制，如 "#DF9C93"）；null 时用渲染主题默认色。
     ///     浅色适合打印后手描，深色适合屏幕直接临摹。
     /// </summary>
    public string? TraceColor { get; init; }

    /// <summary>描红字迹深浅预设。</summary>
    public TraceIntensity TraceIntensity { get; init; } = TraceIntensity.Medium;

    /// <summary>格线颜色（十六进制或 Avalonia 颜色名）；null 时使用渲染主题。</summary>
    public string? GridColor { get; init; }

    /// <summary>范字/正文颜色（十六进制或 Avalonia 颜色名）；null 时使用渲染主题。</summary>
    public string? TextColor { get; init; }

    /// <summary>字帖内容字体族名称；为空时由渲染层选择内置或系统回退字体。</summary>
    public string? FontFamilyName { get; init; }

    /// <summary>空心双钩字：范字以轮廓线描出，供填墨临摹。</summary>
    public bool HollowGlyph { get; init; }

    /// <summary>字符 → 拼音（带声调），缺失时该格不标注。</summary>
    public IReadOnlyDictionary<string, string>? PinyinByGlyph { get; init; }

    public PageSettings Page { get; init; } = PageSettings.A4;
}
