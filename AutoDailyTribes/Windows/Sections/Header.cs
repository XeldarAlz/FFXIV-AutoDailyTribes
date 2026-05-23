using AutoDailyTribes.Core;
using AutoDailyTribes.Core.Tasks;
using AutoDailyTribes.Core.Tribes;
using AutoDailyTribes.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

namespace AutoDailyTribes.Windows.Sections;

internal static class Header
{
    public static void Draw(AutoTribeController controller, Configuration cfg)
    {
        // Preserve click-order — cfg.SelectedTribes is a List populated in the order the user ticked cards.
        var byId = TribeRegistry.Tribes.ToDictionary(t => t.BeastTribeId);
        var selected = cfg.SelectedTribes
            .Where(byId.ContainsKey)
            .Select(id => byId[id])
            .ToArray();

        var runnable = selected
            .Where(t => t.Unlocked
                     && t.MeetsRankRequirement
                     && (t.AcceptSlotsRemaining > 0 || t.HasInProgressQuests))
            .ToArray();

        var canRun = runnable.Length > 0 && !controller.Running;
        if (ActionButton.Draw($"Run selected ({runnable.Length})", enabled: canRun, width: 200))
            controller.RunAll(runnable);
        Tooltip.For(selected.Length == 0
            ? "Tick the circle on the tribe cards below to add them to the batch."
            : runnable.Length < selected.Length
                ? $"{selected.Length} selected, {runnable.Length} runnable — locked/maxed tribes are skipped."
                : $"Runs {runnable.Length} tribe(s) back-to-back. Allowance cap stops the queue early.");

        ImGui.SameLine();
        using (ImRaii.Disabled(!controller.Running))
            if (ImGui.Button("Stop"))
                controller.Stop();

        if (selected.Length > 0)
        {
            ImGui.SameLine();
            using (ImRaii.Disabled(controller.Running))
                if (ImGui.Button("Clear selection"))
                {
                    cfg.SelectedTribes.Clear();
                    cfg.SaveDebounced();
                }
        }

        ImGui.SameLine();
        var allowanceLeft = TribeStateReader.GlobalAllowanceLeft();
        var allowanceUsed = AdtConstants.DailyAllowanceCap - allowanceLeft;
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
            ImGui.TextUnformatted($"   Daily: {allowanceUsed} / {AdtConstants.DailyAllowanceCap}   ·   Reset in {FormatResetCountdown()}");
        Tooltip.For("FFXIV daily reset is 15:00 UTC. Allowance and per-tribe slots refill then.");

        ImGui.Separator();
    }

    private static string FormatResetCountdown()
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
