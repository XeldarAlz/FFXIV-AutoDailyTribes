using AutoDailyTribes.Core.Tribes;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoDailyTribes.Windows.Components;

// Compact queue entry for the running view: icon + name + remaining count in one pill.
internal static class QueueChip
{
    private const float PadX = 9f;
    private const float Gap = 7f;

    public static float Width(TribeInfo tribe)
    {
        var s = ImGuiHelpers.GlobalScale;
        var iconSize = Layout.QueueChipHeight * s * 0.6f;
        return (PadX * 2f + Gap * 2f) * s + iconSize
            + ImGui.CalcTextSize(tribe.Name).X
            + ImGui.CalcTextSize(AllowancePill.GetLabel(tribe)).X;
    }

    public static void Draw(TribeInfo tribe)
    {
        var s = ImGuiHelpers.GlobalScale;
        var height = Layout.QueueChipHeight * s;
        var padX = PadX * s;
        var gap = Gap * s;
        var iconSize = height * 0.6f;
        var count = AllowancePill.GetLabel(tribe);

        var origin = ImGui.GetCursorScreenPos();
        var width = Width(tribe);
        var end = origin + new Vector2(width, height);

        var dl = ImGui.GetWindowDrawList();
        dl.AddRectFilled(origin, end, ImGui.GetColorU32(Styling.CardBgSoft), 6f * s);
        dl.AddRect(origin, end, ImGui.GetColorU32(Styling.WithAlpha(Styling.BorderDim, 0.55f)), 6f * s);

        var midY = origin.Y + height * 0.5f;
        ImGui.SetCursorScreenPos(new Vector2(origin.X + padX, midY - iconSize * 0.5f));
        TribeIcon.Draw(tribe, iconSize);

        var lineH = ImGui.GetTextLineHeight();
        ImGui.SetCursorScreenPos(new Vector2(origin.X + padX + iconSize + gap, midY - lineH * 0.5f));
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextSecondary))
            ImGui.TextUnformatted(tribe.Name);

        var countW = ImGui.CalcTextSize(count).X;
        ImGui.SetCursorScreenPos(new Vector2(end.X - padX - countW, midY - lineH * 0.5f));
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
            ImGui.TextUnformatted(count);

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(new Vector2(width, height));

        if (ImGui.IsItemHovered())
            Tooltip.For(RankBadge.RankLabel(tribe));
    }
}
