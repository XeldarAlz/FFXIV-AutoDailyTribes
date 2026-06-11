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
    public static void Draw(AutoTribeController controller)
    {
        var progress = controller.Progress;
        var (accent, accentSoft, label) = PhasePalette(progress.Phase);

        Styling.VSpace(6);
        DrawHeroRing(progress, accent, accentSoft);
        Styling.VSpace(8);

        Styling.TextCentered(label, Styling.PulseColor(accent, accentSoft, Styling.PulseMedium), 0.95f);
        Styling.VSpace(4);
        DrawCurrentTribe(progress.Current);
        Styling.VSpace(2);
        DrawLiveLine(controller.Status, accentSoft);

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
    }

    private static (Vector4 accent, Vector4 accentSoft, string label) PhasePalette(TribePhase phase) => phase switch
    {
        TribePhase.SwitchingJob => (Styling.AccentViolet, Styling.AccentVioletSoft, "SWITCHING JOB"),
        TribePhase.Traveling    => (Styling.AccentBlue,   Styling.AccentBlueSoft,   "TRAVELING"),
        TribePhase.Accepting    => (Styling.AccentTeal,   Styling.AccentTealSoft,   "ACCEPTING DAILIES"),
        TribePhase.Delegating   => (Styling.AccentViolet, Styling.AccentVioletSoft, "DELEGATING QUESTS"),
        TribePhase.Recovering   => (Styling.AccentRose,   Styling.AccentRose,       "RECOVERING"),
        TribePhase.Done         => (Styling.AccentMint,   Styling.AccentMintSoft,   "TRIBE COMPLETE"),
        _                       => (Styling.AccentTeal,   Styling.AccentTealSoft,   "PREPARING"),
    };

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
        ProgressRing.Fill(center, ringR, thickness, progress.Fraction, accent);
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
        var totalW = dotR * 2f + gap + ts.X;

        Styling.CenterNextItem(totalW);
        var origin = ImGui.GetCursorScreenPos();
        var lineH = ImGui.GetTextLineHeight();
        var midY = origin.Y + lineH * 0.5f;

        var alpha = 0.4f + 0.6f * Styling.Pulse(Styling.PulseBreath);
        ImGui.GetWindowDrawList().AddCircleFilled(new Vector2(origin.X + dotR, midY), dotR,
            ImGui.GetColorU32(Styling.WithAlpha(accent, alpha)));

        ImGui.SetCursorScreenPos(new Vector2(origin.X + dotR * 2f + gap, origin.Y));
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextSecondary))
            ImGui.TextUnformatted(text);
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
