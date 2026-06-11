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

    public static void Draw(AutoTribeController controller)
    {
        var progress = controller.Progress;
        var (accent, accentSoft, label) = PhaseInfo(progress.Phase);

        Styling.VSpace(6);
        DrawHeroRing(progress, accent, accentSoft);
        Styling.VSpace(8);

        Styling.TextCentered(label, Styling.PulseColor(accent, accentSoft, Styling.PulseMedium), 0.95f);
        Styling.VSpace(4);
        DrawCurrentTribe(progress.Current);
        Styling.VSpace(2);
        DrawLiveLine(controller.Status, accentSoft);
        Styling.VSpace(2);
        Styling.TextCentered($"Elapsed {Clock(progress.ElapsedMs)}", Styling.TextDim, 0.9f);

        if (progress.Current is not null)
        {
            Styling.VSpace(10);
            DrawSteps(progress);
        }

        Styling.VSpace(12);
        DrawStopButton(controller);

        var upNext = progress.UpNext.ToArray();
        if (upNext.Length > 0)
        {
            Styling.VSpace(12);
            Styling.SectionLabel("Up Next");
            Styling.VSpace(2);
            foreach (var tribe in upNext)
            {
                UpNextRow.Draw(tribe);
                ImGui.Spacing();
            }
        }

        if (progress.Log.Count > 0)
        {
            Styling.VSpace(12);
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

    private static void DrawHeroRing(TribeRunProgress progress, Vector4 accent, Vector4 accentSoft)
    {
        var s = ImGuiHelpers.GlobalScale;
        var ringR = Layout.HeroRingRadius * s;
        var thickness = 5f * s;

        var start = ImGui.GetCursorScreenPos();
        var availX = ImGui.GetContentRegionAvail().X;
        var center = new Vector2(start.X + availX * 0.5f, start.Y + ringR);

        ProgressRing.Glow(center, ringR, accent, 0.45f + 0.40f * Styling.Pulse(Styling.PulseBreath));
        ProgressRing.Track(center, ringR, thickness, Styling.WithAlpha(Styling.BorderDim, 0.7f));
        ProgressRing.Fill(center, ringR, thickness, SmoothFraction(progress), accent);
        ProgressRing.Sweep(center, ringR, thickness * 0.72f, accentSoft, Styling.PulseOrbit, MathF.PI * 0.5f, 1f);

        var total = Math.Max(progress.Total, 1);
        ProgressRing.CenterValue(center, $"{progress.Completed} / {total}", "tribes",
            Styling.TextStrong, Styling.TextDim, 1.7f);

        ImGui.SetCursorScreenPos(start);
        ImGui.Dummy(new Vector2(availX, ringR * 2f));
    }

    private static void DrawCurrentTribe(TribeInfo? tribe)
    {
        if (tribe is null) return;

        var s = ImGuiHelpers.GlobalScale;
        var iconSize = ImGui.GetTextLineHeight();
        var gap = 8f * s;
        var nameW = ImGui.CalcTextSize(tribe.Name).X;

        Styling.CenterNextItem(iconSize + gap + nameW);
        TribeIcon.Draw(tribe, iconSize);
        ImGui.SameLine(0, gap);
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextStrong))
            ImGui.TextUnformatted(tribe.Name);

        Styling.TextCentered(RankBadge.RankLabel(tribe), Styling.TextDim);
    }

    private static void DrawLiveLine(string text, Vector4 accent)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        var s = ImGuiHelpers.GlobalScale;
        var dotR = 3.5f * s;
        var gap = 8f * s;
        var ts = ImGui.CalcTextSize(text);

        Styling.CenterNextItem(dotR * 2f + gap + ts.X);
        var origin = ImGui.GetCursorScreenPos();
        var midY = origin.Y + ImGui.GetTextLineHeight() * 0.5f;

        var alpha = 0.4f + 0.6f * Styling.Pulse(Styling.PulseBreath);
        ImGui.GetWindowDrawList().AddCircleFilled(new Vector2(origin.X + dotR, midY), dotR,
            ImGui.GetColorU32(Styling.WithAlpha(accent, alpha)));

        ImGui.SetCursorScreenPos(new Vector2(origin.X + dotR * 2f + gap, origin.Y));
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextSecondary))
            ImGui.TextUnformatted(text);
    }

    private static void DrawSteps(TribeRunProgress progress)
    {
        var s = ImGuiHelpers.GlobalScale;
        var rank = PhaseRank(progress.Phase);
        var cur = progress.Current;

        var rows = new (string label, int rank, string suffix)[]
        {
            ("Switch job", 1, ""),
            ("Travel",     2, ""),
            ("Accept",     3, cur is null ? "" : $"{Math.Min(cur.AcceptedTodayCount, AdtConstants.MaxAcceptsPerTribe)}/{AdtConstants.MaxAcceptsPerTribe}"),
            ("Delegate",   4, cur is { InProgressQuestIds.Length: > 0 } ? $"{cur.InProgressQuestIds.Length} left" : ""),
        };

        var blockW = MathF.Min(ImGui.GetContentRegionAvail().X, 320f * s);
        Styling.CenterNextItem(blockW);
        using var child = ImRaii.Child("##steps", new Vector2(blockW, rows.Length * Layout.StepRowHeight * s),
            false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
        if (!child) return;

        foreach (var (lbl, r, suffix) in rows)
        {
            var state = r < rank ? StepState.Done : r == rank ? StepState.Active : StepState.Pending;
            DrawStepRow(lbl, suffix, state);
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

    private static void DrawStepRow(string label, string suffix, StepState state)
    {
        var s = ImGuiHelpers.GlobalScale;
        var rowH = Layout.StepRowHeight * s;
        var origin = ImGui.GetCursorScreenPos();
        var midY = origin.Y + rowH * 0.5f;
        var mR = 5.5f * s;
        var dl = ImGui.GetWindowDrawList();
        var center = new Vector2(origin.X + mR + 2f * s, midY);

        var (marker, text) = state switch
        {
            StepState.Done   => (Styling.AccentMint, Styling.TextDim),
            StepState.Active => (Styling.AccentTeal, Styling.TextStrong),
            _                => (Styling.WithAlpha(Styling.TextMuted, 0.7f), Styling.TextMuted),
        };

        switch (state)
        {
            case StepState.Done:
                dl.AddCircleFilled(center, mR, ImGui.GetColorU32(marker));
                var chk = ImGui.GetColorU32(new Vector4(0.05f, 0.07f, 0.08f, 1f));
                dl.AddLine(center + new Vector2(-0.34f, 0.02f) * mR, center + new Vector2(-0.08f, 0.30f) * mR, chk, 1.8f * s);
                dl.AddLine(center + new Vector2(-0.08f, 0.30f) * mR, center + new Vector2(0.40f, -0.30f) * mR, chk, 1.8f * s);
                break;
            case StepState.Active:
                var pulse = 0.55f + 0.45f * Styling.Pulse(Styling.PulseMedium);
                dl.AddCircleFilled(center, mR, ImGui.GetColorU32(Styling.WithAlpha(marker, pulse)));
                break;
            default:
                dl.AddCircle(center, mR, ImGui.GetColorU32(marker), 0, 1.6f * s);
                break;
        }

        var lineH = ImGui.GetTextLineHeight();
        ImGui.SetCursorScreenPos(new Vector2(center.X + mR + 9f * s, midY - lineH * 0.5f));
        using (ImRaii.PushColor(ImGuiCol.Text, text))
            ImGui.TextUnformatted(label);

        if (suffix.Length > 0)
        {
            var sw = ImGui.CalcTextSize(suffix).X;
            ImGui.SetCursorScreenPos(new Vector2(origin.X + ImGui.GetContentRegionAvail().X - sw, midY - lineH * 0.5f));
            using (ImRaii.PushColor(ImGuiCol.Text, state == StepState.Pending ? Styling.TextMuted : Styling.TextSecondary))
                ImGui.TextUnformatted(suffix);
        }

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(new Vector2(ImGui.GetContentRegionAvail().X, rowH));
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

    private static void DrawStopButton(AutoTribeController controller)
    {
        var height = Layout.PrimaryButtonHeight * ImGuiHelpers.GlobalScale;
        using (ImRaii.PushColor(ImGuiCol.Button, Styling.AccentRose * 0.55f)
            .Push(ImGuiCol.ButtonHovered, Styling.AccentRose * 0.78f)
            .Push(ImGuiCol.ButtonActive, Styling.AccentRose)
            .Push(ImGuiCol.Text, Styling.TextStrong))
            if (ImGui.Button("STOP", new Vector2(-1, height)))
                controller.Stop();
    }
}
