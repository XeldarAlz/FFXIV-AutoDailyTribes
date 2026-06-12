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

        if (!hovered) return;
        using var tt = ImRaii.Tooltip();
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextSecondary))
            ImGui.TextUnformatted("Complete the intro quest in-game to unlock this tribe.");
    }

    // Same card shape as Draw, but dimmed and non-interactive: all daily slots are used, so the
    // card stays in the grid (rather than vanishing into a chip) with a done check in the corner.
    public static void DrawDone(TribeInfo tribe)
    {
        var s = ImGuiHelpers.GlobalScale;
        var startScreen = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var height = Layout.TribeCardHeight * s;
        var hovered = ImGui.IsMouseHoveringRect(startScreen, startScreen + new Vector2(width, height));

        using (ImRaii.PushStyle(ImGuiStyleVar.Alpha, 0.55f))
        using (Card.Begin($"##tribe_{tribe.BeastTribeId}", new Vector2(-1, height),
            Styling.CardBgSoft, Styling.BorderActive * 0.45f))
        {
            DrawBody(tribe, selected: false, hovered: false, selectable: false, locked: false, done: true);
        }

        DrawKindStripe(tribe, startScreen, height, 0.55f);

        if (!hovered) return;
        using var tt = ImRaii.Tooltip();
        if (tribe.CanRankUp)
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.AccentAmber))
                ImGui.TextUnformatted("Daily rep is full — finish the rank-up quest in-game to refresh 3 more dailies today.");
        else
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextSecondary))
                ImGui.TextUnformatted("All daily slots used for this tribe today.");
    }

    private static void DrawBody(TribeInfo tribe, bool selected, bool hovered, bool selectable, bool locked, bool done = false)
    {
        var s = ImGuiHelpers.GlobalScale;
        var origin = ImGui.GetWindowPos();
        var size = ImGui.GetWindowSize();
        var dl = ImGui.GetWindowDrawList();
        var pad = PadX * s;
        var kind = Styling.KindColor(tribe.Kind);

        var plate = 40f * s;
        var plateMin = new Vector2(origin.X + pad, origin.Y + 11f * s);
        var plateMax = plateMin + new Vector2(plate, plate);
        var plateRound = 9f * s;
        dl.AddRectFilled(plateMin, plateMax, ImGui.GetColorU32(Styling.WithAlpha(kind, 0.13f)), plateRound);
        dl.AddRect(plateMin, plateMax, ImGui.GetColorU32(Styling.WithAlpha(kind, 0.32f)), plateRound);

        var icon = 32f * s;
        ImGui.SetCursorScreenPos(plateMin + new Vector2((plate - icon) * 0.5f));
        TribeIcon.Draw(tribe, icon);

        var markHalf = 8f * s;
        var markC = new Vector2(origin.X + size.X - pad - markHalf, plateMin.Y + markHalf);
        if (locked) DrawLockGlyph(markC);
        else if (done) DrawDoneGlyph(markC, markHalf);
        else DrawSelectMark(markC, markHalf, selected, hovered, selectable);

        var textX = plateMax.X + 11f * s;
        ImGui.SetWindowFontScale(1.18f);
        ImGui.SetCursorScreenPos(new Vector2(textX, plateMin.Y - 1f * s));
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextStrong))
            ImGui.TextUnformatted(tribe.Name);
        ImGui.SetWindowFontScale(1f);

        ImGui.SetCursorScreenPos(new Vector2(textX, plateMax.Y - ImGui.GetTextLineHeight() - 1f * s));
        using (ImRaii.PushFont(UiBuilder.IconFont))
        using (ImRaii.PushColor(ImGuiCol.Text, kind))
            ImGui.TextUnformatted(KindIcon.Icon(tribe.Kind).ToIconString());
        ImGui.SameLine(0, 5f * s);
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextSecondary))
            ImGui.TextUnformatted(tribe.Kind.ToString());

        DrawDataRows(tribe, origin, size, locked);
    }

    // Rounded-square checkbox: teal fill + dark check when selected; hollow outline (teal "+" hint
    // on hover) when it can be added to the run; nothing while a run is active so the card reads
    // as read-only.
    private static void DrawSelectMark(Vector2 center, float half, bool selected, bool hovered, bool selectable)
    {
        var s = ImGuiHelpers.GlobalScale;
        var dl = ImGui.GetWindowDrawList();
        var min = center - new Vector2(half);
        var max = center + new Vector2(half);
        var round = 4.5f * s;
        if (selected)
        {
            dl.AddRectFilled(min, max, ImGui.GetColorU32(Styling.AccentTeal), round);
            var check = ImGui.GetColorU32(new Vector4(0.05f, 0.07f, 0.08f, 1f));
            dl.AddLine(center + new Vector2(-0.42f, 0.02f) * half, center + new Vector2(-0.12f, 0.34f) * half, check, 2.2f * s);
            dl.AddLine(center + new Vector2(-0.12f, 0.34f) * half, center + new Vector2(0.46f, -0.34f) * half, check, 2.2f * s);
            return;
        }

        if (!selectable) return;
        dl.AddRect(min, max, ImGui.GetColorU32(Styling.WithAlpha(Styling.TextSecondary, hovered ? 0.95f : 0.45f)), round, 0, 1.6f * s);
        if (hovered)
        {
            var plus = ImGui.GetColorU32(Styling.AccentTealSoft);
            dl.AddLine(center - new Vector2(half * 0.45f, 0), center + new Vector2(half * 0.45f, 0), plus, 1.8f * s);
            dl.AddLine(center - new Vector2(0, half * 0.45f), center + new Vector2(0, half * 0.45f), plus, 1.8f * s);
        }
    }

    private static void DrawDoneGlyph(Vector2 center, float half)
    {
        var s = ImGuiHelpers.GlobalScale;
        var dl = ImGui.GetWindowDrawList();
        var check = ImGui.GetColorU32(Styling.AccentMint);
        dl.AddLine(center + new Vector2(-0.42f, 0.02f) * half, center + new Vector2(-0.12f, 0.34f) * half, check, 2.2f * s);
        dl.AddLine(center + new Vector2(-0.12f, 0.34f) * half, center + new Vector2(0.46f, -0.34f) * half, check, 2.2f * s);
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

    // Two aligned label · bar · value rows anchored to the card bottom: daily quest slots on top,
    // reputation toward the next rank below. Labels and values share fixed columns so both bars
    // start and end on the same edges.
    // Slot segments encode delivery, not acceptance: solid mint = turned in, hollow amber = accepted
    // but still in the journal, faint = unused. The value counts only turned-in quests so "3/3"
    // always means the dailies are actually finished.
    private static void DrawDataRows(TribeInfo tribe, Vector2 origin, Vector2 size, bool locked)
    {
        var s = ImGuiHelpers.GlobalScale;
        var pad = PadX * s;
        var max = AdtConstants.MaxAcceptsPerTribe;
        var accepted = Math.Clamp(tribe.AcceptedTodayCount, 0, max);
        var done = Math.Clamp(accepted - tribe.InProgressQuestIds.Length, 0, max);
        var journal = Math.Min(tribe.InProgressQuestIds.Length, max - done);
        var (fraction, maxed) = RankBadge.Rep(tribe);

        var stateColor = done >= max ? Styling.AccentMint
            : done > 0 || journal > 0 ? Styling.AccentAmber
            : Styling.TextDim;

        const string dailyLabel = "Dailies";
        var rankName = RankBadge.RankName(tribe);
        var rankLabel = locked ? "Rank" : rankName.Length > 0 ? rankName : $"Rank {tribe.Rank}";
        var dailyValue = $"{done}/{max}";
        var repValue = locked ? "–" : maxed ? "MAX" : $"{(int)MathF.Round(fraction * 100f)}%";

        var labelW = MathF.Max(ImGui.CalcTextSize(dailyLabel).X, ImGui.CalcTextSize(rankLabel).X);
        var valueW = MathF.Max(ImGui.CalcTextSize(dailyValue).X, ImGui.CalcTextSize(repValue).X);

        var lineH = ImGui.GetTextLineHeight();
        var row2Y = origin.Y + size.Y - 11f * s - lineH;
        var row1Y = row2Y - 6f * s - lineH;
        var colGap = 9f * s;
        var barX0 = origin.X + pad + labelW + colGap;
        var barX1 = origin.X + size.X - pad - valueW - colGap;
        var valueX = origin.X + size.X - pad;
        if (barX1 <= barX0) return;

        var dl = ImGui.GetWindowDrawList();
        var track = Styling.WithAlpha(Styling.TextSecondary, 0.13f);

        DrawRowText(origin.X + pad, row1Y, dailyLabel, journal > 0 ? Styling.AccentAmber : Styling.TextDim);
        DrawRowText(valueX - ImGui.CalcTextSize(dailyValue).X, row1Y, dailyValue, stateColor);

        var segH = 7f * s;
        var segGap = 5f * s;
        var segY = row1Y + (lineH - segH) * 0.5f;
        var segW = (barX1 - barX0 - segGap * (max - 1)) / max;
        for (var i = 0; i < max; i++)
        {
            var sx = barX0 + i * (segW + segGap);
            var segMin = new Vector2(sx, segY);
            var segMax = new Vector2(sx + segW, segY + segH);
            if (i < done)
            {
                dl.AddRectFilled(segMin, segMax, ImGui.GetColorU32(Styling.AccentMint), segH * 0.5f);
            }
            else if (i < done + journal)
            {
                var outline = Styling.PulseColor(Styling.AccentAmber,
                    Styling.WithAlpha(Styling.AccentAmber, 0.55f), Styling.PulseBreath);
                dl.AddRectFilled(segMin, segMax, ImGui.GetColorU32(Styling.WithAlpha(Styling.AccentAmber, 0.15f)), segH * 0.5f);
                dl.AddRect(segMin, segMax, ImGui.GetColorU32(outline), segH * 0.5f, 0, 1.3f * s);
            }
            else
            {
                dl.AddRectFilled(segMin, segMax, ImGui.GetColorU32(track), segH * 0.5f);
            }
        }

        DrawRowText(origin.X + pad, row2Y, rankLabel, Styling.TextDim);
        DrawRowText(valueX - ImGui.CalcTextSize(repValue).X, row2Y, repValue,
            maxed ? Styling.AccentAmber : Styling.TextSecondary);

        var barH = 5f * s;
        var barY = row2Y + (lineH - barH) * 0.5f;
        dl.AddRectFilled(new Vector2(barX0, barY), new Vector2(barX1, barY + barH),
            ImGui.GetColorU32(track), barH * 0.5f);
        var repFill = maxed ? 1f : fraction;
        if (repFill > 0f && !locked)
            dl.AddRectFilled(new Vector2(barX0, barY), new Vector2(barX0 + (barX1 - barX0) * repFill, barY + barH),
                ImGui.GetColorU32(maxed ? Styling.AccentAmber : Styling.AccentTeal), barH * 0.5f);
    }

    private static void DrawRowText(float x, float y, string text, Vector4 color)
    {
        ImGui.SetCursorScreenPos(new Vector2(x, y));
        using (ImRaii.PushColor(ImGuiCol.Text, color))
            ImGui.TextUnformatted(text);
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

    // The card shows the rank name; the hover carries the numeric rank, says what a click will do,
    // plus the gatherer class-binding caveat (the one kind where behavior differs).
    private static void DrawTooltip(TribeInfo tribe, bool selected)
    {
        using var tt = ImRaii.Tooltip();
        using var wrap = ImRaii.TextWrapPos(ImGui.GetCursorPosX() + 280f * ImGuiHelpers.GlobalScale);

        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
            ImGui.TextUnformatted(RankBadge.RankLabel(tribe));

        if (tribe.HasInProgressQuests)
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.AccentAmber))
                ImGui.TextUnformatted($"{tribe.InProgressQuestIds.Length} accepted quest(s) still in journal — click to run them.");
        else if (selected)
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.AccentTeal))
                ImGui.TextUnformatted("Selected — click to remove from the run");
        else
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.AccentTealSoft))
                ImGui.TextUnformatted("Click to add to the run");

        if (tribe.DailiesRefreshedByRankUp)
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.AccentMint))
                ImGui.TextUnformatted("Ranked up today — 3 fresh dailies are available.");

        if (tribe.Kind == TribeKind.Gatherer)
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
                ImGui.TextUnformatted("Gathering dailies bind to the class you accept them with.");
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
