using AutoDailyTribes.Core.Game;
using AutoDailyTribes.Core.Ipc;
using AutoDailyTribes.Core.Tribes;
using ECommons.DalamudServices;
using System.Threading.Tasks;

namespace AutoDailyTribes.Core.Tasks;

public sealed partial class AutoTribe(TribeInfo tribe, TribeRunProgress? progress = null) : AutoCommon
{
    private readonly QuestionableIPC questionable = new();

    private const int   HeartbeatMs = 30_000;
    private const int   StallWarningMs = 180_000;
    private const int   MaxConsecutiveStateErrors = 20;
    private const int   MaxAcceptFailPasses = 2;
    private const int   MoveToIssuerWatchdogMs = 60_000;
    private const int   MoveOpUnwindSlackMs = 10_000;
    private const int   TeleportWatchdogMs = 60_000;
    private const int   DismountWatchdogMs = 30_000;
    private const int   NavmeshReadyWaitMs = 60_000;
    private const int   MoveProgressLogMs = 15_000;
    private const int   CombatClearTimeoutMs = 15_000;
    private const float IssuerArrivalMeters = 6f;
    private const float TeleportRetryProgressMeters = 3.0f;
    private const int   MaxTravelStuckRetries = 2;

    private enum TribeState { Idle, Done, Unconscious, SwitchingJob, WrongZone, TravelToIssuer, AcceptDailies, Delegate }
    private enum ExitReason { Continue, Quit }
    private enum IssuerMoveResult { Arrived, StuckRetry, StuckInCombat }

    private bool jobResolved;
    private bool arrivedAtIssuer;
    private bool delegated;
    private int  acceptFailPasses;
    private int  consecutiveStuckRetries;

    private RunOutcome runOutcome = RunOutcome.Completed;
    private string     runDetail  = "done";

    private TribeState lastObservedState = TribeState.Idle;
    private long lastStateChangedAtMs;
    private long lastHeartbeatAtMs;

    protected override async Task Execute()
    {
        Svc.Log.Info($"[ADT] Starting {tribe.Name}");
        try
        {
            await RunSupervised();
            Svc.Log.Info($"[ADT] {tribe.Name}: done.");
            progress?.LogOutcome(tribe, runOutcome, runDetail);
        }
        catch (Exception ex)
        {
            var msg = ex.Message;
            var lastBracket = msg.LastIndexOf("] ");
            if (lastBracket >= 0) msg = msg[(lastBracket + 2)..];
            Svc.Log.Error($"[ADT] {tribe.Name} stopped: {msg}");
            if (!CancelToken.IsCancellationRequested)
                progress?.LogOutcome(tribe, RunOutcome.Stopped, msg);
            throw;
        }
    }

    private async Task RunSupervised()
    {
        TribeStateReader.Refresh(tribe);
        IssuerResolver.Resolve(tribe);
        Validate();

        lastStateChangedAtMs = Environment.TickCount64;
        var consecutiveErrors = 0;
        Report(TribeState.Idle);

        while (!CancelToken.IsCancellationRequested)
        {
            try
            {
                var state = ComputeState();

                if (state != lastObservedState)
                {
                    Diag($"{tribe.Name}: {lastObservedState} -> {state}");
                    lastObservedState = state;
                    lastStateChangedAtMs = Environment.TickCount64;
                    Report(state);
                }

                if (Environment.TickCount64 - lastHeartbeatAtMs >= HeartbeatMs)
                {
                    lastHeartbeatAtMs = Environment.TickCount64;
                    LogHeartbeat(state);
                }

                var exit = ExitReason.Continue;
                switch (state)
                {
                    case TribeState.Done:
                        return;

                    case TribeState.Unconscious:
                        await Revive();
                        break;

                    case TribeState.SwitchingJob:
                        if (!await EnsureCorrectJob()) return;
                        jobResolved = true;
                        break;

                    case TribeState.WrongZone:
                        await GoToIssuerTerritory();
                        break;

                    case TribeState.TravelToIssuer:
                        exit = await TravelToIssuerWithRecovery();
                        break;

                    case TribeState.AcceptDailies:
                        exit = await DoAcceptPass();
                        break;

                    case TribeState.Delegate:
                        await DelegateToQuestionable(tribe.InProgressQuestIds);
                        delegated = true;
                        break;

                    case TribeState.Idle:
                    default:
                        await NextFrame(30);
                        break;
                }

                if (exit == ExitReason.Quit) return;

                await NextFrame();
                consecutiveErrors = 0;
            }
            catch (Exception ex) when (!CancelToken.IsCancellationRequested)
            {
                consecutiveErrors++;
                Diag($"{tribe.Name}: state machine caught {ex.GetType().Name} (#{consecutiveErrors}/{MaxConsecutiveStateErrors}): {ex.Message}");
                if (consecutiveErrors >= MaxConsecutiveStateErrors)
                {
                    Diag($"{tribe.Name}: too many consecutive state-machine errors; surfacing and stopping.");
                    throw;
                }
                await NextFrame(30);
            }
        }
    }

    private void Validate()
    {
        ErrorIf(!tribe.Unlocked, $"{tribe.Name}: not unlocked — complete the intro quest in-game first");
        ErrorIf(!tribe.MeetsRankRequirement, $"{tribe.Name}: need rank {tribe.MinRankForDailies} (have {tribe.Rank})");
        ErrorIf(tribe.IssuerInstanceId == 0, $"{tribe.Name}: BaseId placeholder — run /adt target next to the issuer to capture the real one");
        ErrorIf(!questionable.IsAvailable, "Questionable plugin not installed/enabled");
        ErrorIf(!NavmeshIPC.Instance.IsAvailable, "vnavmesh plugin not installed/enabled");
    }

    private TribeState ComputeState()
    {
        var player = Svc.Objects.LocalPlayer;
        if (player is null) return TribeState.Idle;
        if (IsPlayerKO()) return TribeState.Unconscious;

        TribeStateReader.Refresh(tribe);

        if (delegated) return TribeState.Done;

        var remainingToAccept = Math.Min(tribe.AcceptSlotsRemaining, tribe.DailyAllowanceLeft);
        var needAccept = remainingToAccept > 0;

        if (!needAccept && !tribe.HasInProgressQuests) return TribeState.Done;

        if (!jobResolved && !JobSwitcher.CurrentJobSatisfies(tribe.Kind)) return TribeState.SwitchingJob;

        if (needAccept)
        {
            if (Svc.ClientState.TerritoryType != tribe.IssuerTerritoryId) return TribeState.WrongZone;
            return arrivedAtIssuer ? TribeState.AcceptDailies : TribeState.TravelToIssuer;
        }

        return TribeState.Delegate;
    }

    // Surfaces the current state to the UI: Status drives the live activity line, progress.Phase
    // drives the coloured phase label / hero-ring accent. Called on every state transition.
    private void Report(TribeState state)
    {
        Status = state switch
        {
            TribeState.SwitchingJob   => $"Switching to a {tribe.Kind} job",
            TribeState.WrongZone      => $"Teleporting to {tribe.Name}'s home zone",
            TribeState.TravelToIssuer => "Running to the quest issuer",
            TribeState.AcceptDailies  => "Accepting daily quests",
            TribeState.Delegate       => "Handing quests to Questionable",
            TribeState.Unconscious    => "Recovering — reviving",
            TribeState.Done           => "Done",
            _                         => "Preparing",
        };

        if (progress is null) return;
        progress.Phase = state switch
        {
            TribeState.SwitchingJob                   => TribePhase.SwitchingJob,
            TribeState.WrongZone or TribeState.TravelToIssuer => TribePhase.Traveling,
            TribeState.AcceptDailies                  => TribePhase.Accepting,
            TribeState.Delegate                       => TribePhase.Delegating,
            TribeState.Unconscious                    => TribePhase.Recovering,
            TribeState.Done                           => TribePhase.Done,
            _                                         => TribePhase.Preparing,
        };
    }

    private void LogHeartbeat(TribeState state)
    {
        var pos = Svc.Objects.LocalPlayer?.Position;
        var posStr = pos is { } p ? $"({p.X:F0},{p.Y:F0},{p.Z:F0})" : "?";
        var inState = (Environment.TickCount64 - lastStateChangedAtMs) / 1000;
        var nav = NavmeshIPC.Instance;
        Diag($"HEARTBEAT {tribe.Name} state={state} ({inState}s) terr={Svc.ClientState.TerritoryType} issuerTerr={tribe.IssuerTerritoryId} " +
             $"pos={posStr} nav=run={nav.IsRunning()},busy={nav.IsBusy()} rank={tribe.Rank} allowance={tribe.DailyAllowanceLeft} " +
             $"accepted={tribe.AcceptedTodayCount} inProgress={tribe.InProgressQuestIds.Length}");

        if (inState >= StallWarningMs / 1000)
            Diag($"STALL WARNING: {tribe.Name} state {state} held {inState}s — see prior heartbeats for context.");
    }
}
