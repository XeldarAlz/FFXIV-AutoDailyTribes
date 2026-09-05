using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using System.Numerics;

namespace AutoDailyTribes.Windows.Components;

internal static class IconButton
{
    public static bool Draw(FontAwesomeIcon icon, string id, float size, Vector4? color = null, string? tooltip = null, bool enabled = true)
    {
        var origin = ImGui.GetCursorScreenPos();
        var box = new Vector2(size, size);
        var hit = Hit.Area(id, box, enabled);
        var hover = Motion.Hover(Motion.Key(id), hit.Hovered);
        var dl = ImGui.GetWindowDrawList();
        var center = origin + box * 0.5f;

        if (hover > 0.01f)
        {
            var alpha = hover * (hit.Held ? 1f : 0.85f);
            dl.AddCircleFilled(center, size * 0.5f, Paint.Col(Styling.WithAlpha(Styling.Surface3, alpha)));
        }

        var tint = color ?? Styling.TextSecondary;
        var glyphColor = enabled ? Vector4.Lerp(tint, Styling.TextStrong, hover * 0.55f) : Styling.TextMuted;
        TextDraw.IconCentered(icon, center, glyphColor);

        if (hit.Hovered && tooltip is not null) Tooltip.Show(tooltip);
        return hit.Clicked;
    }
}
