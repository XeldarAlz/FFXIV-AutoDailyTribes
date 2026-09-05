using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoDailyTribes.Windows.Components;

internal static class Chip
{
    private const float PaddingX = 10f;

    public static float Width(string label)
        => ImGui.CalcTextSize(label).X + PaddingX * 2f * ImGuiHelpers.GlobalScale;

    public static bool Draw(string label, string id, bool active, Vector4 accent)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var background = active ? Styling.WithAlpha(accent, 0.18f) : Styling.WithAlpha(Styling.TextMuted, 0.08f);
        var hovered = active ? Styling.WithAlpha(accent, 0.32f) : Styling.WithAlpha(Styling.TextMuted, 0.20f);
        var pressed = active ? Styling.WithAlpha(accent, 0.45f) : Styling.WithAlpha(Styling.TextMuted, 0.32f);
        var border = active ? Styling.WithAlpha(accent, 0.75f) : Styling.WithAlpha(Styling.BorderDim, 0.75f);
        var text = active ? accent : Styling.TextMuted;

        using (ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 1f)
            .Push(ImGuiStyleVar.FrameRounding, 11f * scale)
            .Push(ImGuiStyleVar.FramePadding, new Vector2(PaddingX, 3f) * scale))
        using (ImRaii.PushColor(ImGuiCol.Button, background)
            .Push(ImGuiCol.ButtonHovered, hovered)
            .Push(ImGuiCol.ButtonActive, pressed)
            .Push(ImGuiCol.Border, border)
            .Push(ImGuiCol.Text, text))
        {
            return ImGui.Button($"{label}##{id}");
        }
    }
}
