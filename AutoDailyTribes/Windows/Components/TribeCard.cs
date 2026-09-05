using AutoDailyTribes.Core;
using AutoDailyTribes.Core.Tasks;
using AutoDailyTribes.Core.Tribes;
using AutoDailyTribes.Windows.Sections;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using System.Numerics;

namespace AutoDailyTribes.Windows.Components;

internal static class TribeCard
{
    private const float PadX = 14f;
    private const float PadTop = 12f;
    private const float PlateRounding = 11f;
    private const float PlateGlowRadius = 0.46f;
    private const float StripeWidth = 3f;
    private const float StripeInset = 10f;
    private const float StripeOffset = 2.5f;
    private const float SelectorRadius = 10f;
    private const float RowGap = 7f;
    private const float RowPadBottom = 12f;
    private const float SegmentHeight = 8f;
    private const float SegmentGap = 5f;
    private const float RepBarHeight = 6f;
    private const float ColumnGap = 10f;
    private const float TextGap = 12f;
    private const float StatusIconGap = 5f;

    private const string DailiesLabel = "Dailies";
    private const string RankLabelLocked = "Rank";
    private const string RepLocked = "–";
    private const string RepMaxed = "MAX";
    private const string StatusDone = "Done today";
    private const string StatusLocked = "Locked";

    private static readonly string[] SlotLabels = BuildSlotLabels();
    private static readonly string[] QueueLabels = BuildQueueLabels();
    private static readonly string[] RankNeeded = BuildRankNeeded();

    public static void Draw(TribeInfo tribe, Configuration cfg, AutoTribeController controller, float width, int queuePosition)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var size = new Vector2(width, Layout.TribeCardHeight * scale);
        var origin = ImGui.GetCursorScreenPos();
        var end = origin + size;

        var locked = !tribe.Unlocked;
        var underRank = tribe.Unlocked && !tribe.MeetsRankRequirement;
        var runnable = RunPlan.IsRunnable(tribe);
        var done = tribe.Unlocked && tribe.MeetsRankRequirement && !runnable;
        var selected = queuePosition > 0;
        var interactive = !locked && !underRank && !controller.Running;

        ImGui.PushID((nint)tribe.BeastTribeId);
        var hit = Hit.Area("##tribe", size, interactive);
        var hover = Motion.Hover(Motion.Key("##tribe"), hit.Hovered);
        var active = Motion.Approach(Motion.Key("##tribe", 1), selected ? 1f : 0f, 14f);
        ImGui.PopID();

        if (hit.Clicked) Toggle(cfg, tribe.BeastTribeId, !selected);

        var alpha = locked ? 0.55f : underRank ? 0.72f : done ? 0.82f : 1f;
        using (Motion.PushAlpha(alpha))
        {
            var dl = ImGui.GetWindowDrawList();
            var kind = Styling.KindColor(tribe.Kind);
            var rounding = Styling.CardRounding * scale;
            if (active > 0.01f) Paint.Glow(dl, origin, end, rounding, Styling.AccentTeal, 0.45f * active);
            Paint.Glass(dl, origin, end, rounding, Styling.AccentTeal, 0.02f + 0.16f * active, hover);
            DrawKindStripe(dl, origin, end, kind);
            var plateMax = DrawPlate(dl, tribe, origin, kind);
            var cornerLeft = DrawCorner(dl, origin, end, locked, underRank, selected, queuePosition, active, hover);
            DrawIdentity(dl, tribe, origin, end, plateMax, cornerLeft, kind, done, locked, underRank, active, hover);
            DrawDataRows(dl, tribe, origin, end, locked);
        }

        if (hit.Hovered || (!interactive && Hit.HoveringRect(origin, end)))
        {
            DrawTooltip(tribe, selected, done, locked, underRank);
        }
    }

    private static void Toggle(Configuration cfg, uint beastTribeId, bool selected)
    {
        if (selected && !cfg.SelectedTribes.Contains(beastTribeId)) cfg.SelectedTribes.Add(beastTribeId);
        else if (!selected) cfg.SelectedTribes.Remove(beastTribeId);
        cfg.SaveDebounced();
    }

    private static void DrawKindStripe(ImDrawListPtr dl, Vector2 origin, Vector2 end, Vector4 kind)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var x = origin.X + StripeOffset * scale;
        var inset = StripeInset * scale;
        Paint.Fill(dl, new Vector2(x, origin.Y + inset), new Vector2(x + StripeWidth * scale, end.Y - inset),
            Styling.WithAlpha(kind, 0.85f), StripeWidth * scale * 0.5f);
    }

    // A gradient plate tinted by the tribe kind with a soft bloom behind the emblem, so the icon
    // reads as sitting on a lit surface rather than pasted flat onto the card.
    private static Vector2 DrawPlate(ImDrawListPtr dl, TribeInfo tribe, Vector2 origin, Vector4 kind)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var plate = Layout.TribeIconPlate * scale;
        var plateMin = new Vector2(origin.X + PadX * scale, origin.Y + PadTop * scale);
        var plateMax = plateMin + new Vector2(plate, plate);
        var rounding = PlateRounding * scale;
        var center = (plateMin + plateMax) * 0.5f;

        Paint.Gradient(dl, plateMin, plateMax,
            Styling.WithAlpha(Styling.Tint(Styling.Surface2, kind, 0.38f), 0.96f),
            Styling.WithAlpha(Styling.Tint(Styling.Surface0, kind, 0.18f), 0.96f), rounding);
        dl.AddCircleFilled(center, plate * PlateGlowRadius, Paint.Col(Styling.WithAlpha(kind, 0.22f)));
        Paint.TopLight(dl, plateMin, plateMax, rounding, 0.12f);
        Paint.Stroke(dl, plateMin, plateMax, Styling.WithAlpha(kind, 0.42f), rounding);

        var icon = Layout.TribeIconSize * scale;
        TribeIcon.Draw(dl, tribe, center - new Vector2(icon * 0.5f, icon * 0.5f), icon);
        return plateMax;
    }

    // Top-right corner: a lock for locked tribes, an hourglass under the rank floor, and otherwise a
    // selector disc whose fill carries the run-order number so selection and order read as one glyph.
    private static float DrawCorner(ImDrawListPtr dl, Vector2 origin, Vector2 end,
        bool locked, bool underRank, bool selected, int queuePosition, float active, float hover)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var radius = SelectorRadius * scale;
        var center = new Vector2(end.X - PadX * scale - radius, origin.Y + PadTop * scale + radius);

        if (locked)
        {
            TextDraw.IconCentered(FontAwesomeIcon.Lock, center, Styling.TextMuted);
            return center.X - radius;
        }

        if (underRank)
        {
            TextDraw.IconCentered(FontAwesomeIcon.Hourglass, center, Styling.WithAlpha(Styling.AccentAmber, 0.7f));
            return center.X - radius;
        }

        var ring = Vector4.Lerp(Styling.WithAlpha(Styling.BorderDim, 0.9f), Styling.AccentTealSoft, MathF.Max(active, hover * 0.5f));
        if (!selected && hover > 0.01f)
        {
            dl.AddCircleFilled(center, radius, Paint.Col(Styling.WithAlpha(Styling.Surface3, hover)));
        }

        dl.AddCircle(center, radius, Paint.Col(ring), 0, 1.4f * scale);
        if (active > 0.01f)
        {
            dl.AddCircleFilled(center, radius * active, Paint.Col(Styling.AccentTeal));
        }

        if (active > 0.5f && queuePosition > 0)
        {
            using (Fonts.PushCaption())
            {
                var label = queuePosition < QueueLabels.Length ? QueueLabels[queuePosition] : queuePosition.ToString();
                TextDraw.Middle(label, center - new Vector2(radius, radius), center + new Vector2(radius, radius),
                    Styling.WithAlpha(Styling.WindowBg with { W = 1f }, (active - 0.5f) * 2f));
            }
        }

        return center.X - radius;
    }

    private static void DrawIdentity(ImDrawListPtr dl, TribeInfo tribe, Vector2 origin, Vector2 end, Vector2 plateMax, float cornerLeft,
        Vector4 kind, bool done, bool locked, bool underRank, float active, float hover)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var textX = plateMax.X + TextGap * scale;
        var nameY = origin.Y + PadTop * scale + 1f * scale;
        var nameColor = locked ? Styling.TextMuted : Vector4.Lerp(Styling.TextSecondary, Styling.TextStrong, MathF.Max(active, hover));
        var name = TextDraw.Truncate(tribe.Name, cornerLeft - 10f * scale - textX);
        TextDraw.At(name, new Vector2(textX, nameY), nameColor);

        using (Fonts.PushCaption())
        {
            var lineHeight = ImGui.GetTextLineHeight();
            var lineY = plateMax.Y - lineHeight - 1f * scale;
            var kindIcon = KindIcon.Icon(tribe.Kind);
            var kindIconSize = TextDraw.IconSize(kindIcon);
            TextDraw.Icon(kindIcon, new Vector2(textX, lineY + (lineHeight - kindIconSize.Y) * 0.5f), kind);
            TextDraw.At(tribe.KindLabel, new Vector2(textX + kindIconSize.X + 6f * scale, lineY), Styling.TextDim);

            var status = done ? StatusDone
                : locked ? StatusLocked
                : underRank ? RankNeeded[Math.Clamp(tribe.MinRankForDailies, 0, RankNeeded.Length - 1)]
                : null;
            if (status is null) return;

            var color = done ? Styling.AccentMint : underRank ? Styling.AccentAmberSoft : Styling.TextMuted;
            var rightX = end.X - PadX * scale;
            TextDraw.Right(status, rightX, lineY, color);
            if (!done) return;

            var statusWidth = TextDraw.Measure(status).X;
            var checkSize = TextDraw.IconSize(FontAwesomeIcon.Check);
            TextDraw.Icon(FontAwesomeIcon.Check, new Vector2(rightX - statusWidth - StatusIconGap * scale - checkSize.X, lineY + (lineHeight - checkSize.Y) * 0.5f), color);
        }
    }

    // Two aligned label · bar · value rows anchored to the card bottom: daily quest slots on top,
    // reputation toward the next rank below. The slot value counts only turned-in quests so "3/3"
    // always means the dailies are actually finished.
    private static void DrawDataRows(ImDrawListPtr dl, TribeInfo tribe, Vector2 origin, Vector2 end, bool locked)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var pad = PadX * scale;
        var max = AdtConstants.MaxAcceptsPerTribe;
        var accepted = Math.Clamp(tribe.AcceptedTodayCount, 0, max);
        var done = Math.Clamp(accepted - tribe.InProgressQuestIds.Length, 0, max);
        var journal = Math.Min(tribe.InProgressQuestIds.Length, max - done);
        var (fraction, maxed) = RankBadge.Rep(tribe);

        var stateColor = done >= max ? Styling.AccentMint
            : done > 0 || journal > 0 ? Styling.AccentAmber
            : Styling.TextDim;

        var rankName = RankBadge.RankName(tribe);
        var rankLabel = locked ? RankLabelLocked : rankName.Length > 0 ? rankName : $"Rank {tribe.Rank}";
        var dailyValue = SlotLabels[done];
        var repValue = locked ? RepLocked : maxed ? RepMaxed : $"{(int)MathF.Round(fraction * 100f)}%";

        using var caption = Fonts.PushCaption();
        var labelWidth = MathF.Max(TextDraw.Measure(DailiesLabel).X, TextDraw.Measure(rankLabel).X);
        var valueWidth = MathF.Max(TextDraw.Measure(dailyValue).X, TextDraw.Measure(repValue).X);

        var lineHeight = ImGui.GetTextLineHeight();
        var row2Y = end.Y - RowPadBottom * scale - lineHeight;
        var row1Y = row2Y - RowGap * scale - lineHeight;
        var columnGap = ColumnGap * scale;
        var barX0 = origin.X + pad + labelWidth + columnGap;
        var barX1 = end.X - pad - valueWidth - columnGap;
        var valueX = end.X - pad;
        if (barX1 <= barX0) return;

        TextDraw.At(DailiesLabel, new Vector2(origin.X + pad, row1Y), journal > 0 ? Styling.AccentAmber : Styling.TextDim);
        TextDraw.Right(dailyValue, valueX, row1Y, stateColor);

        var segmentHeight = SegmentHeight * scale;
        Paint.Segments(dl, new Vector2(barX0, row1Y + (lineHeight - segmentHeight) * 0.5f), barX1 - barX0, segmentHeight,
            max, done, journal, Styling.AccentMint, Styling.AccentAmber, SegmentGap * scale);

        TextDraw.At(rankLabel, new Vector2(origin.X + pad, row2Y), Styling.TextDim);
        TextDraw.Right(repValue, valueX, row2Y, maxed ? Styling.AccentAmber : Styling.TextSecondary);

        var barHeight = RepBarHeight * scale;
        var barY = row2Y + (lineHeight - barHeight) * 0.5f;
        Paint.Bar(dl, new Vector2(barX0, barY), barX1 - barX0, barHeight, locked ? 0f : fraction, maxed ? Styling.AccentAmber : Styling.AccentTeal);
    }

    private static void DrawTooltip(TribeInfo tribe, bool selected, bool done, bool locked, bool underRank)
    {
        using var tooltip = Tooltip.Begin();

        if (locked)
        {
            Tooltip.Text("Complete the intro quest in game to unlock this tribe.");
            return;
        }

        Tooltip.Text(RankBadge.RankLabel(tribe), Styling.TextDim);

        if (underRank)
        {
            Tooltip.Text($"Reach rank {tribe.MinRankForDailies} to run dailies.", Styling.AccentAmberSoft);
            return;
        }

        Tooltip.Text($"{tribe.AcceptedTodayCount} / {AdtConstants.MaxAcceptsPerTribe} daily slots used · "
                   + $"{AdtConstants.DailyAllowanceCap} allowances shared across all tribes.", Styling.TextDim);

        if (done)
        {
            Tooltip.Text("All daily slots used for this tribe today.", Styling.AccentMint);
            if (tribe.CanRankUp) Tooltip.Text("Daily rep is full. Finish the rank-up quest in game to refresh 3 more dailies today.", Styling.AccentAmberSoft);
            Tooltip.Text(selected
                ? "Still in your list, so it runs again after the reset. Click to drop it."
                : "Click to keep it in your list for after the reset.", Styling.AccentTealSoft);
            return;
        }

        if (tribe.HasInProgressQuests) Tooltip.Text($"{tribe.InProgressQuestIds.Length} accepted quest(s) still in the journal. Click to run them.", Styling.AccentAmberSoft);
        else if (selected) Tooltip.Text("In your list. Click to remove it from the run.", Styling.AccentTealSoft);
        else Tooltip.Text("Click to add it to the run.", Styling.AccentTealSoft);

        if (tribe.DailiesRefreshedByRankUp) Tooltip.Text("Ranked up today, so 3 fresh dailies are available.", Styling.AccentMint);
        if (tribe.Kind == TribeKind.Gatherer) Tooltip.Text("Gathering dailies bind to the class you accept them with.", Styling.TextDim);
    }

    private static string[] BuildSlotLabels()
    {
        var labels = new string[AdtConstants.MaxAcceptsPerTribe + 1];
        for (var index = 0; index < labels.Length; index++) labels[index] = $"{index}/{AdtConstants.MaxAcceptsPerTribe}";
        return labels;
    }

    private static string[] BuildQueueLabels()
    {
        var labels = new string[TribeRegistry.Tribes.Length + 1];
        for (var index = 0; index < labels.Length; index++) labels[index] = index.ToString();
        return labels;
    }

    private static string[] BuildRankNeeded()
    {
        var labels = new string[AdtConstants.MaxTribeRank + 1];
        for (var index = 0; index < labels.Length; index++) labels[index] = $"Rank {index} needed";
        return labels;
    }
}
