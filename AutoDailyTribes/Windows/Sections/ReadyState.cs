using AutoDailyTribes.Core.Localization;
using AutoDailyTribes.Core.Tasks;
using Dalamud.Bindings.ImGui;
using System.Numerics;

namespace AutoDailyTribes.Windows.Sections;

internal static class ReadyState
{
    public enum Kind { SetupNeeded, PickTribes, AllDone, Ready, Running }

    public readonly record struct Info(Kind Kind, Vector4 Accent, Vector4 AccentSoft, string Title, string Detail);

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
            return new Info(Kind.Running, accent, accentSoft, Loc.T(L.Tribes.TitleRunning), PhaseLabel(phase));
        }

        var plan = RunPlan.Resolve(cfg);
        if (!plan.DependenciesReady)
        {
            return new Info(Kind.SetupNeeded, Styling.AccentRose, Styling.AccentRoseSoft, Loc.T(L.Tribes.TitleSetup), Loc.T(L.Tribes.DetailSetup));
        }

        var countdown = Formatting.ResetCountdown();
        if (plan.Exhausted && plan.Runnable.Count == 0)
        {
            return new Info(Kind.AllDone, Styling.AccentMint, Styling.AccentMintSoft,
                Loc.T(L.Tribes.TitleExhausted), Loc.T(L.Tribes.DetailExhausted, countdown));
        }

        if (plan.SelectedCount == 0)
        {
            return new Info(Kind.PickTribes, Styling.AccentAmber, Styling.AccentAmberSoft, Loc.T(L.Tribes.TitlePick), Loc.T(L.Tribes.DetailPick));
        }

        if (plan.Runnable.Count == 0)
        {
            return new Info(Kind.AllDone, Styling.AccentMint, Styling.AccentMintSoft,
                Loc.T(L.Tribes.TitleDone), Loc.T(L.Tribes.DetailDone, countdown));
        }

        var title = Loc.T(L.Tribes.TitleReady, Formatting.Tribes(plan.Runnable.Count));
        var skipped = plan.SelectedCount - plan.Runnable.Count;
        var detail = skipped > 0
            ? Loc.T(L.Tribes.DetailReadySkipped, skipped, plan.SelectedCount)
            : Loc.T(L.Tribes.DetailReady, plan.AllowancesNeeded, plan.AllowanceLeft);
        return new Info(Kind.Ready, Styling.AccentMint, Styling.AccentMintSoft, title, detail);
    }

    public static string ShortLabel(Kind kind) => Loc.T(kind switch
    {
        Kind.Running     => L.Shell.StatusRunning,
        Kind.Ready       => L.Shell.StatusReady,
        Kind.PickTribes  => L.Shell.StatusPickTribes,
        Kind.SetupNeeded => L.Shell.StatusSetupNeeded,
        _                => L.Shell.StatusAllDone,
    });

    public static string PhaseLabel(TribePhase phase) => Loc.T(phase switch
    {
        TribePhase.SwitchingJob => L.Run.PhaseSwitchingJob,
        TribePhase.Traveling    => L.Run.PhaseTraveling,
        TribePhase.Accepting    => L.Run.PhaseAccepting,
        TribePhase.Delegating   => L.Run.PhaseDelegating,
        TribePhase.Recovering   => L.Run.PhaseRecovering,
        TribePhase.Done         => L.Run.PhaseDone,
        TribePhase.Preparing    => L.Run.PhasePreparing,
        _                       => L.Run.PhaseStandingBy,
    });

    public static (Vector4 Accent, Vector4 AccentSoft) PhasePalette(TribePhase phase) => phase switch
    {
        TribePhase.Recovering => (Styling.AccentRose, Styling.AccentRoseSoft),
        TribePhase.Done       => (Styling.AccentMint, Styling.AccentMintSoft),
        _                     => (Styling.AccentTeal, Styling.AccentTealSoft),
    };
}
