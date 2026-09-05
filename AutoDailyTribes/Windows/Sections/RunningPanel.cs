using AutoDailyTribes.Core;
using AutoDailyTribes.Core.Tasks;
using AutoDailyTribes.Core.Tribes;
using AutoDailyTribes.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoDailyTribes.Windows.Sections;

internal static class RunningPanel
{
    private enum StepState { Pending, Active, Done }

    private readonly record struct Step(string Label, TribePhase Phase);

    private const float PadX = 18f;
    private const float RingInset = 22f;
    private const float ColumnGap = 20f;
    private const float IdentityIcon = 36f;
    private const float StepBarHeight = 3f;
    private const float StepGap = 8f;
    private const float StepLabelGap = 5f;
    private const int LogRows = 5;

    private const string StatusRunning = "Running";
    private const string RingCaption = "tribes";
    private const string WaitingLabel = "Waiting for the next tribe…";
    private const string WorkingLabel = "Working…";
    private const string UpNextTitle = "Up next";
    private const string ActivityTitle = "Activity";
    private const string TileAllowances = "Allowances";
    private const string TileCurrent = "This tribe";
    private const string TileElapsed = "Elapsed";
    private const string TileReset = "Reset in";

    private static readonly Step[] Steps =
    [
        new("Switch job", TribePhase.SwitchingJob),
        new("Travel", TribePhase.Traveling),
        new("Accept", TribePhase.Accepting),
        new("Run quests", TribePhase.Delegating),
    ];

    public static void Draw(Configuration cfg, AutoTribeController controller)
    {
        var progress = controller.Progress;
        var (accent, accentSoft) = ReadyState.PhasePalette(progress.Phase);
        var label = ReadyState.PhaseLabel(progress.Phase);

        DrawHeaderStrip(progress, accent, accentSoft);
        Styling.VSpace(6f);
        DrawHeroCard(controller, progress, accent, accentSoft, label);

        Styling.VSpace(10f);
        DrawStatTiles(progress);

        Styling.VSpace(10f);
        DrawQueue(progress);

        if (progress.Log.Count == 0) return;
        Styling.VSpace(10f);
        DrawLog(progress);
    }

    // Completed tribes plus a fraction of the in-flight tribe, so the ring keeps moving within a
    // single tribe instead of only jumping on tribe completion.
    public static float SmoothFraction(TribeRunProgress progress)
    {
        if (progress.Total == 0) return 0f;
        return Math.Clamp((progress.Completed + StageFraction(progress.Phase, progress.Current)) / progress.Total, 0f, 1f);
    }

    private static float StageFraction(TribePhase phase, TribeInfo? current) => phase switch
    {
        TribePhase.SwitchingJob => 0.12f,
        TribePhase.Traveling    => 0.35f,
        TribePhase.Accepting    => 0.45f + 0.25f * AcceptFraction(current),
        TribePhase.Delegating   => 0.72f + 0.28f * DelegateFraction(current),
        TribePhase.Recovering   => 0.30f,
        TribePhase.Done         => 1.00f,
        _                       => 0.05f,
    };

    private static float AcceptFraction(TribeInfo? current)
        => current is null ? 0f : Math.Clamp(current.AcceptedTodayCount / (float)AdtConstants.MaxAcceptsPerTribe, 0f, 1f);

    private static float DelegateFraction(TribeInfo? current)
    {
        if (current is null) return 1f;
        var left = current.InProgressQuestIds.Length;
        var total = Math.Max(Math.Max(current.AcceptedTodayCount, left), 1);
        return Math.Clamp(1f - left / (float)total, 0f, 1f);
    }

    private static void DrawHeaderStrip(TribeRunProgress progress, Vector4 accent, Vector4 accentSoft)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var dl = ImGui.GetWindowDrawList();
        var origin = ImGui.GetCursorScreenPos();
        var avail = ImGui.GetContentRegionAvail().X;
        var lineHeight = ImGui.GetTextLineHeight();
        var midY = origin.Y + lineHeight * 0.5f;

        var radius = 4f * scale;
        Paint.Dot(dl, new Vector2(origin.X + radius + 3f * scale, midY), radius, Styling.PulseColor(accent, accentSoft, Styling.PulseMedium));

        var statusSize = TextDraw.SmallCapsSize(StatusRunning);
        TextDraw.SmallCaps(StatusRunning, new Vector2(origin.X + radius * 2f + 12f * scale, midY - statusSize.Y * 0.5f), Styling.TextSecondary);

        var footer = $"Tribe {progress.CurrentNumber} of {Math.Max(progress.Total, 1)} · {Formatting.Clock(progress.ElapsedMs)}";
        using (Fonts.PushCaption())
        {
            var footerSize = TextDraw.Measure(footer);
            TextDraw.At(footer, new Vector2(origin.X + avail - footerSize.X, midY - footerSize.Y * 0.5f), Styling.TextMuted);
        }

        ImGui.Dummy(new Vector2(avail, lineHeight));
    }

    private static void DrawHeroCard(AutoTribeController controller, TribeRunProgress progress, Vector4 accent, Vector4 accentSoft, string label)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var size = new Vector2(ImGui.GetContentRegionAvail().X, Layout.HeroCardHeight * scale);
        var origin = ImGui.GetCursorScreenPos();
        var end = origin + size;
        var dl = ImGui.GetWindowDrawList();
        var rounding = Styling.PanelRounding * scale;

        Paint.Glass(dl, origin, end, rounding, accent, 0.10f, 0f, elevated: true);
        var border = Styling.PulseColor(Styling.WithAlpha(accent, 0.5f), accentSoft, Styling.PulseMedium);
        Paint.Stroke(dl, origin, end, border, rounding, 1.6f);

        var padX = PadX * scale;
        var ringRadius = size.Y * 0.5f - RingInset * scale;
        var ringCenter = new Vector2(origin.X + padX + ringRadius, origin.Y + size.Y * 0.5f);
        DrawRing(ringCenter, ringRadius, accent, accentSoft, progress);

        var columnX = ringCenter.X + ringRadius + ColumnGap * scale;
        var columnRight = end.X - padX;
        var columnWidth = columnRight - columnX;
        var y = origin.Y + 16f * scale;

        y += DrawPhaseChip(columnX, y, label, accent, accentSoft) + 10f * scale;
        y += DrawIdentity(dl, progress.Current, columnX, y, columnWidth) + 10f * scale;
        DrawLiveLine(dl, controller.Status, accentSoft, new Vector2(columnX, y), columnWidth);

        float captionHeight;
        using (Fonts.PushCaption())
            captionHeight = ImGui.GetTextLineHeight();
        var stepperHeight = StepBarHeight * scale + StepLabelGap * scale + captionHeight;
        DrawStepper(dl, progress, columnX, end.Y - 16f * scale - stepperHeight, columnWidth);

        ImGui.Dummy(size);
    }

    private static void DrawRing(Vector2 center, float radius, Vector4 accent, Vector4 accentSoft, TribeRunProgress progress)
    {
        var thickness = 6f * ImGuiHelpers.GlobalScale;
        ProgressRing.Glow(center, radius, accent, 0.35f + 0.30f * Styling.Pulse(Styling.PulseBreath));
        ProgressRing.Track(center, radius, thickness, Styling.WithAlpha(Styling.BorderDim, 0.7f));
        var fraction = Motion.Approach(Motion.Key("##adt_run_ring"), SmoothFraction(progress), 6f);
        ProgressRing.Fill(center, radius, thickness, fraction, accent);
        ProgressRing.Sweep(center, radius, thickness * 0.72f, accentSoft, Styling.PulseOrbit, MathF.PI * 0.5f, 1f);
        ProgressRing.CenterValue(center, $"{progress.Completed} / {Math.Max(progress.Total, 1)}", RingCaption, Styling.TextStrong, Styling.TextDim);
    }

    private static float DrawPhaseChip(float x, float y, string text, Vector4 accent, Vector4 accentSoft)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var dl = ImGui.GetWindowDrawList();
        var padX = 9f * scale;
        var padY = 3f * scale;

        using (Fonts.PushCaption())
        {
            var label = TextDraw.Upper(text);
            var textSize = TextDraw.Measure(label);
            var chipMin = new Vector2(x, y);
            var chipMax = chipMin + new Vector2(padX * 2f + textSize.X, textSize.Y + padY * 2f);
            Paint.Pill(dl, chipMin, chipMax, Styling.WithAlpha(accent, 0.28f), Styling.WithAlpha(accent, 0.65f));
            TextDraw.At(label, new Vector2(x + padX, y + padY), accentSoft);
            return chipMax.Y - chipMin.Y;
        }
    }

    private static float DrawIdentity(ImDrawListPtr dl, TribeInfo? tribe, float x, float y, float width)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var iconSize = IdentityIcon * scale;
        if (tribe is null)
        {
            using (Fonts.PushHeadline())
            {
                var waitingSize = TextDraw.Measure(WaitingLabel);
                TextDraw.At(WaitingLabel, new Vector2(x, y + (iconSize - waitingSize.Y) * 0.5f), Styling.TextDim);
            }

            return iconSize;
        }

        TribeIcon.Draw(dl, tribe, new Vector2(x, y), iconSize);
        var textX = x + iconSize + 10f * scale;
        using (Fonts.PushHeadline())
        {
            var name = TextDraw.Truncate(tribe.Name, width - (textX - x));
            var nameSize = TextDraw.Measure(name);
            TextDraw.At(name, new Vector2(textX, y + (iconSize - nameSize.Y) * 0.5f), Styling.TextStrong);
        }

        return iconSize;
    }

    private static void DrawLiveLine(ImDrawListPtr dl, string status, Vector4 accent, Vector2 position, float width)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var dotRadius = 3.5f * scale;
        var midY = position.Y + ImGui.GetTextLineHeight() * 0.5f;
        var alpha = 0.4f + 0.6f * Styling.Pulse(Styling.PulseBreath);
        dl.AddCircleFilled(new Vector2(position.X + dotRadius, midY), dotRadius, Paint.Col(Styling.WithAlpha(accent, alpha)));

        var textX = position.X + dotRadius * 2f + 8f * scale;
        var text = string.IsNullOrWhiteSpace(status) ? WorkingLabel : status;
        TextDraw.At(TextDraw.Truncate(text, width - (textX - position.X)), new Vector2(textX, position.Y), Styling.TextSecondary);
    }

    private static void DrawStepper(ImDrawListPtr dl, TribeRunProgress progress, float x, float y, float width)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var rank = PhaseRank(progress.Phase);
        var current = progress.Current;
        var gap = StepGap * scale;
        var segmentWidth = (width - gap * (Steps.Length - 1)) / Steps.Length;
        var barHeight = StepBarHeight * scale;
        var labelY = y + barHeight + StepLabelGap * scale;

        using var caption = Fonts.PushCaption();
        for (var index = 0; index < Steps.Length; index++)
        {
            var step = Steps[index];
            var stepRank = PhaseRank(step.Phase);
            var state = stepRank < rank ? StepState.Done : stepRank == rank ? StepState.Active : StepState.Pending;
            var segmentX = x + index * (segmentWidth + gap);

            var bar = state switch
            {
                StepState.Done   => Styling.AccentMint,
                StepState.Active => Styling.WithAlpha(Styling.AccentTeal, 0.45f + 0.55f * Styling.Pulse(Styling.PulseMedium)),
                _                => Styling.WithAlpha(Styling.BorderDim, 0.6f),
            };
            Paint.Fill(dl, new Vector2(segmentX, y), new Vector2(segmentX + segmentWidth, y + barHeight), bar, barHeight * 0.5f);

            var text = state switch
            {
                StepState.Done   => Styling.TextDim,
                StepState.Active => Styling.TextStrong,
                _                => Styling.TextMuted,
            };
            TextDraw.At(step.Label, new Vector2(segmentX, labelY), text);

            var suffix = StepSuffix(step.Phase, current);
            if (suffix is null) continue;
            TextDraw.Right(suffix, segmentX + segmentWidth, labelY, state == StepState.Pending ? Styling.TextMuted : Styling.TextSecondary);
        }
    }

    private static string? StepSuffix(TribePhase phase, TribeInfo? current)
    {
        if (current is null) return null;
        return phase switch
        {
            TribePhase.Accepting  => $"{Math.Min(current.AcceptedTodayCount, AdtConstants.MaxAcceptsPerTribe)}/{AdtConstants.MaxAcceptsPerTribe}",
            TribePhase.Delegating => current.InProgressQuestIds.Length > 0 ? $"{current.InProgressQuestIds.Length} left" : null,
            _                     => null,
        };
    }

    private static int PhaseRank(TribePhase phase) => phase switch
    {
        TribePhase.SwitchingJob => 1,
        TribePhase.Traveling    => 2,
        TribePhase.Accepting    => 3,
        TribePhase.Delegating   => 4,
        TribePhase.Done         => 5,
        _                       => 0,
    };

    private static void DrawStatTiles(TribeRunProgress progress)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var avail = ImGui.GetContentRegionAvail().X;
        var gap = 8f * scale;
        var tileWidth = (avail - gap * 3f) / 4f;

        var left = Math.Clamp(TribeStateReader.GlobalAllowanceLeft(), 0, AdtConstants.DailyAllowanceCap);
        var used = AdtConstants.DailyAllowanceCap - left;
        var current = progress.Current;
        var accepted = current is null ? 0 : Math.Min(current.AcceptedTodayCount, AdtConstants.MaxAcceptsPerTribe);
        var journal = current?.InProgressQuestIds.Length ?? 0;

        StatTile.Draw(TileAllowances, $"{used} / {AdtConstants.DailyAllowanceCap}", $"{left} left", Styling.AccentTeal, tileWidth);
        ImGui.SameLine(0, gap);
        StatTile.Draw(TileCurrent, $"{accepted} / {AdtConstants.MaxAcceptsPerTribe}", journal > 0 ? $"{journal} in journal" : null, Styling.AccentAmber, tileWidth);
        ImGui.SameLine(0, gap);
        StatTile.Draw(TileElapsed, Formatting.Clock(progress.ElapsedMs), null, Styling.AccentMint, tileWidth);
        ImGui.SameLine(0, gap);
        StatTile.Draw(TileReset, Formatting.ResetCountdown(), null, Styling.AccentViolet, tileWidth);
    }

    private static void DrawQueue(TribeRunProgress progress)
    {
        var runList = progress.RunList;
        var first = progress.Completed + 1;
        if (first >= runList.Count) return;

        SectionTitle(UpNextTitle);
        for (var index = first; index < runList.Count; index++)
        {
            var tribe = runList[index];
            TribeStateReader.Refresh(tribe);
            DrawQueueRow(tribe, index == first);
            Styling.VSpace(2f);
        }
    }

    private static void DrawQueueRow(TribeInfo tribe, bool emphasize)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var size = new Vector2(ImGui.GetContentRegionAvail().X, Layout.QueueRowHeight * scale);
        var origin = ImGui.GetCursorScreenPos();
        var end = origin + size;
        var dl = ImGui.GetWindowDrawList();
        var midY = origin.Y + size.Y * 0.5f;

        Paint.Glass(dl, origin, end, Styling.CardRounding * scale, Styling.AccentTeal, emphasize ? 0.10f : 0.03f);

        var padX = 13f * scale;
        var icon = 28f * scale;
        TribeIcon.Draw(dl, tribe, new Vector2(origin.X + padX, midY - icon * 0.5f), icon);

        var meta = $"{tribe.AcceptSlotsRemaining} dailies · {RankBadge.RankName(tribe)}";
        float metaWidth;
        using (Fonts.PushCaption())
        {
            var metaSize = TextDraw.Measure(meta);
            metaWidth = metaSize.X;
            TextDraw.At(meta, new Vector2(end.X - padX - metaWidth, midY - metaSize.Y * 0.5f), Styling.TextDim);
        }

        var nameX = origin.X + padX + icon + 12f * scale;
        var name = TextDraw.Truncate(tribe.Name, end.X - padX - metaWidth - 12f * scale - nameX);
        var nameSize = TextDraw.Measure(name);
        TextDraw.At(name, new Vector2(nameX, midY - nameSize.Y * 0.5f), emphasize ? Styling.TextStrong : Styling.TextSecondary);

        ImGui.Dummy(size);
    }

    private static void DrawLog(TribeRunProgress progress)
    {
        SectionTitle(ActivityTitle);

        var scale = ImGuiHelpers.GlobalScale;
        var rowHeight = Layout.LogRowHeight * scale;
        var rows = Math.Min(progress.Log.Count, LogRows);
        var height = rows * rowHeight + 4f * scale;

        using var child = ImRaii.Child("##adt_run_log", new Vector2(-1f, height), false, ImGuiWindowFlags.NoBackground);
        if (!child) return;

        var log = progress.Log;
        for (var index = 0; index < log.Count; index++) DrawLogRow(log[index], rowHeight);
        if (log.Count > rows) ImGui.SetScrollHereY(1f);
    }

    private static void DrawLogRow(RunLogEntry entry, float rowHeight)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var origin = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var midY = origin.Y + rowHeight * 0.5f;
        var dl = ImGui.GetWindowDrawList();

        var color = entry.Outcome switch
        {
            RunOutcome.Completed => Styling.AccentMint,
            RunOutcome.Partial   => Styling.AccentAmber,
            RunOutcome.Skipped   => Styling.AccentAmber,
            _                    => Styling.AccentRose,
        };

        var radius = 4.5f * scale;
        var center = new Vector2(origin.X + radius + 4f * scale, midY);
        if (entry.Outcome == RunOutcome.Completed)
        {
            dl.AddCircleFilled(center, radius, Paint.Col(color));
            Paint.Check(dl, center, radius * 1.1f, Styling.WindowBg with { W = 1f }, 1.6f * scale);
        }
        else
        {
            dl.AddCircle(center, radius, Paint.Col(color), 0, 1.6f * scale);
        }

        var textX = center.X + radius + 10f * scale;
        var nameSize = TextDraw.Measure(entry.Name);
        TextDraw.At(entry.Name, new Vector2(textX, midY - nameSize.Y * 0.5f), Styling.TextSecondary);

        using (Fonts.PushCaption())
        {
            var detailX = textX + nameSize.X + 8f * scale;
            var detail = TextDraw.Truncate(entry.Detail, origin.X + width - detailX);
            var detailSize = TextDraw.Measure(detail);
            TextDraw.At(detail, new Vector2(detailX, midY - detailSize.Y * 0.5f), color);
        }

        ImGui.Dummy(new Vector2(width, rowHeight));
    }

    private static void SectionTitle(string text)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var origin = ImGui.GetCursorScreenPos();
        var size = TextDraw.SectionTitleSize(text);
        TextDraw.SectionTitle(text, origin, Styling.TextStrong);
        ImGui.Dummy(new Vector2(ImGui.GetContentRegionAvail().X, size.Y + 8f * scale));
    }
}
