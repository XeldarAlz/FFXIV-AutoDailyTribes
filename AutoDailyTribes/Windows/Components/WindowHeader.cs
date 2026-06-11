using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

namespace AutoDailyTribes.Windows.Components;

internal static class WindowHeader
{
    public static void Draw(string title, string? subtitle = null)
    {
        ImGui.SetWindowFontScale(1.35f);
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextStrong))
            ImGui.TextUnformatted(title);
        ImGui.SetWindowFontScale(1f);

        if (!string.IsNullOrEmpty(subtitle))
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextMuted))
                ImGui.TextUnformatted(subtitle);

        Styling.VSpace(5);
        ImGui.Separator();
        Styling.VSpace(6);
    }
}
