using AutoDailyTribes.Core.Localization;
using AutoDailyTribes.Windows.Components;
using AutoDailyTribes.Windows.Sections;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using System.Numerics;

namespace AutoDailyTribes.Windows.Shell;

internal static class MiniPlayer
{
    private const float PadX = 18f;
    private const float ButtonSize = 34f;
    private const float BarWidth = 160f;
    private const float BarHeight = 8f;

    public static bool Draw(Plugin plugin, Vector2 size, float windowRounding)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var origin = ImGui.GetCursorScreenPos();
        var end = origin + size;
        var dl = ImGui.GetWindowDrawList();
        var ctrl = plugin.Controller;
        var progress = ctrl.Progress;
        var info = ReadyState.Resolve(plugin.Configuration, ctrl);

        Dock.Background(dl, origin, end, windowRounding);

        var padX = PadX * scale;
        var buttonSize = ButtonSize * scale;
        ImGui.SetCursorScreenPos(origin);
        var hit = Hit.Area("##adt_mini_open", new Vector2(size.X - padX - buttonSize - 8f * scale, size.Y));
        var hover = Motion.Hover(Motion.Key("##adt_mini_open"), hit.Hovered);
        if (hover > 0.01f)
        {
            Paint.Fill(dl, origin, end, Styling.WithAlpha(Styling.Surface2, 0.35f * hover), windowRounding, ImDrawFlags.RoundCornersBottom);
        }

        var midY = origin.Y + size.Y * 0.5f;
        Paint.Dot(dl, new Vector2(origin.X + padX + 4f * scale, midY), 4f * scale, Styling.PulseColor(info.Accent, info.AccentSoft, Styling.PulseMedium));

        var barWidth = BarWidth * scale;
        var barRight = end.X - padX - buttonSize - 16f * scale;
        var barX = barRight - barWidth;
        var barY = midY - BarHeight * scale * 0.5f;
        Paint.Bar(dl, new Vector2(barX, barY), barWidth, BarHeight * scale, RunningPanel.SmoothFraction(progress), info.Accent);

        var textX = origin.X + padX + 22f * scale;
        var phase = ReadyState.PhaseLabel(progress.Phase);
        var phaseSize = TextDraw.SmallCapsSize(phase);
        var lineHeight = ImGui.GetTextLineHeight();
        var gap = 2f * scale;
        var top = midY - (phaseSize.Y + gap + lineHeight) * 0.5f;
        TextDraw.SmallCaps(phase, new Vector2(textX, top), info.AccentSoft);

        var main = progress.Current is { } tribe ? Loc.T(L.Shell.MiniStatus, tribe.Name, ctrl.Status) : Loc.T(L.Shell.Waiting);
        TextDraw.At(TextDraw.Truncate(main, barX - 16f * scale - textX), new Vector2(textX, top + phaseSize.Y + gap), Styling.TextStrong);

        ImGui.SetCursorScreenPos(new Vector2(end.X - padX - buttonSize, midY - buttonSize * 0.5f));
        if (IconButton.Draw(FontAwesomeIcon.Stop, "##adt_mini_stop", buttonSize, Styling.AccentRose, Loc.T(L.Shell.StopHint)))
        {
            ctrl.Stop();
        }

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(size);
        return hit.Clicked;
    }
}
