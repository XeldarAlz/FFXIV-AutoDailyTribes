using AutoTribeQuests.Core;
using AutoTribeQuests.Core.Tasks;
using AutoTribeQuests.Core.Tribes;
using AutoTribeQuests.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

namespace AutoTribeQuests.Windows.Sections;

internal static class Header
{
    public static void Draw(AutoTribeController controller, Configuration cfg)
    {
        var selected = TribeRegistry.Tribes
            .Where(t => cfg.SelectedTribes.Contains(t.BeastTribeId))
            .ToArray();

        var runnable = selected
            .Where(t => t.Unlocked
                     && t.MeetsRankRequirement
                     && t.AcceptSlotsRemaining > 0
                     && !cfg.DisabledTribes.Contains(t.BeastTribeId))
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
        var allowanceUsed = AtqConstants.DailyAllowanceCap - allowanceLeft;
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
            ImGui.TextUnformatted($"   Daily: {allowanceUsed} / {AtqConstants.DailyAllowanceCap}");

        ImGui.Separator();
    }
}
