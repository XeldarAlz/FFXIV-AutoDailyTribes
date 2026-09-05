using AutoDailyTribes.Core.External;
using AutoDailyTribes.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using System.Numerics;

namespace AutoDailyTribes.Windows.Shell;

internal static class NavRail
{
    private readonly record struct Entry(AppWindow.Page Page, FontAwesomeIcon Icon, string Id, string Label);

    private const float TopPad = 12f;
    private const float Gap = 8f;
    private const float IconHeight = 17f;

    private static readonly Entry[] entries =
    [
        new(AppWindow.Page.Tribes,   FontAwesomeIcon.Users,      "##adt_nav_tribes",   "Tribes"),
        new(AppWindow.Page.Settings, FontAwesomeIcon.SlidersH,   "##adt_nav_settings", "Settings"),
        new(AppWindow.Page.Plugins,  FontAwesomeIcon.Plug,       "##adt_nav_plugins",  "Plugins"),
        new(AppWindow.Page.About,    FontAwesomeIcon.InfoCircle, "##adt_nav_about",    "About"),
    ];

    public static AppWindow.Page? Draw(AppWindow.Page current, Plugin plugin)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var button = Layout.RailButton * scale;
        var gap = Gap * scale;
        var railOrigin = ImGui.GetCursorScreenPos();
        var avail = ImGui.GetContentRegionAvail().X;
        var x = railOrigin.X + (avail - button) * 0.5f;
        var startY = railOrigin.Y + TopPad * scale;
        var dl = ImGui.GetWindowDrawList();

        var selectedIndex = 0;
        for (var index = 0; index < entries.Length; index++)
        {
            if (entries[index].Page == current) selectedIndex = index;
        }

        var indicator = Motion.Approach(Motion.Key("##adt_rail_indicator"), selectedIndex, 16f);
        var indicatorY = startY + (button + gap) * indicator;
        var indicatorMin = new Vector2(x, indicatorY);
        var indicatorMax = indicatorMin + new Vector2(button, button);
        Paint.Glass(dl, indicatorMin, indicatorMax, 12f * scale, Styling.AccentTeal, 0.30f);
        Paint.Fill(dl, new Vector2(railOrigin.X, indicatorY + button * 0.25f), new Vector2(railOrigin.X + 3f * scale, indicatorY + button * 0.75f),
            Styling.AccentTeal, 2f * scale);

        var missingPlugins = !ExternalPlugins.AllRequiredInstalled();
        var running = plugin.Controller.Running;
        AppWindow.Page? clicked = null;

        for (var index = 0; index < entries.Length; index++)
        {
            var entry = entries[index];
            var y = startY + (button + gap) * index;
            ImGui.SetCursorScreenPos(new Vector2(x, y));
            var hit = Hit.Area(entry.Id, new Vector2(button, button));
            var hover = Motion.Hover(Motion.Key(entry.Id), hit.Hovered);
            var selected = index == selectedIndex;

            if (!selected && hover > 0.01f)
            {
                Paint.Fill(dl, new Vector2(x, y), new Vector2(x + button, y + button), Styling.WithAlpha(Styling.Surface2, 0.8f * hover), 12f * scale);
            }

            var center = new Vector2(x + button * 0.5f, y + button * 0.5f);
            var color = selected ? Styling.TextStrong : Vector4.Lerp(Styling.TextDim, Styling.TextSecondary, hover);
            ProgressRing.CenterIcon(center, entry.Icon, color, IconHeight * scale);

            DrawBadge(dl, entry.Page, center, button, missingPlugins, running);

            if (hit.Hovered) Tooltip.Show(entry.Label);
            if (hit.Clicked) clicked = entry.Page;
        }

        ImGui.SetCursorScreenPos(railOrigin);
        ImGui.Dummy(new Vector2(avail, TopPad * scale + (button + gap) * entries.Length));
        return clicked;
    }

    private static void DrawBadge(ImDrawListPtr dl, AppWindow.Page page, Vector2 center, float button, bool missingPlugins, bool running)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var badgeCenter = center + new Vector2(button * 0.30f, -button * 0.30f);
        var radius = 3.5f * scale;

        if (page == AppWindow.Page.Plugins && missingPlugins)
        {
            dl.AddCircleFilled(badgeCenter, radius + 1.5f * scale, Paint.Col(Styling.WindowBg));
            dl.AddCircleFilled(badgeCenter, radius, Paint.Col(Styling.AccentRose));
        }
        else if (page == AppWindow.Page.Tribes && running)
        {
            dl.AddCircleFilled(badgeCenter, radius + 1.5f * scale, Paint.Col(Styling.WindowBg));
            dl.AddCircleFilled(badgeCenter, radius, Paint.Col(Styling.PulseColor(Styling.AccentTeal, Styling.AccentTealSoft, Styling.PulseMedium)));
        }
    }
}
