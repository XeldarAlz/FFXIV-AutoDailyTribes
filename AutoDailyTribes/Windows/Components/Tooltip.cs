using Dalamud.Bindings.ImGui;

namespace AutoDailyTribes.Windows.Components;

internal static class Tooltip
{
    public static void For(string text)
    {
        if (!ImGui.IsItemHovered()) return;
        ImGui.BeginTooltip();
        ImGui.PushTextWrapPos(360f);
        ImGui.TextUnformatted(text);
        ImGui.PopTextWrapPos();
        ImGui.EndTooltip();
    }
}
