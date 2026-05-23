using AutoTribeQuests.Core.Tribes;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoTribeQuests.Windows.Components;

// Compact rank display: "Rank 4" label with a thin reputation progress bar
// underneath. Locked tribes get a dim "locked" line instead.
internal static class RankBadge
{
    public static void Draw(TribeInfo tribe)
    {
        if (!tribe.Unlocked)
        {
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextMuted))
                ImGui.TextUnformatted("Locked");
            return;
        }

        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextSecondary))
            ImGui.TextUnformatted($"Rank {tribe.Rank}");

        if (tribe.RepMax > 0)
        {
            var fraction = Math.Clamp((float)tribe.RepCur / tribe.RepMax, 0f, 1f);
            DrawRepBar(fraction);
        }
    }

    private static void DrawRepBar(float fraction)
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
            drawList.AddRectFilled(origin, fillEnd, ImGui.GetColorU32(Styling.AccentTeal), 2f);
        }
        ImGui.Dummy(new Vector2(width, height));
    }
}
