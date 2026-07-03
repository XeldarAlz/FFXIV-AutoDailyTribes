using AutoDailyTribes.Core;
using AutoDailyTribes.Core.External;
using AutoDailyTribes.Core.Tasks;
using AutoDailyTribes.Core.Tribes;
using AutoDailyTribes.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ECommons.Throttlers;
using System.Numerics;

namespace AutoDailyTribes.Windows.Sections;

internal static class SetupPanel
{
    private static readonly List<TribeInfo> readyBuffer = [];
    private static readonly List<TribeInfo> runnableBuffer = [];

    public static void Draw(AutoTribeController controller, Configuration cfg)
    {
        if (EzThrottler.Throttle(AdtConstants.ThrottleKeys.UiRefresh, AdtConstants.UiRefreshMs))
        {
            foreach (var tribe in TribeRegistry.Tribes)
                TribeStateReader.Refresh(tribe);
        }

        readyBuffer.Clear();
        for (var i = 0; i < TribeRegistry.Tribes.Length; i++)
        {
            var tribe = TribeRegistry.Tribes[i];
            if (TribeList.IsRunnable(tribe)) readyBuffer.Add(tribe);
        }
        PruneSelection(cfg, readyBuffer);

        var allowanceLeft = TribeStateReader.GlobalAllowanceLeft();
        var exhausted = allowanceLeft <= 0;

        runnableBuffer.Clear();
        var selectedReadyCount = 0;
        for (var i = 0; i < readyBuffer.Count; i++)
        {
            var tribe = readyBuffer[i];
            if (!cfg.SelectedTribes.Contains(tribe.BeastTribeId)) continue;
            selectedReadyCount++;
            if ((tribe.AcceptSlotsRemaining > 0 && !exhausted) || tribe.HasInProgressQuests)
                runnableBuffer.Add(tribe);
        }

        var depsOk = ExternalPlugins.AllRequiredInstalled();
        var canRun = runnableBuffer.Count > 0 && depsOk;

        DrawHero(controller, cfg, readyBuffer, selectedReadyCount, runnableBuffer, depsOk, exhausted, allowanceLeft, canRun);
        TribeList.Draw(controller, cfg);
    }

    // Drop tribes from the saved selection once they're no longer runnable (finished mid-session).
    private static void PruneSelection(Configuration cfg, List<TribeInfo> ready)
    {
        var removed = false;
        for (var i = cfg.SelectedTribes.Count - 1; i >= 0; i--)
        {
            if (IsReadyId(ready, cfg.SelectedTribes[i])) continue;
            cfg.SelectedTribes.RemoveAt(i);
            removed = true;
        }
        if (removed) cfg.SaveDebounced();
    }

    private static bool IsReadyId(List<TribeInfo> ready, uint beastTribeId)
    {
        for (var i = 0; i < ready.Count; i++)
            if (ready[i].BeastTribeId == beastTribeId) return true;
        return false;
    }

    private static void DrawHero(
        AutoTribeController controller, Configuration cfg, List<TribeInfo> ready,
        int selectedCount, List<TribeInfo> runnable, bool depsOk, bool exhausted, int allowanceLeft, bool canRun)
    {
        var s = ImGuiHelpers.GlobalScale;
        var radius = Layout.HeroRingRadius * s;

        Styling.VSpace(6);
        var start = ImGui.GetCursorScreenPos();
        var availX = ImGui.GetContentRegionAvail().X;
        var center = new Vector2(start.X + availX * 0.5f, start.Y + radius);

        var allDone = depsOk && exhausted && runnable.Count == 0;
        var clicked = false;
        if (allDone) ProgressRing.DoneBadge(center, radius);
        else clicked = ProgressRing.PlayButton(center, radius, canRun);
        var hovered = ImGui.IsMouseHoveringRect(center - new Vector2(radius), center + new Vector2(radius));

        ImGui.SetCursorScreenPos(start);
        ImGui.Dummy(new Vector2(availX, radius * 2f));

        if (clicked) controller.RunAll(runnable);
        if (hovered) DrawHeroTooltip(depsOk, exhausted, selectedCount, runnable.Count);

        Styling.VSpace(8);
        var (caption, captionColor) = Caption(depsOk, exhausted, selectedCount, runnable.Count);
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
