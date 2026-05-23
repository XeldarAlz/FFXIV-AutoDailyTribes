using AutoTribeQuests.Core.External;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

namespace AutoTribeQuests.Windows.Sections;

// Compact warning that renders only when a required dependency is missing.
// "Manage" jumps to the dedicated Dependencies window which has install
// buttons + repo-URL copy affordance.
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
        using (ImRaii.Child("##depbanner", new(-1, 46), true))
        {
            ImGui.AlignTextToFramePadding();
            using (ImRaii.PushFont(UiBuilder.IconFont))
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.AccentRose))
                ImGui.TextUnformatted(FontAwesomeIcon.ExclamationTriangle.ToIconString());

            ImGui.SameLine();
            var names = string.Join(", ", missing.Select(p => ExternalPlugins.Catalog[p].DisplayName));
            ImGui.AlignTextToFramePadding();
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.AccentRose))
                ImGui.TextUnformatted($"Missing required: {names}");

            // Right-align the Manage button: GetContentRegionMax().X is the
            // child window's inner right edge; subtract the button's width.
            const string label = "Manage";
            var btnW = ImGui.CalcTextSize(label).X + ImGui.GetStyle().FramePadding.X * 2 + 4f;
            ImGui.SameLine(ImGui.GetContentRegionMax().X - btnW);
            if (ImGui.Button(label))
                plugin.ToggleDependenciesUi();
        }
    }
}
