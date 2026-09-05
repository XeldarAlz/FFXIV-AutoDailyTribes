using AutoDailyTribes.Core.Tasks;
using AutoDailyTribes.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using System.Numerics;

namespace AutoDailyTribes.Windows.Sections;

internal static class Headline
{
    private const float GreetingGap = 8f;
    private const float DetailGap = 6f;
    private const float RightGap = 18f;
    private const float ButtonHeight = 30f;

    private const string OpenPlugins = "Open Plugins";
    private const string GreetingMorning = "Good morning, ready for your dailies?";
    private const string GreetingAfternoon = "Good afternoon, ready for your dailies?";
    private const string GreetingEvening = "Good evening, ready for your dailies?";
    private const string GreetingNight = "Late night, still on dailies?";

    public static bool Draw(Configuration cfg, AutoTribeController ctrl)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var info = ReadyState.Resolve(cfg, ctrl);
        var origin = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var y = origin.Y;

        var (icon, color, greeting) = Greeting();
        using (Fonts.PushCaption())
        {
            var greetingSize = TextDraw.Measure(greeting);
            var iconSize = TextDraw.IconSize(icon);
            TextDraw.Icon(icon, new Vector2(origin.X + 1f * scale, y + (greetingSize.Y - iconSize.Y) * 0.5f), color);
            TextDraw.At(greeting, new Vector2(origin.X + iconSize.X + 8f * scale, y), Styling.TextDim);
            y += greetingSize.Y + GreetingGap * scale;
        }

        float titleHeight;
        using (Fonts.PushTitle())
            titleHeight = ImGui.GetTextLineHeight();
        var detailHeight = ImGui.GetTextLineHeight();
        var blockHeight = titleHeight + DetailGap * scale + detailHeight;
        var blockMidY = y + blockHeight * 0.5f;

        var rightWidth = DrawRightColumn(info, origin.X + width, blockMidY, out var openPlugins);
        var maxTextWidth = width - rightWidth - RightGap * scale;

        using (Fonts.PushTitle())
            TextDraw.At(TextDraw.Truncate(info.Title, maxTextWidth), new Vector2(origin.X, y), Styling.TextStrong);
        y += titleHeight + DetailGap * scale;
        TextDraw.At(TextDraw.Truncate(info.Detail, maxTextWidth), new Vector2(origin.X, y), Styling.TextDim);
        y += detailHeight;

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(new Vector2(width, y - origin.Y));
        return openPlugins;
    }

    private static (FontAwesomeIcon Icon, Vector4 Color, string Greeting) Greeting() => DateTime.Now.Hour switch
    {
        >= 5 and < 12  => (FontAwesomeIcon.Sun,       Styling.AccentAmber,    GreetingMorning),
        >= 12 and < 17 => (FontAwesomeIcon.Sun,       Styling.AccentAmber,    GreetingAfternoon),
        >= 17 and < 22 => (FontAwesomeIcon.CloudMoon, Styling.AccentTealSoft, GreetingEvening),
        _              => (FontAwesomeIcon.Moon,      Styling.AccentBlue,     GreetingNight),
    };

    private static float DrawRightColumn(ReadyState.Info info, float rightX, float midY, out bool openPlugins)
    {
        var scale = ImGuiHelpers.GlobalScale;
        openPlugins = false;

        if (info.Kind == ReadyState.Kind.SetupNeeded)
        {
            var buttonWidth = PillButton.Width(OpenPlugins, FontAwesomeIcon.Plug);
            ImGui.SetCursorScreenPos(new Vector2(rightX - buttonWidth, midY - ButtonHeight * scale * 0.5f));
            openPlugins = PillButton.Draw("##adt_open_plugins", OpenPlugins, Styling.AccentRose, PillButton.Emphasis.Tinted, FontAwesomeIcon.Plug, height: ButtonHeight);
            return buttonWidth;
        }

        var title = $"Reset in {Formatting.ResetCountdown()}";
        var detail = $"Daily reset at {Formatting.LocalResetTime()}";
        using (Fonts.PushCaption())
        {
            var titleSize = TextDraw.Measure(title);
            var detailSize = TextDraw.Measure(detail);
            var gap = 3f * scale;
            var top = midY - (titleSize.Y + gap + detailSize.Y) * 0.5f;
            TextDraw.At(title, new Vector2(rightX - titleSize.X, top), Styling.TextSecondary);
            TextDraw.At(detail, new Vector2(rightX - detailSize.X, top + titleSize.Y + gap), Styling.TextDim);
            return MathF.Max(titleSize.X, detailSize.X);
        }
    }
}
