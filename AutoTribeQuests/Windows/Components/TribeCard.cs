using AutoTribeQuests.Core.Tasks;
using AutoTribeQuests.Core.Tribes;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoTribeQuests.Windows.Components;

// Single card representing one tribe. Composed of the smaller primitives:
//   [KindIcon] [Name + Era]            [AllowancePill]
//   [RankBadge with rep bar]
//   [Action button: "Do dailies"]
internal static class TribeCard
{
    public static void Draw(TribeInfo tribe, AutoTribeController controller, Configuration cfg)
    {
        var disabled = cfg.DisabledTribes.Contains(tribe.BeastTribeId);
        var runnable = tribe.Unlocked
            && tribe.MeetsRankRequirement
            && tribe.AcceptSlotsRemaining > 0
            && !disabled
            && !controller.Running;

        var border = ResolveBorder(tribe, disabled, controller.Running);
        var bg = Vector4.Lerp(Styling.CardBg, Styling.EraTint(tribe.Era), 1f);
        var size = new Vector2(-1, Layout.TribeCardHeight * ImGuiHelpers.GlobalScale);

        using (Card.Begin($"##tribe_{tribe.BeastTribeId}", size, bg, border, 1.2f))
        {
            DrawHeaderRow(tribe);
            ImGui.Spacing();
            RankBadge.Draw(tribe);
            ImGui.Spacing();
            DrawActionRow(tribe, controller, cfg, runnable);
        }
    }

    private static void DrawHeaderRow(TribeInfo tribe)
    {
        KindIcon.Draw(tribe.Kind);
        ImGui.SameLine();
        ImGui.SetWindowFontScale(1.10f);
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextStrong))
            ImGui.TextUnformatted(tribe.Name);
        ImGui.SetWindowFontScale(1.0f);

        ImGui.SameLine();
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextMuted))
            ImGui.TextUnformatted($" [{tribe.Era}]");

        // Allowance pill on the right.
        var pillLabel = $"{tribe.AlreadyAcceptedToday.Length}/{Constants.MaxAcceptsPerTribe}";
        var pillWidth = ImGui.CalcTextSize(pillLabel).X + 16 * ImGuiHelpers.GlobalScale;
        ImGui.SameLine(ImGui.GetContentRegionAvail().X + ImGui.GetCursorPosX() - pillWidth);
        AllowancePill.Draw(tribe);
    }

    private static void DrawActionRow(TribeInfo tribe, AutoTribeController controller, Configuration cfg, bool runnable)
    {
        if (ActionButton.Draw("Do dailies", enabled: runnable, width: -1))
            controller.Run(tribe);

        if (!tribe.Unlocked)
            Tooltip.For($"Locked behind quest {tribe.UnlockQuestId}");
        else if (!tribe.MeetsRankRequirement)
            Tooltip.For($"Requires rank {tribe.MinRankForDailies} (have {tribe.Rank})");
        else if (tribe.AcceptSlotsRemaining <= 0)
            Tooltip.For("All daily slots already used for this tribe today");
        else if (cfg.DisabledTribes.Contains(tribe.BeastTribeId))
            Tooltip.For("Disabled in config");
    }

    private static Vector4 ResolveBorder(TribeInfo tribe, bool disabled, bool running)
    {
        if (disabled) return Styling.BorderLocked;
        if (!tribe.Unlocked) return Styling.BorderLocked;
        if (running) return Styling.PulseColor(Styling.BorderActive, Styling.AccentTealSoft, Styling.PulseMedium);
        if (tribe.AcceptSlotsRemaining <= 0) return Styling.BorderDim;
        return Styling.BorderActive * 0.65f;
    }
}
