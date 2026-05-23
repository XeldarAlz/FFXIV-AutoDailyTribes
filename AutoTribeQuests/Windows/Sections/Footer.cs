using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

namespace AutoTribeQuests.Windows.Sections;

internal static class Footer
{
    public static void Draw()
    {
        ImGui.Separator();
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextMuted))
            ImGui.TextUnformatted($"Allied Tribes — {Constants.PrimaryCommand} / {Constants.AliasCommand}");
    }
}
