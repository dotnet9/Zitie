using Zitie.Core.Models;

namespace Zitie.Core.Layout;

public static class PracticeLayoutEngineRegistry
{
    private static readonly IReadOnlyDictionary<PracticeLayoutKind, IPracticeLayoutEngine> Engines =
        new IPracticeLayoutEngine[]
        {
            new BracketWordRowsLayoutEngine(),
            new BracketWordColumnsLayoutEngine(),
            new BracketGridWordsLayoutEngine(),
            new BracketPinyinColumnsLayoutEngine()
        }.ToDictionary(static engine => engine.Kind);

    public static bool TryGet(PracticeLayoutKind kind, out IPracticeLayoutEngine engine)
    {
        return Engines.TryGetValue(kind, out engine!);
    }
}
