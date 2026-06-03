using AutoDailyTribes.Core;
using AutoDailyTribes.Core.Tasks;
using AutoDailyTribes.Core.Tribes;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoDailyTribes.Windows.Components;

internal static class TribeCard
{
    public static void Draw(TribeInfo tribe, AutoTribeController controller, Configuration cfg)
    {
        var hasWork = tribe.AcceptSlotsRemaining > 0 || tribe.HasInProgressQuests;
        var doneToday = tribe.Unlocked && tribe.MeetsRankRequirement && !hasWork;

        if (doneToday && cfg.SelectedTribes.Remove(tribe.BeastTribeId))
            cfg.SaveDebounced();

        var selected = cfg.SelectedTribes.Contains(tribe.BeastTribeId);
        var selectable = tribe.Unlocked
            && tribe.MeetsRankRequirement
            && hasWork
            && !controller.Running;

        var startScreen = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var height = Layout.TribeCardHeight * ImGuiHelpers.GlobalScale;
        var endScreen = startScreen + new Vector2(width, height);
        var hovered = ImGui.IsMouseHoveringRect(startScreen, endScreen);

        var border = ResolveBorder(tribe, selected, controller.Running);
        var bg = ResolveBg(tribe, selected, hovered);

        using (Card.Begin($"##tribe_{tribe.BeastTribeId}", new Vector2(-1, height), bg, border, selected ? 1.8f : 1.2f))
        {
            DrawHeader(tribe);
            ImGui.Spacing();
            RankBadge.Draw(tribe);
        }

        if (hovered)
        {
            DrawTooltip(tribe, selected);
            if (selectable)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    if (selected) cfg.SelectedTribes.Remove(tribe.BeastTribeId);
                    else if (!cfg.SelectedTribes.Contains(tribe.BeastTribeId))
                        cfg.SelectedTribes.Add(tribe.BeastTribeId);
                    cfg.SaveDebounced();
                }
            }
        }
    }

    private static void DrawHeader(TribeInfo tribe)
    {
        TribeIcon.Draw(tribe);
        ImGui.SameLine();

        ImGui.SetWindowFontScale(1.10f);
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextStrong))
        {
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(tribe.Name);
        }
        ImGui.SetWindowFontScale(1.0f);

        var pillLabel = AllowancePill.GetLabel(tribe);
        var pillWidth = ImGui.CalcTextSize(pillLabel).X + 16 * ImGuiHelpers.GlobalScale;
        ImGui.SameLine(ImGui.GetContentRegionAvail().X + ImGui.GetCursorPosX() - pillWidth);
        AllowancePill.Draw(tribe);
    }

    private static void DrawTooltip(TribeInfo tribe, bool selected)
    {
        using var tt = ImRaii.Tooltip();
        if (!tribe.Unlocked)
            ImGui.TextUnformatted("Tribe not yet unlocked — complete the intro quest in-game first");
        else if (!tribe.MeetsRankRequirement)
            ImGui.TextUnformatted($"Requires rank {tribe.MinRankForDailies} (have {tribe.Rank})");
        else if (tribe.AllSlotsDone && tribe.CanRankUp)
            ImGui.TextUnformatted("Daily quests done — rep bar is full. Visit the issuer manually to pick up the rank-up quest.");
        else if (tribe.AllSlotsDone)
            ImGui.TextUnformatted("All daily slots already used for this tribe today");
        else if (tribe.AcceptSlotsRemaining <= 0)
            ImGui.TextUnformatted($"Slots maxed — {tribe.InProgressQuestIds.Length} quest(s) still in journal. Click to run Questionable on them.");
        else if (selected)
            ImGui.TextUnformatted("Click to remove from batch run");
        else
            ImGui.TextUnformatted("Click to add to batch run");
    }

    private static Vector4 ResolveBg(TribeInfo tribe, bool selected, bool hovered)
    {
        if (!tribe.Unlocked) return Styling.CardBg * 0.6f;
        if (tribe.AllSlotsDone) return Styling.CardBg * 0.6f;
        if (selected && hovered) return Vector4.Lerp(Styling.CardBgHover, Styling.AccentTeal, 0.15f);
        if (selected) return Vector4.Lerp(Styling.CardBg, Styling.AccentTeal, 0.10f);
        if (hovered) return Styling.CardBgHover;
        return Vector4.Lerp(Styling.CardBg, Styling.EraTint(tribe.Era), 1f);
    }

    private static Vector4 ResolveBorder(TribeInfo tribe, bool selected, bool running)
    {
        if (!tribe.Unlocked) return Styling.BorderLocked;
        if (running) return Styling.PulseColor(Styling.BorderActive, Styling.AccentTealSoft, Styling.PulseMedium);
        if (selected) return Styling.AccentTeal;
        if (tribe.AllSlotsDone) return Styling.BorderDim;
        return Styling.BorderActive * 0.65f;
    }
}
