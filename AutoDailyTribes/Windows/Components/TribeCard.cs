using AutoDailyTribes.Core;
using AutoDailyTribes.Core.Tasks;
using AutoDailyTribes.Core.Tribes;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoDailyTribes.Windows.Components;

internal static class TribeCard
{
    private const float PadX = 13f;

    public static void Draw(TribeInfo tribe, AutoTribeController controller, Configuration cfg)
    {
        var selected = cfg.SelectedTribes.Contains(tribe.BeastTribeId);
        var selectable = tribe.Unlocked && tribe.MeetsRankRequirement && !tribe.AllSlotsDone && !controller.Running;

        var s = ImGuiHelpers.GlobalScale;
        var startScreen = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var height = Layout.TribeCardHeight * s;
        var hovered = ImGui.IsMouseHoveringRect(startScreen, startScreen + new Vector2(width, height));

        using (Card.Begin($"##tribe_{tribe.BeastTribeId}", new Vector2(-1, height),
            ResolveBg(selected, hovered), ResolveBorder(selected, hovered), selected ? 2f : 1f))
        {
            DrawBody(tribe, selected, hovered, selectable, locked: false);
        }

        DrawKindStripe(tribe, startScreen, height, 1f);
        DrawRepLine(tribe, startScreen, width, height, 1f);

        if (!hovered) return;
        DrawTooltip(tribe, selected);
        if (!selectable) return;

        ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            if (selected) cfg.SelectedTribes.Remove(tribe.BeastTribeId);
            else cfg.SelectedTribes.Add(tribe.BeastTribeId);
            cfg.SaveDebounced();
        }
    }

    // Same card shape as Draw, but greyed out and non-interactive: a tribe the player hasn't unlocked
    // yet still occupies the grid so they can see what's coming and why it isn't selectable.
    public static void DrawLocked(TribeInfo tribe)
    {
        var s = ImGuiHelpers.GlobalScale;
        var startScreen = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var height = Layout.TribeCardHeight * s;
        var hovered = ImGui.IsMouseHoveringRect(startScreen, startScreen + new Vector2(width, height));

        using (ImRaii.PushStyle(ImGuiStyleVar.Alpha, 0.45f))
        using (Card.Begin($"##tribe_{tribe.BeastTribeId}", new Vector2(-1, height),
            Styling.CardBgSoft, Styling.BorderLocked))
        {
            DrawBody(tribe, selected: false, hovered: false, selectable: false, locked: true);
        }

        DrawKindStripe(tribe, startScreen, height, 0.45f);
        DrawRepLine(tribe, startScreen, width, height, 0.45f);

        if (!hovered) return;
        using var tt = ImRaii.Tooltip();
        ImGui.TextUnformatted($"{tribe.Name} — locked");
        ImGui.Separator();
        ImGui.TextUnformatted("Complete the intro quest in-game to unlock this tribe.");
    }

    private static void DrawBody(TribeInfo tribe, bool selected, bool hovered, bool selectable, bool locked)
    {
        var s = ImGuiHelpers.GlobalScale;
        var origin = ImGui.GetWindowPos();
        var size = ImGui.GetWindowSize();
        var dl = ImGui.GetWindowDrawList();
        var pad = PadX * s;
        var kind = Styling.KindColor(tribe.Kind);

        var plate = 44f * s;
        var plateMin = new Vector2(origin.X + pad, origin.Y + 11f * s);
        var plateMax = plateMin + new Vector2(plate, plate);
        var plateRound = 9f * s;
        dl.AddRectFilled(plateMin, plateMax, ImGui.GetColorU32(Styling.WithAlpha(kind, 0.13f)), plateRound);
        dl.AddRect(plateMin, plateMax, ImGui.GetColorU32(Styling.WithAlpha(kind, 0.32f)), plateRound);

        var icon = 36f * s;
        ImGui.SetCursorScreenPos(plateMin + new Vector2((plate - icon) * 0.5f));
        TribeIcon.Draw(tribe, icon);

        var markR = 8.5f * s;
        var markC = new Vector2(origin.X + size.X - pad - markR, plateMin.Y + markR);
        if (locked) DrawLockGlyph(markC);
        else DrawSelectMark(markC, markR, selected, hovered, selectable);

        var textX = plateMax.X + 11f * s;
        ImGui.SetWindowFontScale(1.18f);
        ImGui.SetCursorScreenPos(new Vector2(textX, plateMin.Y + 1f * s));
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextStrong))
            ImGui.TextUnformatted(tribe.Name);
        ImGui.SetWindowFontScale(1f);

        var chipH = ImGui.GetTextLineHeight() + 2f * s;
        ImGui.SetCursorScreenPos(new Vector2(textX, plateMax.Y - chipH));
        Styling.IconChip(KindIcon.Icon(tribe.Kind), tribe.Kind.ToString(), kind);
        ImGui.SameLine(0, 8f * s);
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextSecondary))
            ImGui.TextUnformatted(RankBadge.RankLabel(tribe));

        DrawDailies(tribe, origin, size);
    }

    // Teal disc + dark check when selected; hollow ring (with a "+" hint on hover) when it can be
    // added to the run; nothing while a run is active so the card reads as read-only.
    private static void DrawSelectMark(Vector2 center, float r, bool selected, bool hovered, bool selectable)
    {
        var s = ImGuiHelpers.GlobalScale;
        var dl = ImGui.GetWindowDrawList();
        if (selected)
        {
            dl.AddCircleFilled(center, r, ImGui.GetColorU32(Styling.AccentTeal));
            var check = ImGui.GetColorU32(new Vector4(0.05f, 0.07f, 0.08f, 1f));
            dl.AddLine(center + new Vector2(-0.36f, 0.02f) * r, center + new Vector2(-0.10f, 0.30f) * r, check, 2.1f * s);
            dl.AddLine(center + new Vector2(-0.10f, 0.30f) * r, center + new Vector2(0.40f, -0.30f) * r, check, 2.1f * s);
            return;
        }

        if (!selectable) return;
        dl.AddCircle(center, r, ImGui.GetColorU32(Styling.WithAlpha(Styling.TextSecondary, hovered ? 0.95f : 0.45f)), 0, 1.8f * s);
        if (hovered)
        {
            var plus = ImGui.GetColorU32(Styling.AccentTealSoft);
            dl.AddLine(center - new Vector2(r * 0.45f, 0), center + new Vector2(r * 0.45f, 0), plus, 1.8f * s);
            dl.AddLine(center - new Vector2(0, r * 0.45f), center + new Vector2(0, r * 0.45f), plus, 1.8f * s);
        }
    }

    private static void DrawLockGlyph(Vector2 center)
    {
        var icon = FontAwesomeIcon.Lock.ToIconString();
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            var size = ImGui.CalcTextSize(icon);
            ImGui.SetCursorScreenPos(center - size * 0.5f);
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextMuted))
                ImGui.TextUnformatted(icon);
        }
    }

    // Eyebrow + counter row, then three wide segments — one per daily accept slot. Mint when the day
    // is complete, amber while partial; pulses when slots are spent but quests still sit in the journal.
    private static void DrawDailies(TribeInfo tribe, Vector2 origin, Vector2 size)
    {
        var s = ImGuiHelpers.GlobalScale;
        var pad = PadX * s;
        var max = AdtConstants.MaxAcceptsPerTribe;
        var accepted = Math.Clamp(tribe.AcceptedTodayCount, 0, max);
        var inJournal = tribe.AcceptSlotsRemaining <= 0 && tribe.HasInProgressQuests;

        var stateColor = accepted >= max ? Styling.AccentMint
            : accepted > 0 ? Styling.AccentAmber
            : Styling.TextSecondary;

        var rowTop = origin.Y + size.Y - 50f * s;

        var count = $"{accepted}/{max}";
        var countSize = ImGui.CalcTextSize(count);
        ImGui.SetCursorScreenPos(new Vector2(origin.X + size.X - pad - countSize.X, rowTop));
        using (ImRaii.PushColor(ImGuiCol.Text, stateColor))
            ImGui.TextUnformatted(count);

        ImGui.SetWindowFontScale(0.8f);
        var labelH = ImGui.GetTextLineHeight();
        ImGui.SetCursorScreenPos(new Vector2(origin.X + pad, rowTop + (countSize.Y - labelH) * 0.5f));
        using (ImRaii.PushColor(ImGuiCol.Text, inJournal ? Styling.AccentAmber : Styling.TextDim))
            ImGui.TextUnformatted(inJournal ? "IN JOURNAL" : "DAILIES");
        ImGui.SetWindowFontScale(1f);

        var dl = ImGui.GetWindowDrawList();
        var segTop = origin.Y + size.Y - 26f * s;
        var segH = 6f * s;
        var gap = 5f * s;
        var x0 = origin.X + pad;
        var segW = (size.X - pad * 2f - gap * (max - 1)) / max;
        var fill = inJournal
            ? Styling.PulseColor(Styling.AccentMint, Styling.AccentMintSoft, Styling.PulseBreath)
            : stateColor;
        for (var i = 0; i < max; i++)
        {
            var sx = x0 + i * (segW + gap);
            var col = i < accepted ? fill : Styling.WithAlpha(Styling.TextSecondary, 0.13f);
            dl.AddRectFilled(new Vector2(sx, segTop), new Vector2(sx + segW, segTop + segH),
                ImGui.GetColorU32(col), segH * 0.5f);
        }
    }

    // Slim kind-coloured pill hugging the left edge — lets the eye sort the grid by tribe type
    // without reading the chips.
    private static void DrawKindStripe(TribeInfo tribe, Vector2 cardOrigin, float height, float alphaMul)
    {
        var s = ImGuiHelpers.GlobalScale;
        var w = 3.5f * s;
        var insetY = 9f * s;
        var x = cardOrigin.X + 2.5f * s;
        ImGui.GetWindowDrawList().AddRectFilled(
            new Vector2(x, cardOrigin.Y + insetY),
            new Vector2(x + w, cardOrigin.Y + height - insetY),
            ImGui.GetColorU32(Styling.WithAlpha(Styling.KindColor(tribe.Kind), 0.85f * alphaMul)), w * 0.5f);
    }

    // Thin, text-free reputation line pinned to the card's bottom edge (exact % lives in the tooltip).
    private static void DrawRepLine(TribeInfo tribe, Vector2 cardOrigin, float width, float height, float alphaMul)
    {
        var s = ImGuiHelpers.GlobalScale;
        var (fraction, maxed) = RankBadge.Rep(tribe);

        var inset = PadX * s;
        var th = 3f * s;
        var y = cardOrigin.Y + height - 8f * s;
        var x0 = cardOrigin.X + inset;
        var x1 = cardOrigin.X + width - inset;
        if (x1 <= x0) return;

        var dl = ImGui.GetWindowDrawList();
        void Bar(float ex, Vector4 col)
            => dl.AddRectFilled(new Vector2(x0, y - th * 0.5f), new Vector2(ex, y + th * 0.5f), ImGui.GetColorU32(col), th * 0.5f);

        Bar(x1, Styling.WithAlpha(Styling.BorderDim, 0.6f * alphaMul));
        if (maxed)
            Bar(x1, Styling.WithAlpha(Styling.AccentAmber, 0.55f * alphaMul));
        else if (fraction > 0f)
            Bar(x0 + (x1 - x0) * fraction, Styling.WithAlpha(Styling.AccentTeal, alphaMul));
    }

    private static void DrawTooltip(TribeInfo tribe, bool selected)
    {
        var s = ImGuiHelpers.GlobalScale;
        var (fraction, maxed) = RankBadge.Rep(tribe);
        var max = AdtConstants.MaxAcceptsPerTribe;
        var accepted = Math.Clamp(tribe.AcceptedTodayCount, 0, max);
        using var tt = ImRaii.Tooltip();

        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextStrong))
            ImGui.TextUnformatted(tribe.Name);
        ImGui.SameLine(0, 6f * s);
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.KindColor(tribe.Kind)))
            ImGui.TextUnformatted(tribe.Kind.ToString());

        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextSecondary))
        {
            ImGui.TextUnformatted(maxed
                ? "Reputation maxed (rank 8)"
                : $"Reputation {(int)MathF.Round(fraction * 100f)}% toward rank {tribe.Rank + 1}");
            ImGui.TextUnformatted($"{accepted} of {max} daily quests picked up today");
        }

        ImGui.Separator();
        if (tribe.AcceptSlotsRemaining <= 0 && tribe.HasInProgressQuests)
            ImGui.TextUnformatted($"Slots maxed — {tribe.InProgressQuestIds.Length} quest(s) still in journal. Click to run them.");
        else if (selected)
            ImGui.TextUnformatted("Selected — click to remove from the run");
        else
            ImGui.TextUnformatted("Click to add to the run");
    }

    private static Vector4 ResolveBg(bool selected, bool hovered)
    {
        if (selected && hovered) return Vector4.Lerp(Styling.CardBgHover, Styling.AccentTeal, 0.30f);
        if (selected) return Vector4.Lerp(Styling.CardBg, Styling.AccentTeal, 0.20f);
        if (hovered) return Styling.CardBgHover;
        return Styling.CardBg;
    }

    private static Vector4 ResolveBorder(bool selected, bool hovered)
        => selected ? Styling.AccentTeal
            : hovered ? Styling.WithAlpha(Styling.BorderActive, 0.70f)
            : Styling.BorderActive * 0.45f;
}
