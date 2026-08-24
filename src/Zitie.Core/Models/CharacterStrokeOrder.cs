namespace Zitie.Core.Models;

/// <summary>单个汉字的 SVG 笔画路径，按书写顺序排列。</summary>
public sealed record CharacterStrokeOrder(IReadOnlyList<string> Strokes);
