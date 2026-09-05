using AutoDailyTribes.Windows.Components;
using AutoDailyTribes.Windows.Sections;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using System.Numerics;

namespace AutoDailyTribes.Windows.Shell;

internal static class HeaderBar
{
    private const string Title = "Auto Daily Tribes";
    private const string CloseHint = "Close";
    private const string MinimizeHint = "Minimize to the header bar";
    private const string RestoreHint = "Restore the window";
    private const string CompactPick = "Pick tribes to begin";
    private const float PadX = 16f;
    private const float IconBox = 26f;
    private const float ButtonSize = 30f;
    private const float ButtonGap = 6f;
    private const int ButtonCount = 2;
    private const float CompactBarWidth = 90f;

    public const float MinimumWidth = PadX * 2f + IconBox + 12f + ButtonSize * ButtonCount + ButtonGap * (ButtonCount - 1);

    public static float ButtonsWidth() => (ButtonSize * ButtonCount + ButtonGap * (ButtonCount - 1)) * ImGuiHelpers.GlobalScale;

    public static bool HandleDrag(Vector2 windowPos, float width, float height)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var dragWidth = width - PadX * scale - ButtonsWidth() - 8f * scale;
        ImGui.SetCursorScreenPos(windowPos);
        ImGui.InvisibleButton("##adt_drag", new Vector2(MathF.Max(1f, dragWidth), height));
        var doubleClicked = ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left);
        if (!ImGui.IsItemActive()) return doubleClicked;

        var delta = ImGui.GetIO().MouseDelta;
        if (delta != Vector2.Zero) ImGui.SetWindowPos(ImGui.GetWindowPos() + delta, ImGuiCond.Always);
        return doubleClicked;
    }

    public static void Draw(AppWindow window, Plugin plugin, Vector2 origin, float width, float height, float windowRounding, bool compact)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var dl = ImGui.GetWindowDrawList();
        var end = origin + new Vector2(width, height);
        var padX = PadX * scale;
        var midY = origin.Y + height * 0.5f;

        Paint.Fill(dl, origin, end, Styling.WithAlpha(Styling.Surface1, 0.40f), windowRounding,
            compact ? ImDrawFlags.RoundCornersAll : ImDrawFlags.RoundCornersTop);
        if (!compact) Paint.Hairline(dl, new Vector2(origin.X, end.Y - 0.5f), new Vector2(end.X, end.Y - 0.5f));

        var iconBox = IconBox * scale;
        var iconMin = new Vector2(origin.X + padX, midY - iconBox * 0.5f);
        AppIcon.Draw(dl, iconMin, iconMin + new Vector2(iconBox, iconBox), 7f * scale);

        var buttonsLeft = end.X - padX - ButtonsWidth();
        var x = iconMin.X + iconBox + 12f * scale;
        using (Fonts.PushHeadline())
        {
            var titleSize = TextDraw.Measure(Title);
            if (x + titleSize.X <= buttonsLeft)
            {
                TextDraw.At(Title, new Vector2(x, midY - titleSize.Y * 0.5f), Styling.TextStrong);
                x += titleSize.X + 14f * scale;
            }
        }

        var info = ReadyState.Resolve(plugin.Configuration, plugin.Controller);
        var pillEnd = DrawStatusPill(dl, info, x, buttonsLeft, midY);
        if (pillEnd > x) x = pillEnd + 14f * scale;

        if (compact) DrawCompactInfo(plugin, info, x, buttonsLeft - 14f * scale, midY);

        DrawButtons(window, end, midY, compact);
    }

    private static void DrawButtons(AppWindow window, Vector2 end, float midY, bool compact)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var padX = PadX * scale;
        var buttonSize = ButtonSize * scale;
        var stride = buttonSize + ButtonGap * scale;
        var top = midY - buttonSize * 0.5f;

        ImGui.SetCursorScreenPos(new Vector2(end.X - padX - buttonSize, top));
        if (IconButton.Draw(FontAwesomeIcon.Times, "##adt_close", buttonSize, tooltip: CloseHint))
        {
            window.IsOpen = false;
        }

        ImGui.SetCursorScreenPos(new Vector2(end.X - padX - buttonSize - stride, top));
        if (IconButton.Draw(compact ? FontAwesomeIcon.ChevronUp : FontAwesomeIcon.ChevronDown, "##adt_minimize", buttonSize,
                tooltip: compact ? RestoreHint : MinimizeHint))
        {
            window.ToggleCompact();
        }
    }

    private static float DrawStatusPill(ImDrawListPtr dl, ReadyState.Info info, float x, float rightLimit, float midY)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var label = ReadyState.ShortLabel(info.Kind);
        var padX = 10f * scale;
        var dotRadius = 3.5f * scale;

        using (Fonts.PushCaption())
        {
            var labelSize = TextDraw.Measure(label);
            var pillHeight = labelSize.Y + 8f * scale;
            var pillMin = new Vector2(x, midY - pillHeight * 0.5f);
            var pillMax = pillMin + new Vector2(padX * 2f + dotRadius * 2f + 6f * scale + labelSize.X, pillHeight);
            if (pillMax.X > rightLimit) return x;

            Paint.Pill(dl, pillMin, pillMax, Styling.WithAlpha(info.Accent, 0.16f), Styling.WithAlpha(info.Accent, 0.45f));

            var animated = info.Kind is ReadyState.Kind.Running;
            var dotColor = animated ? Styling.PulseColor(info.Accent, info.AccentSoft, Styling.PulseMedium) : info.Accent;
            dl.AddCircleFilled(new Vector2(pillMin.X + padX + dotRadius, midY), dotRadius, Paint.Col(dotColor));
            TextDraw.At(label, new Vector2(pillMin.X + padX + dotRadius * 2f + 6f * scale, midY - labelSize.Y * 0.5f), info.AccentSoft);
            return pillMax.X;
        }
    }

    private static void DrawCompactInfo(Plugin plugin, ReadyState.Info info, float x, float rightX, float midY)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var dl = ImGui.GetWindowDrawList();
        var ctrl = plugin.Controller;
        if (rightX - x < CompactBarWidth * scale) return;

        if (!ctrl.Running)
        {
            var plan = RunPlan.Resolve(plugin.Configuration);
            var summary = plan.SelectedCount == 0
                ? CompactPick
                : $"{Formatting.Plural(plan.Runnable.Count, "tribe", "tribes")} · {plan.AllowancesNeeded} allowances · reset in {Formatting.ResetCountdown()}";
            using (Fonts.PushCaption())
            {
                var summarySize = TextDraw.Measure(summary);
                TextDraw.At(TextDraw.Truncate(summary, rightX - x), new Vector2(x, midY - summarySize.Y * 0.5f), Styling.TextDim);
            }

            return;
        }

        var progress = ctrl.Progress;
        var barWidth = CompactBarWidth * scale;
        var barX = rightX - barWidth;
        var barHeight = 6f * scale;
        Paint.Bar(dl, new Vector2(barX, midY - barHeight * 0.5f), barWidth, barHeight, RunningPanel.SmoothFraction(progress), info.Accent);

        var textWidth = barX - 12f * scale - x;
        if (textWidth <= 0f) return;

        var text = $"{ReadyState.PhaseLabel(progress.Phase)}  ·  {ctrl.Status}";
        using (Fonts.PushCaption())
        {
            var textSize = TextDraw.Measure(text);
            TextDraw.At(TextDraw.Truncate(text, textWidth), new Vector2(x, midY - textSize.Y * 0.5f), Styling.TextSecondary);
        }
    }
}
