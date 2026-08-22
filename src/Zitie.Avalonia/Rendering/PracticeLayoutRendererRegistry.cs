using Zitie.Core.Models;

namespace Zitie.Avalonia.Rendering;

internal static class PracticeLayoutRendererRegistry
{
    private static readonly IReadOnlyDictionary<PracticeLayoutKind, IPracticeLayoutRenderer> Renderers =
        new IPracticeLayoutRenderer[]
        {
            new BracketWordRowsRenderer(),
            new BracketWordColumnsRenderer(),
            new BracketGridWordsRenderer(),
            new BracketPinyinColumnsRenderer(),
            new CharacterWordsPoemRenderer()
        }.ToDictionary(static renderer => renderer.Kind);

    public static bool TryGet(PracticeLayoutKind kind, out IPracticeLayoutRenderer renderer)
    {
        return Renderers.TryGetValue(kind, out renderer!);
    }
}
