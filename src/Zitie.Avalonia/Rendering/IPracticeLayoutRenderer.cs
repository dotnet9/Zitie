using SkiaSharp;
using Zitie.Core.Layout;
using Zitie.Core.Models;

namespace Zitie.Avalonia.Rendering;

internal interface IPracticeLayoutRenderer
{
    PracticeLayoutKind Kind { get; }

    void Render(SKCanvas canvas, CharacterSheetSpec spec, SheetPage page, SheetRenderTheme theme);
}
