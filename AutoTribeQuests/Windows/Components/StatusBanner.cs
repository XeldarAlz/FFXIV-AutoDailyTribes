using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoTribeQuests.Windows.Components;

// One-line "idle / running / error" indicator for the header row. Pulses
// when running so peripheral vision can spot active automation.
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
