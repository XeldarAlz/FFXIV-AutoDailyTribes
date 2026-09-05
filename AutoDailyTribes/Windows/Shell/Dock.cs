using Dalamud.Bindings.ImGui;
using System.Numerics;

namespace AutoDailyTribes.Windows.Shell;

internal static class Dock
{
    public static void Background(ImDrawListPtr dl, Vector2 origin, Vector2 end, float windowRounding)
    {
        Paint.Gradient(dl, origin, end, Styling.WithAlpha(Styling.Surface1, 0.97f), Styling.WithAlpha(Styling.Surface0, 0.97f), windowRounding, ImDrawFlags.RoundCornersBottom);
        Paint.Hairline(dl, origin, new Vector2(end.X, origin.Y));
    }
}
