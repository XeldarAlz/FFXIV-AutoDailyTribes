using AutoDailyTribes.Windows.Components;
using AutoDailyTribes.Windows.Sections;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using System.Numerics;

namespace AutoDailyTribes.Windows.Shell;

internal static class ActionDock
{
    private const float PadX = 18f;
    private const string StartLabel = "Start";
    private const string StopLabel = "Stop";
    private const string ReasonInstall = "Install the required plugins first";
    private const string ReasonPick = "Pick a tribe below";
    private const string ReasonExhausted = "All allowances spent, back after the reset";
    private const string ReasonDone = "Your tribes are done for today";

    public static void Draw(Plugin plugin, Vector2 size, float windowRounding)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var origin = ImGui.GetCursorScreenPos();
        var end = origin + size;
        var dl = ImGui.GetWindowDrawList();
        Dock.Background(dl, origin, end, windowRounding);

        var padX = PadX * scale;
        var buttonHeight = Layout.HeroButtonHeight * scale;
        var innerWidth = size.X - padX * 2f;
        ImGui.SetCursorScreenPos(new Vector2(origin.X + padX, origin.Y + (size.Y - buttonHeight) * 0.5f));

        if (plugin.Controller.Running) DrawStop(plugin, innerWidth);
        else DrawStart(plugin, innerWidth);

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(size);
    }

    private static void DrawStop(Plugin plugin, float innerWidth)
    {
        var ctrl = plugin.Controller;
        var progress = ctrl.Progress;
        var sub = $"Tribe {progress.CurrentNumber} of {Math.Max(progress.Total, 1)} · {Formatting.Clock(progress.ElapsedMs)}";
        if (HeroButton.Draw(FontAwesomeIcon.Stop, StopLabel, sub, Styling.AccentRose, true, null, innerWidth)) ctrl.Stop();
    }

    private static void DrawStart(Plugin plugin, float innerWidth)
    {
        var plan = RunPlan.Resolve(plugin.Configuration);
        var reason = !plan.DependenciesReady ? ReasonInstall
            : plan.SelectedCount == 0 ? ReasonPick
            : plan.Exhausted && plan.Runnable.Count == 0 ? ReasonExhausted
            : plan.Runnable.Count == 0 ? ReasonDone
            : string.Empty;
        var sub = $"{Formatting.Plural(plan.Runnable.Count, "tribe", "tribes")} · {plan.AllowancesNeeded} allowances · reset in {Formatting.ResetCountdown()}";

        if (HeroButton.Draw(FontAwesomeIcon.Play, StartLabel, sub, Styling.AccentTealDeep, plan.CanRun, reason, innerWidth))
        {
            plugin.Controller.RunAll(plan.Runnable);
        }
    }
}
