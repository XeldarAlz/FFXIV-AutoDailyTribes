using AutoDailyTribes.Core.Game;
using AutoDailyTribes.Core.Ipc;
using AutoDailyTribes.Core.Tribes;
using ECommons.DalamudServices;
using System.Threading.Tasks;

namespace AutoDailyTribes.Core.Tasks;

// Each iteration computes the desired phase, then dispatches a bounded handler. Every movement op runs
// through a cancellable MoveOp with a wall-clock watchdog and stuck detection, so no single step can
// park the run; a transient fault is tolerated and retried, and an unrecoverable one ends THIS tribe
// cleanly (the controller moves on to the next). Implementation is split across partial files:
//   AutoTribe.Movement.cs — travel, stuck recovery, teleports, dismount.
//   AutoTribe.Quests.cs    — job switching, accepting dailies, Questionable delegation.
//   AutoTribe.Recovery.cs  — death/KO handling.
public sealed partial class AutoTribe(TribeInfo tribe) : AutoCommon
{
    private readonly QuestionableIPC _questionable = new();

    public TribeInfo Tribe => tribe;

    private const int   HeartbeatMs = 30_000;
    private const int   StallWarningMs = 180_000;
    private const int   MaxConsecutiveStateErrors = 20;
    private const int   MoveToIssuerWatchdogMs = 60_000;
    // Slack on top of the in-move deadline so clib's own graceful exit wins over the hard cancel while it
    // is following a path; the hard cancel only catches a wedge in a non-polling phase.
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
        }
        catch (Exception ex)
        {
            var msg = ex.Message;
            var lastBracket = msg.LastIndexOf("] ");
            if (lastBracket >= 0) msg = msg[(lastBracket + 2)..];
            Svc.Log.Error($"[ADT] {tribe.Name} stopped: {msg}");
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
                        Status = "Done";
                        return;

                    case TribeState.Unconscious:
                        await Revive();
                        break;

                    case TribeState.SwitchingJob:
                        if (!await EnsureCorrectJob()) return; // EnsureCorrectJob warns; skip this tribe
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

                // Hard safety net: guarantee the loop yields the framework thread at least once per
                // iteration, even if a handler returned synchronously.
                await NextFrame();
                consecutiveErrors = 0;
            }
            catch (Exception ex) when (!CancelToken.IsCancellationRequested)
            {
                // One transient fault (e.g. a despawned-object NRE) must not end the tribe; back off and
                // retry. Only a long unbroken run of failures surfaces and stops (the controller then
                // moves on to the next tribe).
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
        ErrorIf(!_questionable.IsAvailable, "Questionable plugin not installed/enabled");
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

        // Switch to a suitable job before anything else (needed both to accept and for Questionable to
        // run the quest). Done once per tribe; a hard failure skips the tribe inside the handler.
        if (!jobResolved && !JobSwitcher.CurrentJobSatisfies(tribe.Kind)) return TribeState.SwitchingJob;

        if (needAccept)
        {
            if (Svc.ClientState.TerritoryType != tribe.IssuerTerritoryId) return TribeState.WrongZone;
            return arrivedAtIssuer ? TribeState.AcceptDailies : TribeState.TravelToIssuer;
        }

        // Nothing left to accept, but quests are in the journal — hand them to Questionable (which does
        // its own travel/class-switching for the quest).
        return TribeState.Delegate;
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
