using AutoDailyTribes.Core.External;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

namespace AutoDailyTribes.Windows.Sections;

internal static class DependencyBanner
{
    public static void Draw(Plugin plugin)
    {
        var missing = ExternalPlugins.All
            .Where(p => ExternalPlugins.Catalog[p].Required && !ExternalPlugins.IsInstalled(p))
            .ToArray();
        if (missing.Length > 0)
        {
            var names = string.Join(", ", missing.Select(p => ExternalPlugins.Catalog[p].DisplayName));
            DrawRow(plugin, "##depbanner", Styling.AccentRose, $"Missing required: {names}");
            return;
        }

        if (ExternalPlugins.IsInstalledButDisabled(ExternalPlugin.TextAdvance))
            DrawRow(plugin, "##depbanner_disabled", Styling.AccentAmber,
                "TextAdvance is disabled — quest dialogue won't advance and dailies will stall.");
    }

    private static void DrawRow(Plugin plugin, string id, System.Numerics.Vector4 color, string message)
    {
        using (ImRaii.PushColor(ImGuiCol.Border, color))
        using (ImRaii.PushColor(ImGuiCol.ChildBg, Styling.CardBgSoft))
        using (ImRaii.PushStyle(ImGuiStyleVar.ChildBorderSize, 1.5f))
        using (ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 6f))
        using (ImRaii.Child(id, new(-1, 46), true))
        {
            ImGui.AlignTextToFramePadding();
            using (ImRaii.PushFont(UiBuilder.IconFont))
            using (ImRaii.PushColor(ImGuiCol.Text, color))
                ImGui.TextUnformatted(FontAwesomeIcon.ExclamationTriangle.ToIconString());

            ImGui.SameLine();
            ImGui.AlignTextToFramePadding();
            using (ImRaii.PushColor(ImGuiCol.Text, color))
                ImGui.TextUnformatted(message);

            const string label = "Manage";
            var btnW = ImGui.CalcTextSize(label).X + ImGui.GetStyle().FramePadding.X * 2 + 4f;
            ImGui.SameLine(ImGui.GetContentRegionMax().X - btnW);
            if (ImGui.Button(label))
                plugin.ToggleDependenciesUi();
        }
    }
}
