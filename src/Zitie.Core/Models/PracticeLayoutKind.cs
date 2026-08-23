namespace Zitie.Core.Models;

/// <summary>练习版式：标准格子排版之外的固定题型布局。</summary>
public enum PracticeLayoutKind
{
    /// <summary>普通字帖排版。</summary>
    Standard,

    /// <summary>一行一个词，后接多个括号组词空位。</summary>
    BracketWordRows,

    /// <summary>多列给字，每字后接括号组词空位。</summary>
    BracketWordColumns,

    /// <summary>词语方格加括号组词空位。</summary>
    BracketGridWords,

    /// <summary>拼音/生字小格加两行括号组词空位。</summary>
    BracketPinyinColumns,

    /// <summary>同步生字、组词训练和同步古诗三段组合版式。</summary>
    CharacterWordsPoem,

    /// <summary>五言古诗书法卡片：水墨背景、花形中框和 5×4 诗格。</summary>
    FiveCharacterPoemCalligraphy
}
