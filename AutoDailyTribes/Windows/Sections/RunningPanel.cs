using AutoDailyTribes.Core;
using AutoDailyTribes.Core.Tasks;
using AutoDailyTribes.Core.Tribes;
using AutoDailyTribes.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ECommons.Throttlers;
using System.Numerics;

namespace AutoDailyTribes.Windows.Sections;

internal static class RunningPanel
{
    private enum StepState { Pending, Active, Done }

    public static void Draw(AutoTribeController controller)
    {
        var progress = controller.Progress;
        var (accent, accentSoft, label) = PhaseInfo(progress.Phase);

        Styling.VSpace(4);
        DrawHeroCard(controller, progress, accent, accentSoft, label);

        var upNextCount = progress.Total - (progress.Completed + 1);
        if (upNextCount > 0)
        {
            Styling.VSpace(10);
            DrawQueue(progress, upNextCount);
        }

        if (progress.Log.Count > 0)
        {
            Styling.VSpace(10);
            Styling.SectionLabel("Activity");
            Styling.VSpace(2);
            DrawLog(progress);
        }
    }

    private static (Vector4 accent, Vector4 accentSoft, string label) PhaseInfo(TribePhase phase) => phase switch
    {
        TribePhase.SwitchingJob => (Styling.AccentTeal, Styling.AccentTealSoft, "SWITCHING JOB"),
        TribePhase.Traveling    => (Styling.AccentTeal, Styling.AccentTealSoft, "TRAVELING"),
        TribePhase.Accepting    => (Styling.AccentTeal, Styling.AccentTealSoft, "ACCEPTING DAILIES"),
        TribePhase.Delegating   => (Styling.AccentTeal, Styling.AccentTealSoft, "DELEGATING QUESTS"),
        TribePhase.Recovering   => (Styling.AccentRose, Styling.AccentRose,     "RECOVERING"),
        TribePhase.Done         => (Styling.AccentMint, Styling.AccentMintSoft, "TRIBE COMPLETE"),
        _                       => (Styling.AccentTeal, Styling.AccentTealSoft, "PREPARING"),
    };

    // Continuous batch progress: completed tribes plus a fraction of the in-flight tribe, so the ring
    // keeps moving within a single tribe instead of only jumping on tribe completion.
    private static float SmoothFraction(TribeRunProgress p)
    {
        if (p.Total == 0) return 0f;
        return Math.Clamp((p.Completed + StageFraction(p.Phase, p.Current)) / p.Total, 0f, 1f);
    }

    private static float StageFraction(TribePhase phase, TribeInfo? cur) => phase switch
    {
        TribePhase.SwitchingJob => 0.12f,
        TribePhase.Traveling    => 0.35f,
        TribePhase.Accepting    => 0.45f + 0.25f * AcceptFraction(cur),
        TribePhase.Delegating   => 0.72f + 0.28f * DelegateFraction(cur),
        TribePhase.Recovering   => 0.30f,
        TribePhase.Done         => 1.00f,
        _                       => 0.05f,
    };

    private static float AcceptFraction(TribeInfo? cur)
        => cur is null ? 0f : Math.Clamp(cur.AcceptedTodayCount / (float)AdtConstants.MaxAcceptsPerTribe, 0f, 1f);

    private static float DelegateFraction(TribeInfo? cur)
    {
        if (cur is null) return 1f;
        var left = cur.InProgressQuestIds.Length;
        var total = Math.Max(Math.Max(cur.AcceptedTodayCount, left), 1);
        return Math.Clamp(1f - left / (float)total, 0f, 1f);
    }

    private static void DrawHeroCard(
        AutoTribeController controller, TribeRunProgress progress, Vector4 accent, Vector4 accentSoft, string phaseLabel)
    {
        var s = ImGuiHelpers.GlobalScale;
        using var card = Card.Begin("##runhero", new Vector2(-1, Layout.RunHeroHeight * s),
            Styling.CardBg, Styling.WithAlpha(accent, 0.35f));

        var origin = ImGui.GetCursorScreenPos();
        var avail = ImGui.GetContentRegionAvail();
        var wide = avail.X >= 430f * s;
        var leftW = wide ? 156f * s : 0f;

        if (wide)
        {
            DrawRingZone(progress, accent, accentSoft, origin, leftW, avail.Y);
            var lineX = origin.X + leftW;
            ImGui.GetWindowDrawList().AddLine(
                new Vector2(lineX, origin.Y + 6f * s), new Vector2(lineX, origin.Y + avail.Y - 6f * s),
                ImGui.GetColorU32(Styling.Hairline), 1f);
        }
        else
        {
            phaseLabel = $"{phaseLabel} · {progress.Completed}/{Math.Max(progress.Total, 1)} · {Clock(progress.ElapsedMs)}";
        }

        var zoneX = origin.X + leftW + (wide ? 16f * s : 0f);
        var zoneW = origin.X + avail.X - zoneX;
        DrawStatusZone(controller, progress, accent, accentSoft, phaseLabel, new Vector2(zoneX, origin.Y), zoneW, avail.Y);

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(avail);
    }

    private static void DrawRingZone(
        TribeRunProgress progress, Vector4 accent, Vector4 accentSoft, Vector2 origin, float leftW, float innerH)
    {
        var s = ImGuiHelpers.GlobalScale;
        var ringR = Layout.RunRingRadius * s;
        var thickness = 5f * s;
        var lineH = ImGui.GetTextLineHeight();
        var blockH = ringR * 2f + 8f * s + lineH;
        var topY = origin.Y + MathF.Max(0f, (innerH - blockH) * 0.5f);
        var center = new Vector2(origin.X + leftW * 0.5f, topY + ringR);

        ProgressRing.Glow(center, ringR, accent, 0.35f + 0.30f * Styling.Pulse(Styling.PulseBreath));
        ProgressRing.Track(center, ringR, thickness, Styling.WithAlpha(Styling.BorderDim, 0.7f));
        ProgressRing.Fill(center, ringR, thickness, SmoothFraction(progress), accent);
        ProgressRing.Sweep(center, ringR, thickness * 0.72f, accentSoft, Styling.PulseOrbit, MathF.PI * 0.5f, 1f);

        var total = Math.Max(progress.Total, 1);
        ProgressRing.CenterValue(center, $"{progress.Completed} / {total}", "tribes",
            Styling.TextStrong, Styling.TextDim, 1.45f);

        var clock = Clock(progress.ElapsedMs);
        var clockW = ImGui.CalcTextSize(clock).X;
        ImGui.SetCursorScreenPos(new Vector2(center.X - clockW * 0.5f, topY + ringR * 2f + 8f * s));
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
            ImGui.TextUnformatted(clock);
    }

    private static void DrawStatusZone(
        AutoTribeController controller, TribeRunProgress progress,
        Vector4 accent, Vector4 accentSoft, string phaseLabel, Vector2 origin, float width, float innerH)
    {
        var s = ImGuiHelpers.GlobalScale;
        var lineH = ImGui.GetTextLineHeight();

        var btnH = 24f * s;
        DrawStopButton(controller, new Vector2(origin.X + width, origin.Y), btnH);

        ImGui.SetCursorScreenPos(new Vector2(origin.X, origin.Y + (btnH - lineH) * 0.5f));
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.PulseColor(accent, accentSoft, Styling.PulseMedium)))
            ImGui.TextUnformatted(phaseLabel);

        var y = origin.Y + btnH + 12f * s;
        DrawIdentity(progress.Current, new Vector2(origin.X, y));

        y += 40f * s;
        DrawLiveLine(controller.Status, accentSoft, new Vector2(origin.X, y));

        var stepH = 3f * s + 5f * s + lineH;
        DrawStepper(progress, origin.X, origin.Y + innerH - stepH, width);
    }

    private static void DrawIdentity(TribeInfo? tribe, Vector2 pos)
    {
        var s = ImGuiHelpers.GlobalScale;
        if (tribe is null)
        {
            ImGui.SetCursorScreenPos(pos);
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
                ImGui.TextUnformatted("Waiting for next tribe…");
            return;
        }

        var iconSize = 36f * s;
        ImGui.SetCursorScreenPos(pos);
        TribeIcon.Draw(tribe, iconSize);

        var textX = pos.X + iconSize + 10f * s;
        var nameH = ImGui.GetTextLineHeight() * 1.25f;

        ImGui.SetCursorScreenPos(new Vector2(textX, pos.Y + (iconSize - nameH) * 0.5f));
        ImGui.SetWindowFontScale(1.25f);
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextStrong))
            ImGui.TextUnformatted(tribe.Name);
        ImGui.SetWindowFontScale(1f);
    }

    private static void DrawLiveLine(string text, Vector4 accent, Vector2 pos)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        var s = ImGuiHelpers.GlobalScale;
        var dotR = 3.5f * s;
        var midY = pos.Y + ImGui.GetTextLineHeight() * 0.5f;

        var alpha = 0.4f + 0.6f * Styling.Pulse(Styling.PulseBreath);
        ImGui.GetWindowDrawList().AddCircleFilled(new Vector2(pos.X + dotR, midY), dotR,
            ImGui.GetColorU32(Styling.WithAlpha(accent, alpha)));

        ImGui.SetCursorScreenPos(new Vector2(pos.X + dotR * 2f + 8f * s, pos.Y));
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextSecondary))
            ImGui.TextUnformatted(text);
    }

    private static readonly (string label, int rank)[] StepperSteps =
    {
        ("Switch job", 1),
        ("Travel",     2),
        ("Accept",     3),
        ("Delegate",   4),
    };

    private static string StepSuffix(int rank, TribeInfo? cur) => rank switch
    {
        3 => cur is null ? "" : $"{Math.Min(cur.AcceptedTodayCount, AdtConstants.MaxAcceptsPerTribe)}/{AdtConstants.MaxAcceptsPerTribe}",
        4 => cur is { InProgressQuestIds.Length: > 0 } ? $"{cur.InProgressQuestIds.Length} left" : "",
        _ => "",
    };

    private static void DrawStepper(TribeRunProgress progress, float x, float y, float width)
    {
        var s = ImGuiHelpers.GlobalScale;
        var rank = PhaseRank(progress.Phase);
        var cur = progress.Current;

        var gap = 8f * s;
        var segW = (width - gap * (StepperSteps.Length - 1)) / StepperSteps.Length;
        var barH = 3f * s;
        var dl = ImGui.GetWindowDrawList();

        for (var i = 0; i < StepperSteps.Length; i++)
        {
            var (label, r) = StepperSteps[i];
            var suffix = StepSuffix(r, cur);
            var state = r < rank ? StepState.Done : r == rank ? StepState.Active : StepState.Pending;
            var segX = x + i * (segW + gap);

            var bar = state switch
            {
                StepState.Done   => Styling.AccentMint,
                StepState.Active => Styling.WithAlpha(Styling.AccentTeal, 0.45f + 0.55f * Styling.Pulse(Styling.PulseMedium)),
                _                => Styling.WithAlpha(Styling.BorderDim, 0.6f),
            };
            dl.AddRectFilled(new Vector2(segX, y), new Vector2(segX + segW, y + barH),
                ImGui.GetColorU32(bar), barH * 0.5f);

            var text = state switch
            {
                StepState.Done   => Styling.TextDim,
                StepState.Active => Styling.TextStrong,
                _                => Styling.TextMuted,
            };

            ImGui.SetCursorScreenPos(new Vector2(segX, y + barH + 5f * s));
            using (ImRaii.PushColor(ImGuiCol.Text, text))
                ImGui.TextUnformatted(label);

            if (suffix.Length > 0)
            {
                var suffixW = ImGui.CalcTextSize(suffix).X;
                ImGui.SetCursorScreenPos(new Vector2(segX + segW - suffixW, y + barH + 5f * s));
                using (ImRaii.PushColor(ImGuiCol.Text, state == StepState.Pending ? Styling.TextMuted : Styling.TextSecondary))
                    ImGui.TextUnformatted(suffix);
            }
        }
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

    private static void DrawStopButton(AutoTribeController controller, Vector2 topRight, float height)
    {
        var s = ImGuiHelpers.GlobalScale;
        const string label = "Stop";
        var width = ImGui.CalcTextSize(label).X + 28f * s;

        ImGui.SetCursorScreenPos(new Vector2(topRight.X - width, topRight.Y));
        using (ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 1f))
        using (ImRaii.PushColor(ImGuiCol.Button, Styling.WithAlpha(Styling.AccentRose, 0.14f))
            .Push(ImGuiCol.ButtonHovered, Styling.WithAlpha(Styling.AccentRose, 0.45f))
            .Push(ImGuiCol.ButtonActive, Styling.WithAlpha(Styling.AccentRose, 0.70f))
            .Push(ImGuiCol.Border, Styling.WithAlpha(Styling.AccentRose, 0.65f))
            .Push(ImGuiCol.Text, Styling.TextStrong))
            if (ImGui.Button(label, new Vector2(width, height)))
                controller.Stop();
    }

    private static void DrawQueue(TribeRunProgress progress, int upNextCount)
    {
        var s = ImGuiHelpers.GlobalScale;
        Styling.SectionLabel(upNextCount == 1 ? "Up next" : $"Up next · {upNextCount}");
        Styling.VSpace(2);

        var refresh = EzThrottler.Throttle(AdtConstants.ThrottleKeys.UiRefresh, AdtConstants.UiRefreshMs);
        var maxX = ImGui.GetCursorScreenPos().X + ImGui.GetContentRegionAvail().X;
        var startIndex = progress.Completed + 1;
        for (var index = startIndex; index < progress.Total; index++)
        {
            var tribe = progress.RunList[index];
            if (refresh) TribeStateReader.Refresh(tribe);
            if (index > startIndex)
            {
                ImGui.SameLine(0, 6f * s);
                if (ImGui.GetCursorScreenPos().X + QueueChip.Width(tribe) > maxX)
                    ImGui.NewLine();
            }
            QueueChip.Draw(tribe);
        }
    }

    private static void DrawLog(TribeRunProgress progress)
    {
        var s = ImGuiHelpers.GlobalScale;
        var rowH = Layout.LogRowHeight * s;
        var maxRows = Math.Min(progress.Log.Count, 5);
        var height = maxRows * rowH + 4f * s;

        using var child = ImRaii.Child("##runlog", new Vector2(-1, height), false);
        if (!child) return;

        foreach (var entry in progress.Log)
            DrawLogRow(entry, rowH);

        if (progress.Log.Count > maxRows)
            ImGui.SetScrollHereY(1f);
    }

    private static void DrawLogRow(RunLogEntry entry, float rowH)
    {
        var s = ImGuiHelpers.GlobalScale;
        var origin = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var midY = origin.Y + rowH * 0.5f;
        var dl = ImGui.GetWindowDrawList();

        var color = entry.Outcome switch
        {
            RunOutcome.Completed => Styling.AccentMint,
            RunOutcome.Partial   => Styling.AccentAmber,
            RunOutcome.Skipped   => Styling.AccentAmber,
            _                    => Styling.AccentRose,
        };

        var mR = 4f * s;
        var center = new Vector2(origin.X + mR + 4f * s, midY);
        if (entry.Outcome == RunOutcome.Completed)
        {
            dl.AddCircleFilled(center, mR, ImGui.GetColorU32(color));
            var chk = ImGui.GetColorU32(new Vector4(0.05f, 0.07f, 0.08f, 1f));
            dl.AddLine(center + new Vector2(-0.34f, 0.02f) * mR, center + new Vector2(-0.08f, 0.30f) * mR, chk, 1.6f * s);
            dl.AddLine(center + new Vector2(-0.08f, 0.30f) * mR, center + new Vector2(0.40f, -0.30f) * mR, chk, 1.6f * s);
        }
        else
        {
            dl.AddCircle(center, mR, ImGui.GetColorU32(color), 0, 1.6f * s);
        }

        var lineH = ImGui.GetTextLineHeight();
        var textX = center.X + mR + 9f * s;
        ImGui.SetCursorScreenPos(new Vector2(textX, midY - lineH * 0.5f));
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextSecondary))
            ImGui.TextUnformatted(entry.Name);

        ImGui.SameLine(0, 6f * s);
        using (ImRaii.PushColor(ImGuiCol.Text, color))
            ImGui.TextUnformatted($"· {entry.Detail}");

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(new Vector2(width, rowH));
    }

    private static string Clock(long ms)
    {
        var total = (int)(ms / 1000);
        return $"{total / 60:D2}:{total % 60:D2}";
    }
}
