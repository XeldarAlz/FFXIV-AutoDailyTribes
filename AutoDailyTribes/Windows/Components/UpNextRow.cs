using AutoDailyTribes.Core.Tribes;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoDailyTribes.Windows.Components;

internal static class UpNextRow
{
    public static void Draw(TribeInfo tribe)
    {
        TribeStateReader.Refresh(tribe);

        var s = ImGuiHelpers.GlobalScale;
        var rowH = Layout.UpNextRowHeight * s;
        var origin = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var end = origin + new Vector2(width, rowH);
        var dl = ImGui.GetWindowDrawList();
        dl.AddRectFilled(origin, end, ImGui.GetColorU32(Styling.CardBgSoft), 5f);

        var padX = 10f * s;
        var midY = origin.Y + rowH * 0.5f;
        var iconSize = rowH * 0.52f;

        ImGui.SetCursorScreenPos(new Vector2(origin.X + padX, midY - iconSize * 0.5f));
        TribeIcon.Draw(tribe, iconSize);

        var lineH = ImGui.GetTextLineHeight();
        ImGui.SetCursorScreenPos(new Vector2(origin.X + padX + iconSize + 10f * s, midY - lineH * 0.5f));
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextStrong))
            ImGui.TextUnformatted(tribe.Name);

        var label = AllowancePill.GetLabel(tribe);
        var pillW = ImGui.CalcTextSize(label).X + 16f * s;
        var pillH = lineH + 4f * s;
        ImGui.SetCursorScreenPos(new Vector2(end.X - padX - pillW, midY - pillH * 0.5f));
        AllowancePill.Draw(tribe);

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(new Vector2(width, rowH));
    }
}
