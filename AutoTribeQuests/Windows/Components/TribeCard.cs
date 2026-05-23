using AutoTribeQuests.Core;
using AutoTribeQuests.Core.Tasks;
using AutoTribeQuests.Core.Tribes;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoTribeQuests.Windows.Components;

internal static class TribeCard
{
    public static void Draw(TribeInfo tribe, AutoTribeController controller, Configuration cfg)
    {
        var disabled = cfg.DisabledTribes.Contains(tribe.BeastTribeId);
        var selected = cfg.SelectedTribes.Contains(tribe.BeastTribeId);
        var selectable = tribe.Unlocked
            && tribe.MeetsRankRequirement
            && tribe.AcceptSlotsRemaining > 0
            && !disabled
            && !controller.Running;

        var startScreen = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var height = Layout.TribeCardHeight * ImGuiHelpers.GlobalScale;
        var endScreen = startScreen + new Vector2(width, height);
        var hovered = ImGui.IsMouseHoveringRect(startScreen, endScreen);

        var border = ResolveBorder(tribe, disabled, selected, controller.Running);
        var bg = ResolveBg(tribe, selected, hovered, disabled);

        using (Card.Begin($"##tribe_{tribe.BeastTribeId}", new Vector2(-1, height), bg, border, selected ? 1.8f : 1.2f))
        {
            DrawHeader(tribe);
            ImGui.Spacing();
            RankBadge.Draw(tribe);
        }

        if (hovered)
        {
            DrawTooltip(tribe, selectable, selected, disabled);
            if (selectable)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    if (selected) cfg.SelectedTribes.Remove(tribe.BeastTribeId);
                    else cfg.SelectedTribes.Add(tribe.BeastTribeId);
                    cfg.SaveDebounced();
                }
            }
        }
    }

    private static void DrawHeader(TribeInfo tribe)
    {
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

    private static void DrawTooltip(TribeInfo tribe, bool selectable, bool selected, bool disabled)
    {
        using var tt = ImRaii.Tooltip();
        if (disabled)
            ImGui.TextUnformatted("Disabled in config");
        else if (!tribe.Unlocked)
            ImGui.TextUnformatted("Tribe not yet unlocked — complete the intro quest in-game first");
        else if (!tribe.MeetsRankRequirement)
            ImGui.TextUnformatted($"Requires rank {tribe.MinRankForDailies} (have {tribe.Rank})");
        else if (tribe.AcceptSlotsRemaining <= 0)
            ImGui.TextUnformatted("All daily slots already used for this tribe today");
        else if (selected)
            ImGui.TextUnformatted("Click to remove from batch run");
        else
            ImGui.TextUnformatted("Click to add to batch run");
    }

    private static Vector4 ResolveBg(TribeInfo tribe, bool selected, bool hovered, bool disabled)
    {
        if (disabled || !tribe.Unlocked) return Styling.CardBg * 0.6f;
        if (selected && hovered) return Vector4.Lerp(Styling.CardBgHover, Styling.AccentTeal, 0.15f);
        if (selected) return Vector4.Lerp(Styling.CardBg, Styling.AccentTeal, 0.10f);
        if (hovered) return Styling.CardBgHover;
        return Vector4.Lerp(Styling.CardBg, Styling.EraTint(tribe.Era), 1f);
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
