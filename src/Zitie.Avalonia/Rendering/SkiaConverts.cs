using Avalonia.Media;
using SkiaSharp;

namespace Zitie.Avalonia.Rendering;

internal static class SkiaConverts
{
    public static SKColor ToSKColor(this Color color)
    {
        return new SKColor(color.R, color.G, color.B, color.A);
    }
}
