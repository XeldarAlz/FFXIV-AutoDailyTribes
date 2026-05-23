using AutoDailyTribes.Core;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

namespace AutoDailyTribes.Windows.Sections;

internal static class Footer
{
    public static void Draw()
    {
        ImGui.Separator();
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextMuted))
            ImGui.TextUnformatted($"Auto Daily Tribes — {AdtConstants.PrimaryCommand} / {AdtConstants.AliasCommand}");
    }
}
