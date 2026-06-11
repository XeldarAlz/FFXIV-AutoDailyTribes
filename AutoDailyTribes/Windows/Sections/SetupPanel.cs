using AutoDailyTribes.Core;
using AutoDailyTribes.Core.External;
using AutoDailyTribes.Core.Tasks;
using AutoDailyTribes.Core.Tribes;
using AutoDailyTribes.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoDailyTribes.Windows.Sections;

internal static class SetupPanel
{
    public static void Draw(AutoTribeController controller, Configuration cfg)
    {
        foreach (var tribe in TribeRegistry.Tribes)
            TribeStateReader.Refresh(tribe);

        var ready = TribeRegistry.Tribes.Where(TribeList.IsRunnable).ToList();
        PruneSelection(cfg, ready);

        var allowanceLeft = TribeStateReader.GlobalAllowanceLeft();
        var exhausted = allowanceLeft <= 0;

        var selectedReady = ready.Where(t => cfg.SelectedTribes.Contains(t.BeastTribeId)).ToArray();
        var runnable = selectedReady
            .Where(t => (t.AcceptSlotsRemaining > 0 && !exhausted) || t.HasInProgressQuests)
            .ToArray();

        var depsOk = ExternalPlugins.AllRequiredInstalled();
        var canRun = runnable.Length > 0 && depsOk;

        DrawHero(controller, cfg, ready, selectedReady.Length, runnable, depsOk, exhausted, allowanceLeft, canRun);
        TribeList.Draw(controller, cfg);
    }

    // Drop tribes from the saved selection once they're no longer runnable (finished mid-session).
    private static void PruneSelection(Configuration cfg, List<TribeInfo> ready)
    {
        var readyIds = ready.Select(t => t.BeastTribeId).ToHashSet();
        if (cfg.SelectedTribes.RemoveAll(id => !readyIds.Contains(id)) > 0)
            cfg.SaveDebounced();
    }

    private static void DrawHero(
        AutoTribeController controller, Configuration cfg, List<TribeInfo> ready,
        int selectedCount, TribeInfo[] runnable, bool depsOk, bool exhausted, int allowanceLeft, bool canRun)
    {
        var s = ImGuiHelpers.GlobalScale;
        var radius = Layout.HeroRingRadius * s;

        Styling.VSpace(6);
        var start = ImGui.GetCursorScreenPos();
        var availX = ImGui.GetContentRegionAvail().X;
        var center = new Vector2(start.X + availX * 0.5f, start.Y + radius);

        var clicked = ProgressRing.PlayButton(center, radius, canRun);
        var hovered = ImGui.IsMouseHoveringRect(center - new Vector2(radius), center + new Vector2(radius));

        ImGui.SetCursorScreenPos(start);
        ImGui.Dummy(new Vector2(availX, radius * 2f));

        if (clicked) controller.RunAll(runnable);
        if (hovered) DrawHeroTooltip(depsOk, exhausted, selectedCount, runnable.Length);

        Styling.VSpace(8);
        var (caption, captionColor) = Caption(depsOk, exhausted, selectedCount, runnable.Length);
        Styling.TextCentered(caption, captionColor, 1.15f);

        Styling.VSpace(2);
        var used = AdtConstants.DailyAllowanceCap - allowanceLeft;
        Styling.TextCentered($"Allowance {used} / {AdtConstants.DailyAllowanceCap}   ·   Reset {ResetCountdown()}", Styling.TextDim);

        DrawSelectionButtons(cfg, ready, selectedCount);

        Styling.VSpace(10);
        ImGui.Separator();
        Styling.VSpace(4);
    }

    private static void DrawSelectionButtons(Configuration cfg, List<TribeInfo> ready, int selectedCount)
    {
        var s = ImGuiHelpers.GlobalScale;
        var canSelectAll = ready.Count > selectedCount;
        var canClear = selectedCount > 0;
        if (!canSelectAll && !canClear) return;

        const string selectAll = "Select all available";
        const string clear = "Clear";
        var pad = ImGui.GetStyle().FramePadding.X * 2f;
        var spacing = ImGui.GetStyle().ItemSpacing.X;

        var wSelect = canSelectAll ? ImGui.CalcTextSize(selectAll).X + pad : 0f;
        var wClear = canClear ? ImGui.CalcTextSize(clear).X + pad : 0f;
        var total = wSelect + wClear + (canSelectAll && canClear ? spacing : 0f);

        Styling.VSpace(6);
        Styling.CenterNextItem(total);

        if (canSelectAll)
        {
            if (ImGui.Button(selectAll))
            {
                foreach (var tribe in ready)
                    if (!cfg.SelectedTribes.Contains(tribe.BeastTribeId))
                        cfg.SelectedTribes.Add(tribe.BeastTribeId);
                cfg.SaveDebounced();
            }
            if (canClear) ImGui.SameLine();
        }

        if (canClear && ImGui.Button(clear))
        {
            cfg.SelectedTribes.Clear();
            cfg.SaveDebounced();
        }
    }

    private static (string text, Vector4 color) Caption(bool depsOk, bool exhausted, int selectedCount, int runnableCount)
    {
        if (!depsOk) return ("Install required plugins first", Styling.AccentRose);
        if (exhausted && runnableCount == 0) return ("All done today — back after reset", Styling.AccentMint);
        if (selectedCount == 0) return ("Pick tribes below to begin", Styling.TextSecondary);
        if (runnableCount < selectedCount)
            return ($"Run {runnableCount} of {selectedCount} selected", Styling.TextStrong);
        return ($"Run {runnableCount} selected tribe{(runnableCount == 1 ? "" : "s")}", Styling.TextStrong);
    }

    private static void DrawHeroTooltip(bool depsOk, bool exhausted, int selectedCount, int runnableCount)
    {
        var text = !depsOk
            ? "Install all required plugins first (see the plug icon)."
            : exhausted && runnableCount == 0
                ? $"All {AdtConstants.DailyAllowanceCap} daily quests done — try again after reset."
                : selectedCount == 0
                    ? "Tick the tribe cards below to add them to the batch, then press play."
                    : runnableCount < selectedCount
                        ? $"{selectedCount} selected, {runnableCount} runnable — locked/maxed tribes are skipped."
                        : $"Run {runnableCount} tribe(s) back-to-back. The daily allowance cap stops the queue early.";
        Tooltip.For(text);
    }

    private static string ResetCountdown()
    {
        var now = DateTime.UtcNow;
        var nextReset = new DateTime(now.Year, now.Month, now.Day, 15, 0, 0, DateTimeKind.Utc);
        if (nextReset <= now) nextReset = nextReset.AddDays(1);
        var r = nextReset - now;
        return r.TotalHours >= 1
            ? $"{(int)r.TotalHours}h {r.Minutes:D2}m"
            : r.TotalMinutes >= 1
                ? $"{r.Minutes}m {r.Seconds:D2}s"
                : $"{r.Seconds}s";
    }
}
