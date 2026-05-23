using AutoTribeQuests.Core.Tribes;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoTribeQuests.Windows.Components;

internal static class RankBadge
{
    public static void Draw(TribeInfo tribe)
    {
        var label = tribe.Unlocked ? $"Rank {tribe.Rank}" : "Locked";
        var labelColor = tribe.Unlocked ? Styling.TextSecondary : Styling.TextMuted;

        using (ImRaii.PushColor(ImGuiCol.Text, labelColor))
            ImGui.TextUnformatted(label);

        var fraction = tribe.RepMax > 0
            ? Math.Clamp((float)tribe.RepCur / tribe.RepMax, 0f, 1f)
            : 0f;
        DrawRepBar(fraction, tribe.Unlocked);
    }

    private static void DrawRepBar(float fraction, bool active)
    {
        var drawList = ImGui.GetWindowDrawList();
        var origin = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var height = Layout.RankBarHeight * ImGuiHelpers.GlobalScale;
        var end = origin + new Vector2(width, height);

        drawList.AddRectFilled(origin, end, ImGui.GetColorU32(Styling.CardBgSoft), 2f);
        if (fraction > 0)
        {
            var fillEnd = new Vector2(origin.X + width * fraction, end.Y);
            var fillColor = active ? Styling.AccentTeal : Styling.BorderDim;
            drawList.AddRectFilled(origin, fillEnd, ImGui.GetColorU32(fillColor), 2f);
        }
        ImGui.Dummy(new Vector2(width, height));
    }
}
