using AutoTribeQuests.Core;
using AutoTribeQuests.Core.Tasks;
using AutoTribeQuests.Core.Tribes;
using AutoTribeQuests.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

namespace AutoTribeQuests.Windows.Sections;

// Action row + global allowance counter. Sits between the toolbar and the tribe grid.
internal static class Header
{
    public static void Draw(AutoTribeController controller, Configuration cfg)
    {
        var available = TribeRegistry.Tribes
            .Where(t => !cfg.DisabledTribes.Contains(t.BeastTribeId))
            .Where(t => t.Unlocked && t.MeetsRankRequirement && t.AcceptSlotsRemaining > 0)
            .ToArray();

        var canRunAll = available.Length > 0 && !controller.Running;
        if (ActionButton.Draw($"Run all unlocked ({available.Length})", enabled: canRunAll, width: 220))
            controller.RunAll(available);

        ImGui.SameLine();
        using (ImRaii.Disabled(!controller.Running))
            if (ImGui.Button("Stop"))
                controller.Stop();

        ImGui.SameLine();
        var allowance = TribeStateReader.GlobalAllowanceLeft();
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
            ImGui.TextUnformatted($"   Daily allowance: {AtqConstants.DailyAllowanceCap - allowance} / {AtqConstants.DailyAllowanceCap}");

        ImGui.Separator();
    }
}
