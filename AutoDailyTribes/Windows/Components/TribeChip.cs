using AutoDailyTribes.Core.Tribes;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoDailyTribes.Windows.Components;

// Compact, non-interactive pill for unlocked tribes still under the rank needed for dailies.
// Dim by design so the eye stays on the "Ready to run" cards above.
internal static class TribeChip
{
    private static readonly Vector2 Pad = new(9, 3);

    public static float Width(TribeInfo tribe)
        => ImGui.CalcTextSize(Label(tribe)).X + Pad.X * 2f * ImGuiHelpers.GlobalScale;

    public static void Draw(TribeInfo tribe)
    {
        var s = ImGuiHelpers.GlobalScale;
        var (label, color) = Describe(tribe);
        var pad = Pad * s;
        var size = ImGui.CalcTextSize(label) + pad * 2f;
        var origin = ImGui.GetCursorScreenPos();
        var end = origin + size;

        var dl = ImGui.GetWindowDrawList();
        dl.AddRectFilled(origin, end, ImGui.GetColorU32(Styling.CardBgSoft), 6f * s);
        dl.AddRect(origin, end, ImGui.GetColorU32(Styling.WithAlpha(color, 0.45f)), 6f * s);

        ImGui.Dummy(size);
        var after = ImGui.GetCursorPos();
        ImGui.SetCursorScreenPos(origin + pad);
        using (ImRaii.PushColor(ImGuiCol.Text, color))
            ImGui.TextUnformatted(label);
        ImGui.SetCursorPos(after);

        if (ImGui.IsMouseHoveringRect(origin, end))
        {
            using var tt = ImRaii.Tooltip();
            ImGui.TextUnformatted($"Reach rank {tribe.MinRankForDailies} to run dailies.");
        }
    }

    private static string Label(TribeInfo tribe) => Describe(tribe).label;

    private static (string label, Vector4 color) Describe(TribeInfo tribe)
        => ($"{tribe.Name} · rank {tribe.MinRankForDailies}", Styling.TextDim);
}
