using AutoDailyTribes.Windows.Components;
using AutoDailyTribes.Windows.Sections.Config;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoDailyTribes.Windows.Pages;

internal sealed class SettingsPage
{
    private enum Tab { General, Jobs, AfterRun }

    private readonly record struct Entry(Tab Tab, string Label, FontAwesomeIcon Icon, string Subtitle);

    private const string Title = "Settings";

    private static readonly Entry[] entries =
    [
        new(Tab.General,  "General",       FontAwesomeIcon.Cog,        "How the window behaves and how the tribe list is arranged."),
        new(Tab.Jobs,     "Jobs",          FontAwesomeIcon.UserShield, "Which job runs each kind of tribe."),
        new(Tab.AfterRun, "After the run", FontAwesomeIcon.Terminal,   "Chat commands to fire once every queued tribe has finished."),
    ];

    private Tab activeTab = Tab.General;
    private bool resetScroll;

    public void Draw(Plugin plugin)
    {
        var cfg = plugin.Configuration;
        var scale = ImGuiHelpers.GlobalScale;
        var navWidth = Layout.SettingsNavWidth * scale;

        using (ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.Zero))
        {
            using (var nav = ImRaii.Child("##adt_settings_nav", new Vector2(navWidth, -1f), false, ImGuiWindowFlags.NoScrollbar))
            {
                if (nav) DrawNav();
            }

            ImGui.SameLine(0f, 18f * scale);

            using (var content = ImRaii.Child("##adt_settings_content", new Vector2(-1f, -1f), false, ImGuiWindowFlags.None))
            {
                if (content) DrawContent(cfg);
            }
        }
    }

    private void DrawNav()
    {
        var scale = ImGuiHelpers.GlobalScale;
        var origin = ImGui.GetCursorScreenPos();
        using (Fonts.PushTitle())
        {
            TextDraw.At(Title, new Vector2(origin.X + 6f * scale, origin.Y), Styling.TextStrong);
            ImGui.Dummy(new Vector2(ImGui.GetContentRegionAvail().X, TextDraw.Measure(Title).Y + 10f * scale));
        }

        for (var index = 0; index < entries.Length; index++)
        {
            var entry = entries[index];
            if (SidebarTab.Draw(entry.Label, entry.Icon, Styling.AccentTeal, activeTab == entry.Tab)) Select(entry.Tab);
        }
    }

    private void Select(Tab tab)
    {
        if (activeTab == tab) return;
        activeTab = tab;
        resetScroll = true;
    }

    private void DrawContent(Configuration cfg)
    {
        if (resetScroll)
        {
            ImGui.SetScrollY(0f);
            resetScroll = false;
        }

        var entry = entries[(int)activeTab];
        var scale = ImGuiHelpers.GlobalScale;

        using var reveal = Motion.PushSwitch("##adt_settings_tab", (int)activeTab);
        using var group = ImRaii.Group();
        ImGui.Dummy(new Vector2(0f, 2f * scale));
        PageHeader.Draw(entry.Label, entry.Subtitle);

        switch (activeTab)
        {
            case Tab.General: GeneralSettings.Draw(cfg); break;
            case Tab.Jobs: JobSettings.Draw(cfg); break;
            case Tab.AfterRun: PostRunSettings.Draw(cfg); break;
        }
    }
}
