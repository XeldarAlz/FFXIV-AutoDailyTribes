using AutoDailyTribes.Core;
using AutoDailyTribes.Core.Tribes;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoDailyTribes.Windows.Components;

internal static class RankBadge
{
    private static readonly string[] RankNames =
    [
        "Neutral",
        "Recognized",
        "Friendly",
        "Trusted",
        "Respected",
        "Honored",
        "Sworn",
        "Bloodsworn",
    ];

    public static void Draw(TribeInfo tribe)
    {
        var label = tribe.Unlocked ? $"Rank {tribe.Rank} - {RankName(tribe.Rank)}" : "Locked";
        var labelColor = tribe.Unlocked ? Styling.TextSecondary : Styling.TextMuted;

        using (ImRaii.PushColor(ImGuiCol.Text, labelColor))
            ImGui.TextUnformatted(label);

        var maxed = tribe.Unlocked && tribe.Rank >= AdtConstants.MaxTribeRank;
        var fraction = maxed
            ? 1f
            : tribe.RepMax > 0
                ? Math.Clamp((float)tribe.RepCur / tribe.RepMax, 0f, 1f)
                : 0f;
        DrawRepBar(fraction, tribe.Unlocked, maxed);
    }

    private static string RankName(int rank)
    {
        var idx = rank - 1;
        return idx >= 0 && idx < RankNames.Length ? RankNames[idx] : "";
    }

    private static void DrawRepBar(float fraction, bool active, bool maxed)
    {
        var drawList = ImGui.GetWindowDrawList();
        var origin = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var height = Layout.RankBarHeight * ImGuiHelpers.GlobalScale;
        var end = origin + new Vector2(width, height);

        drawList.AddRectFilled(origin, end, ImGui.GetColorU32(Styling.CardBgSoft), 3f);
        if (fraction > 0)
        {
            var fillEnd = new Vector2(origin.X + width * fraction, end.Y);
            var fillColor = maxed
                ? Styling.AccentAmber
                : active ? Styling.AccentTeal : Styling.BorderDim;
            drawList.AddRectFilled(origin, fillEnd, ImGui.GetColorU32(fillColor), 3f);
        }

        if (active)
        {
            var dark = ImGui.GetColorU32(new Vector4(0.08f, 0.06f, 0.04f, 1f));
            var label = maxed ? "MAX" : $"{(int)MathF.Round(fraction * 100f)}%";
            DrawCenteredBarText(drawList, origin, width, height, label, dark);
        }

        ImGui.Dummy(new Vector2(width, height));
    }

    private static void DrawCenteredBarText(
        ImDrawListPtr drawList, Vector2 origin, float width, float height,
        string text, uint color)
    {
        var size = ImGui.CalcTextSize(text);
        var pos = new Vector2(
            origin.X + (width - size.X) * 0.5f,
            origin.Y + (height - size.Y) * 0.5f);
        drawList.AddText(pos, color, text);
    }
}
