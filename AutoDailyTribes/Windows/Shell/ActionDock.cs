using AutoDailyTribes.Core.Localization;
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
        var sub = Loc.T(L.Shell.StopSub, progress.CurrentNumber, Math.Max(progress.Total, 1), Formatting.Clock(progress.ElapsedMs));
        if (HeroButton.Draw(FontAwesomeIcon.Stop, Loc.T(L.Shell.Stop), sub, Styling.AccentRose, true, null, innerWidth)) ctrl.Stop();
    }

    private static void DrawStart(Plugin plugin, float innerWidth)
    {
        var plan = RunPlan.Resolve(plugin.Configuration);
        var reason = !plan.DependenciesReady ? Loc.T(L.Shell.ReasonInstall)
            : plan.SelectedCount == 0 ? Loc.T(L.Shell.ReasonPick)
            : plan.Exhausted && plan.Runnable.Count == 0 ? Loc.T(L.Shell.ReasonExhausted)
            : plan.Runnable.Count == 0 ? Loc.T(L.Shell.ReasonDone)
            : string.Empty;
        var sub = Loc.T(L.Shell.StartSub, Formatting.Tribes(plan.Runnable.Count), plan.AllowancesNeeded, Formatting.ResetCountdown());

        if (HeroButton.Draw(FontAwesomeIcon.Play, Loc.T(L.Shell.Start), sub, Styling.AccentTealDeep, plan.CanRun, reason, innerWidth))
        {
            plugin.Controller.RunAll(plan.Runnable);
        }
    }
}
