using AutoDailyTribes.Core;
using AutoDailyTribes.Core.External;
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
        var allowanceExhausted = TribeStateReader.GlobalAllowanceLeft() <= 0;

        var byId = TribeRegistry.Tribes.ToDictionary(t => t.BeastTribeId);
        var selected = cfg.SelectedTribes
            .Where(byId.ContainsKey)
            .Select(id => byId[id])
            .ToArray();

        var runnable = selected
            .Where(t => t.Unlocked
                     && t.MeetsRankRequirement
                     && ((t.AcceptSlotsRemaining > 0 && !allowanceExhausted) || t.HasInProgressQuests))
            .ToArray();

        var depsOk = ExternalPlugins.AllRequiredInstalled();
        var canRun = runnable.Length > 0 && !controller.Running && depsOk;
        if (ActionButton.Draw($"Run selected ({runnable.Length})", enabled: canRun, width: 200))
            controller.RunAll(runnable);
        Tooltip.For(!depsOk
            ? "Install all required plugins first (see the plug icon)."
            : allowanceExhausted && runnable.Length == 0
                ? $"All {AdtConstants.DailyAllowanceCap} daily quests done — try again after reset."
                : selected.Length == 0
                    ? "Tick the circle on the tribe cards below to add them to the batch."
                    : runnable.Length < selected.Length
                        ? $"{selected.Length} selected, {runnable.Length} runnable — locked/maxed tribes are skipped."
                        : $"Runs {runnable.Length} tribe(s) back-to-back. Allowance cap stops the queue early.");

        if (selected.Length > 0)
        {
            ImGui.SameLine();
            if (ImGui.Button("Clear selection"))
            {
                cfg.SelectedTribes.Clear();
                cfg.SaveDebounced();
            }
        }

        ImGui.Separator();
    }
}
