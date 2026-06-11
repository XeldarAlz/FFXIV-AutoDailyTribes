using AutoDailyTribes.Core.External;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoDailyTribes.Windows.Sections;

internal static class HeaderStrip
{
    public static void Draw(Plugin plugin)
    {
        var (icon, color, greeting, tagline) = Greeting();

        ImGui.AlignTextToFramePadding();
        using (ImRaii.PushFont(UiBuilder.IconFont))
        using (ImRaii.PushColor(ImGuiCol.Text, color))
            ImGui.TextUnformatted(icon.ToIconString());

        ImGui.SameLine(0, 7f);
        ImGui.AlignTextToFramePadding();
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextSecondary))
            ImGui.TextUnformatted($"{greeting}, {tagline}");

        ImGui.SameLine();
        DrawIconsInline(plugin);
    }

    private static (FontAwesomeIcon icon, Vector4 color, string greeting, string tagline) Greeting() => DateTime.Now.Hour switch
    {
        >= 5 and < 12  => (FontAwesomeIcon.Sun,       Styling.AccentAmber,    "Good morning",   "ready for your dailies?"),
        >= 12 and < 17 => (FontAwesomeIcon.Sun,       Styling.AccentAmber,    "Good afternoon", "ready for your dailies?"),
        >= 17 and < 22 => (FontAwesomeIcon.CloudMoon, Styling.AccentTealSoft, "Good evening",   "ready for your dailies?"),
        _              => (FontAwesomeIcon.Moon,      Styling.AccentBlue,     "Late night",     "still on dailies?"),
    };

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
            ImGui.SetCursorPosX(ImGui.GetWindowContentRegionMax().X - btnW * 3 - spacingX * 2);

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
