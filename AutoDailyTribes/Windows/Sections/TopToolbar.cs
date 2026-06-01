using AutoDailyTribes.Core.External;
using AutoDailyTribes.Core.Tasks;
using AutoDailyTribes.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

namespace AutoDailyTribes.Windows.Sections;

internal static class TopToolbar
{
    public static void Draw(Plugin plugin, AutoTribeController controller)
    {
        StatusBanner.Draw(controller.Running, controller.Status);

        var plugLabel = FontAwesomeIcon.Plug.ToIconString();
        var infoLabel = FontAwesomeIcon.InfoCircle.ToIconString();
        var gearLabel = FontAwesomeIcon.Cog.ToIconString();

        var anyMissing = !ExternalPlugins.AllRequiredInstalled();
        var anyDisabled = ExternalPlugins.IsInstalledButDisabled(ExternalPlugin.TextAdvance);
        var plugColor = anyMissing ? Styling.AccentRose
            : anyDisabled ? Styling.AccentAmber
            : Styling.TextSecondary;

        bool plugClicked, infoClicked, gearClicked;
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            var framePadX = ImGui.GetStyle().FramePadding.X;
            var spacingX = ImGui.GetStyle().ItemSpacing.X;
            var btnW = ImGui.CalcTextSize(gearLabel).X + framePadX * 2;
            ImGui.SameLine(ImGui.GetWindowContentRegionMax().X - btnW * 3 - spacingX * 2);

            using (ImRaii.PushColor(ImGuiCol.Text, plugColor))
                plugClicked = ImGui.Button(plugLabel + "##deps");
            ImGui.SameLine();
            infoClicked = ImGui.Button(infoLabel + "##about");
            ImGui.SameLine();
            gearClicked = ImGui.Button(gearLabel + "##gear");
        }

        if (plugClicked) plugin.ToggleDependenciesUi();
        if (infoClicked) plugin.ToggleAboutUi();
        if (gearClicked) plugin.ToggleConfigUi();

        ImGui.Separator();
    }
}
