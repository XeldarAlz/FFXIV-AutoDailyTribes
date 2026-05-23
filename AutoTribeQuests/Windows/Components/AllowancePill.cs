using AutoTribeQuests.Core.Tribes;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoTribeQuests.Windows.Components;

// "N / 3" pill, colored by remaining capacity. Sits on the right side of a card.
internal static class AllowancePill
{
    public static void Draw(TribeInfo tribe)
    {
        var taken = tribe.AlreadyAcceptedToday.Length;
        var label = $"{taken} / {Constants.MaxAcceptsPerTribe}";

        var color = taken switch
        {
            >= Constants.MaxAcceptsPerTribe => Styling.AccentMint,
            > 0 => Styling.AccentAmber,
            _ => Styling.TextDim,
        };

        var pad = new Vector2(8, 2) * ImGuiHelpers.GlobalScale;
        var textSize = ImGui.CalcTextSize(label);
        var size = textSize + pad * 2;
        var origin = ImGui.GetCursorScreenPos();
        var end = origin + size;

        var drawList = ImGui.GetWindowDrawList();
        drawList.AddRectFilled(origin, end, ImGui.GetColorU32(Styling.CardBgSoft), 9f);
        drawList.AddRect(origin, end, ImGui.GetColorU32(color), 9f);

        ImGui.Dummy(size);
        var prev = ImGui.GetCursorPos();
        ImGui.SetCursorScreenPos(origin + pad);
        using (ImRaii.PushColor(ImGuiCol.Text, color))
            ImGui.TextUnformatted(label);
        ImGui.SetCursorPos(prev);
    }
}
