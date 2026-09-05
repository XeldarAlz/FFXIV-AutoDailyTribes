using AutoDailyTribes.Core;
using AutoDailyTribes.Core.External;
using AutoDailyTribes.Core.Tasks;
using AutoDailyTribes.Core.Tribes;
using AutoDailyTribes.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using System.Numerics;

namespace AutoDailyTribes.Windows.Sections;

internal static class SetupPanel
{
    private static readonly List<TribeInfo> Selectable = [];
    private static readonly List<TribeInfo> Runnable = [];

    public static void Draw(AutoTribeController controller, Configuration cfg)
    {
        var tribes = TribeRegistry.Tribes;
        for (var tribeIndex = 0; tribeIndex < tribes.Length; tribeIndex++)
        {
            TribeStateReader.Refresh(tribes[tribeIndex]);
        }

        var allowanceLeft = TribeStateReader.GlobalAllowanceLeft();
        var exhausted = allowanceLeft <= 0;

        Selectable.Clear();
        Runnable.Clear();
        var selectedCount = 0;
        var selectedReadyCount = 0;

        for (var tribeIndex = 0; tribeIndex < tribes.Length; tribeIndex++)
        {
            var tribe = tribes[tribeIndex];
            var selected = cfg.SelectedTribes.Contains(tribe.BeastTribeId);
            if (selected) selectedCount++;

            if (!TribeList.IsRunnable(tribe) || !FilterBar.PassesKindFilter(cfg, tribe)) continue;
            Selectable.Add(tribe);
            if (!selected) continue;

            selectedReadyCount++;
            if (tribe.HasInProgressQuests || (tribe.AcceptSlotsRemaining > 0 && !exhausted)) Runnable.Add(tribe);
        }

        var depsOk = ExternalPlugins.AllRequiredInstalled();
        var canRun = Runnable.Count > 0 && depsOk;

        DrawHero(controller, cfg, selectedCount, selectedReadyCount, depsOk, exhausted, allowanceLeft, canRun);
        FilterBar.Draw(cfg);
        Styling.VSpace(7);
        TribeList.Draw(controller, cfg);
    }

    private static void DrawHero(
        AutoTribeController controller, Configuration cfg, int selectedCount, int selectedReadyCount,
        bool depsOk, bool exhausted, int allowanceLeft, bool canRun)
    {
        var s = ImGuiHelpers.GlobalScale;
        var radius = Layout.HeroRingRadius * s;

        Styling.VSpace(6);
        var start = ImGui.GetCursorScreenPos();
        var availX = ImGui.GetContentRegionAvail().X;
        var center = new Vector2(start.X + availX * 0.5f, start.Y + radius);

        var allDone = depsOk && Runnable.Count == 0 && (exhausted || selectedCount > 0);
        var clicked = false;
        if (allDone) ProgressRing.DoneBadge(center, radius);
        else clicked = ProgressRing.PlayButton(center, radius, canRun);
        var hovered = ImGui.IsMouseHoveringRect(center - new Vector2(radius), center + new Vector2(radius));

        ImGui.SetCursorScreenPos(start);
        ImGui.Dummy(new Vector2(availX, radius * 2f));

        if (clicked) controller.RunAll(Runnable.ToArray());
        if (hovered) DrawHeroTooltip(depsOk, exhausted, selectedCount, Runnable.Count);

        Styling.VSpace(8);
        var (caption, captionColor) = Caption(depsOk, exhausted, selectedCount, Runnable.Count);
        Styling.TextCentered(caption, captionColor, 1.15f);

        Styling.VSpace(2);
        var used = AdtConstants.DailyAllowanceCap - allowanceLeft;
        Styling.TextCentered($"Allowance {used} / {AdtConstants.DailyAllowanceCap}   ·   Reset {ResetCountdown()}", Styling.TextDim);

        DrawSelectionButtons(cfg, selectedCount, selectedReadyCount);

        Styling.VSpace(10);
        ImGui.Separator();
        Styling.VSpace(6);
    }

    private static void DrawSelectionButtons(Configuration cfg, int selectedCount, int selectedReadyCount)
    {
        var canSelectAll = Selectable.Count > selectedReadyCount;
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
                for (var tribeIndex = 0; tribeIndex < Selectable.Count; tribeIndex++)
                {
                    var id = Selectable[tribeIndex].BeastTribeId;
                    if (!cfg.SelectedTribes.Contains(id)) cfg.SelectedTribes.Add(id);
                }
                cfg.SaveDebounced();
            }
            Tooltip.For("Adds every tribe currently shown as ready. Tribes hidden by the filters are left alone.");
            if (canClear) ImGui.SameLine();
        }

        if (!canClear) return;
        if (ImGui.Button(clear))
        {
            cfg.SelectedTribes.Clear();
            cfg.SaveDebounced();
        }
        Tooltip.For("Empties your standing pick, including tribes hidden by the filters.");
    }

    private static (string text, Vector4 color) Caption(bool depsOk, bool exhausted, int selectedCount, int runnableCount)
    {
        if (!depsOk) return ("Install required plugins first", Styling.AccentRose);
        if (exhausted && runnableCount == 0) return ("All done today — back after reset", Styling.AccentMint);
        if (selectedCount == 0) return ("Pick tribes below to begin", Styling.TextSecondary);
        if (runnableCount == 0) return ("Your tribes are done — back after reset", Styling.AccentMint);
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
                    ? "Tick the tribe cards below to build your list, then press play. The list is remembered, so tomorrow is one click."
                    : runnableCount == 0
                        ? "Every tribe in your list is finished for today. The list is kept, so it runs again after the reset."
                        : runnableCount < selectedCount
                            ? $"{selectedCount} in your list, {runnableCount} runnable right now — finished and locked tribes are skipped."
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
