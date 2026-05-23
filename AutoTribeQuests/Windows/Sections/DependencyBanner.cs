using AutoTribeQuests.Core.External;
using AutoTribeQuests.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

namespace AutoTribeQuests.Windows.Sections;

// Compact warning that renders only when a required dependency is missing.
// Click jumps to the About window's deps grid which has the install buttons.
internal static class DependencyBanner
{
    public static void Draw(Plugin plugin)
    {
        var missing = ExternalPlugins.All
            .Where(p => ExternalPlugins.Catalog[p].Required && !ExternalPlugins.IsInstalled(p))
            .ToArray();
        if (missing.Length == 0) return;

        using (ImRaii.PushColor(ImGuiCol.Border, Styling.AccentRose))
        using (ImRaii.PushColor(ImGuiCol.ChildBg, Styling.CardBgSoft))
        using (ImRaii.PushStyle(ImGuiStyleVar.ChildBorderSize, 1.5f))
        using (ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 6f))
        using (ImRaii.Child("##depbanner", new(-1, 38), true))
        {
            using (ImRaii.PushFont(UiBuilder.IconFont))
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.AccentRose))
                ImGui.TextUnformatted(FontAwesomeIcon.ExclamationTriangle.ToIconString());
            ImGui.SameLine();

            var names = string.Join(", ", missing.Select(p => ExternalPlugins.Catalog[p].DisplayName));
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.AccentRose))
                ImGui.TextUnformatted($"Missing required: {names}");

            ImGui.SameLine(ImGui.GetContentRegionAvail().X);
            if (ImGui.SmallButton("Open About"))
                plugin.ToggleAboutUi();
        }
    }
}
