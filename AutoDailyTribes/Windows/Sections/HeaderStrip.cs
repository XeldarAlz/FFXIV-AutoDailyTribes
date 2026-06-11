using AutoDailyTribes.Core.External;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

namespace AutoDailyTribes.Windows.Sections;

internal static class HeaderStrip
{
    public static void Draw(Plugin plugin)
    {
        ImGui.AlignTextToFramePadding();
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
            ImGui.TextUnformatted("AUTO DAILY TRIBES");

        DrawIconsInline(plugin);
        ImGui.Separator();
    }

    // Right-aligns the plug/info/gear buttons on the current line. The plug tints rose/amber when a
    // required plugin is missing / TextAdvance is disabled, so the toolbar doubles as a health light.
    public static void DrawIconsInline(Plugin plugin)
    {
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
    }
}
