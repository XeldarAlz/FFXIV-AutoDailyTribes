using AutoTribeQuests.Core;
using AutoTribeQuests.Core.Tasks;
using AutoTribeQuests.Core.Tribes;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoTribeQuests.Windows.Components;

// Single card representing one tribe. Composed of the smaller primitives:
//
//   [☐/☑] [KindIcon] [Name + Era]                     [AllowancePill]
//   [Rank N ━━━━━━━━━━━━━━━━━━━━]
//   [               Do dailies                       ]
//
// Top-left checkbox controls Configuration.SelectedTribes membership and is
// rendered as a hollow / filled circle (FontAwesome). The card border switches
// to the teal accent while selected so the selection state reads at a glance
// across the grid. Locked tribes can't be selected.
internal static class TribeCard
{
    public static void Draw(TribeInfo tribe, AutoTribeController controller, Configuration cfg)
    {
        var disabled = cfg.DisabledTribes.Contains(tribe.BeastTribeId);
        var selected = cfg.SelectedTribes.Contains(tribe.BeastTribeId);
        var runnable = tribe.Unlocked
            && tribe.MeetsRankRequirement
            && tribe.AcceptSlotsRemaining > 0
            && !disabled
            && !controller.Running;

        var border = ResolveBorder(tribe, disabled, selected, controller.Running);
        var bg = Vector4.Lerp(Styling.CardBg, Styling.EraTint(tribe.Era), 1f);
        var size = new Vector2(-1, Layout.TribeCardHeight * ImGuiHelpers.GlobalScale);

        using (Card.Begin($"##tribe_{tribe.BeastTribeId}", size, bg, border, selected ? 1.8f : 1.2f))
        {
            DrawHeaderRow(tribe, cfg, runnable, selected);
            ImGui.Spacing();
            RankBadge.Draw(tribe);
            ImGui.Spacing();
            DrawActionRow(tribe, controller, cfg, runnable);
        }
    }

    private static void DrawHeaderRow(TribeInfo tribe, Configuration cfg, bool runnable, bool selected)
    {
        DrawSelectToggle(tribe, cfg, enabled: runnable, selected);
        ImGui.SameLine();

        KindIcon.Draw(tribe.Kind);
        ImGui.SameLine();

        ImGui.SetWindowFontScale(1.10f);
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextStrong))
            ImGui.TextUnformatted(tribe.Name);
        ImGui.SetWindowFontScale(1.0f);

        var pillLabel = $"{tribe.AlreadyAcceptedToday.Length}/{AtqConstants.MaxAcceptsPerTribe}";
        var pillWidth = ImGui.CalcTextSize(pillLabel).X + 16 * ImGuiHelpers.GlobalScale;
        ImGui.SameLine(ImGui.GetContentRegionAvail().X + ImGui.GetCursorPosX() - pillWidth);
        AllowancePill.Draw(tribe);
    }

    private static void DrawSelectToggle(TribeInfo tribe, Configuration cfg, bool enabled, bool selected)
    {
        var icon = selected ? FontAwesomeIcon.CheckCircle : FontAwesomeIcon.Circle;
        var color = !enabled
            ? Styling.TextMuted
            : selected ? Styling.AccentTeal : Styling.TextDim;

        using (ImRaii.PushFont(UiBuilder.IconFont))
        using (ImRaii.PushColor(ImGuiCol.Button, Vector4.Zero))
        using (ImRaii.PushColor(ImGuiCol.ButtonHovered, Styling.CardBgSoft))
        using (ImRaii.PushColor(ImGuiCol.ButtonActive, Styling.CardBgSoft))
        using (ImRaii.PushColor(ImGuiCol.Text, color))
        using (ImRaii.Disabled(!enabled))
        {
            if (ImGui.Button($"{icon.ToIconString()}##sel_{tribe.BeastTribeId}"))
            {
                if (selected) cfg.SelectedTribes.Remove(tribe.BeastTribeId);
                else cfg.SelectedTribes.Add(tribe.BeastTribeId);
                cfg.SaveDebounced();
            }
        }
        if (enabled)
            Tooltip.For(selected ? "Click to deselect this tribe from batch run" : "Click to add this tribe to the batch run");
    }

    private static void DrawActionRow(TribeInfo tribe, AutoTribeController controller, Configuration cfg, bool runnable)
    {
        if (ActionButton.Draw("Do dailies", enabled: runnable, width: -1))
            controller.Run(tribe);

        if (!tribe.Unlocked)
            Tooltip.For("Tribe not yet unlocked — complete the intro quest in-game first");
        else if (!tribe.MeetsRankRequirement)
            Tooltip.For($"Requires rank {tribe.MinRankForDailies} (have {tribe.Rank})");
        else if (tribe.AcceptSlotsRemaining <= 0)
            Tooltip.For("All daily slots already used for this tribe today");
        else if (cfg.DisabledTribes.Contains(tribe.BeastTribeId))
            Tooltip.For("Disabled in config");
    }

    private static Vector4 ResolveBorder(TribeInfo tribe, bool disabled, bool selected, bool running)
    {
        if (disabled) return Styling.BorderLocked;
        if (!tribe.Unlocked) return Styling.BorderLocked;
        if (running) return Styling.PulseColor(Styling.BorderActive, Styling.AccentTealSoft, Styling.PulseMedium);
        if (selected) return Styling.AccentTeal;
        if (tribe.AcceptSlotsRemaining <= 0) return Styling.BorderDim;
        return Styling.BorderActive * 0.65f;
    }
}
