using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

namespace AutoTribeQuests.Windows.Components;

internal static class StatusBanner
{
    public static void Draw(bool running, string status)
    {
        var icon = running ? FontAwesomeIcon.Spinner : FontAwesomeIcon.Pause;
        var color = running
            ? Styling.PulseColor(Styling.AccentTeal, Styling.AccentTealSoft, Styling.PulseMedium)
            : Styling.TextDim;

        using (ImRaii.PushFont(UiBuilder.IconFont))
        using (ImRaii.PushColor(ImGuiCol.Text, color))
            ImGui.TextUnformatted(icon.ToIconString());

        ImGui.SameLine();
        using (ImRaii.PushColor(ImGuiCol.Text, color))
            ImGui.TextUnformatted(status);
    }
}
