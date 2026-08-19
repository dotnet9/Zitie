namespace Zitie.Core.Models;

/// <summary>字帖排布方向。</summary>
public enum SheetOrientation
{
    /// <summary>横排：行从上到下，每行从左到右（现代字帖）。</summary>
    Horizontal,

    /// <summary>竖排：列从右到左，每列自上而下（传统书法帖式）。</summary>
    Vertical
}
