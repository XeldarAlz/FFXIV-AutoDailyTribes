using AutoDailyTribes.Core.Tasks;
using Dalamud.Bindings.ImGui;
using System.Numerics;

namespace AutoDailyTribes.Windows.Sections;

internal static class ReadyState
{
    public enum Kind { SetupNeeded, PickTribes, AllDone, Ready, Running }

    public readonly record struct Info(Kind Kind, Vector4 Accent, Vector4 AccentSoft, string Title, string Detail);

    private const string TitleRunning = "Running your dailies.";
    private const string TitleSetup = "Install the required plugins first.";
    private const string DetailSetup = "vnavmesh, Questionable and TextAdvance do the walking, questing and dialogue.";
    private const string TitleExhausted = "All allowances spent for today.";
    private const string TitlePick = "Pick the tribes to run.";
    private const string DetailPick = "Tap a card below. Your list is remembered, so tomorrow is one click.";
    private const string TitleDone = "Your tribes are done for today.";

    private static int cachedFrame = -1;
    private static Info cached;

    public static Info Resolve(Configuration cfg, AutoTribeController ctrl)
    {
        var frame = ImGui.GetFrameCount();
        if (frame == cachedFrame) return cached;

        cached = Compute(cfg, ctrl);
        cachedFrame = frame;
        return cached;
    }

    private static Info Compute(Configuration cfg, AutoTribeController ctrl)
    {
        if (ctrl.Running)
        {
            var phase = ctrl.Progress.Phase;
            var (accent, accentSoft) = PhasePalette(phase);
            return new Info(Kind.Running, accent, accentSoft, TitleRunning, PhaseLabel(phase));
        }

        var plan = RunPlan.Resolve(cfg);
        if (!plan.DependenciesReady)
        {
            return new Info(Kind.SetupNeeded, Styling.AccentRose, Styling.AccentRoseSoft, TitleSetup, DetailSetup);
        }

        var countdown = Formatting.ResetCountdown();
        if (plan.Exhausted && plan.Runnable.Count == 0)
        {
            return new Info(Kind.AllDone, Styling.AccentMint, Styling.AccentMintSoft, TitleExhausted, $"Fresh allowances arrive with the reset in {countdown}.");
        }

        if (plan.SelectedCount == 0)
        {
            return new Info(Kind.PickTribes, Styling.AccentAmber, Styling.AccentAmberSoft, TitlePick, DetailPick);
        }

        if (plan.Runnable.Count == 0)
        {
            return new Info(Kind.AllDone, Styling.AccentMint, Styling.AccentMintSoft, TitleDone, $"The list stays and runs again after the reset in {countdown}.");
        }

        var title = $"Ready to run {Formatting.Plural(plan.Runnable.Count, "tribe", "tribes")}.";
        var skipped = plan.SelectedCount - plan.Runnable.Count;
        var detail = skipped > 0
            ? $"{skipped} of your {plan.SelectedCount} picks are finished, hidden or locked and will be skipped."
            : $"Uses {plan.AllowancesNeeded} of the {plan.AllowanceLeft} allowances left today.";
        return new Info(Kind.Ready, Styling.AccentMint, Styling.AccentMintSoft, title, detail);
    }

    public static string ShortLabel(Kind kind) => kind switch
    {
        Kind.Running     => "Running",
        Kind.Ready       => "Ready",
        Kind.PickTribes  => "Pick tribes",
        Kind.SetupNeeded => "Setup needed",
        _                => "All done",
    };

    public static string PhaseLabel(TribePhase phase) => phase switch
    {
        TribePhase.SwitchingJob => "Switching job",
        TribePhase.Traveling    => "Traveling",
        TribePhase.Accepting    => "Accepting dailies",
        TribePhase.Delegating   => "Running quests",
        TribePhase.Recovering   => "Recovering",
        TribePhase.Done         => "Tribe complete",
        TribePhase.Preparing    => "Preparing",
        _                       => "Standing by",
    };

    public static (Vector4 Accent, Vector4 AccentSoft) PhasePalette(TribePhase phase) => phase switch
    {
        TribePhase.Recovering => (Styling.AccentRose, Styling.AccentRoseSoft),
        TribePhase.Done       => (Styling.AccentMint, Styling.AccentMintSoft),
        _                     => (Styling.AccentTeal, Styling.AccentTealSoft),
    };
}
