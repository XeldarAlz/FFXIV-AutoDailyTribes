using AutoDailyTribes.Core.Game;
using AutoDailyTribes.Core.Ipc;
using AutoDailyTribes.Core.Tribes;
using clib.Extensions;
using clib.TaskSystem;
using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.UI;
using System.Numerics;
using System.Threading.Tasks;

namespace AutoDailyTribes.Core.Tasks;

// Each iteration computes the desired phase, then dispatches a bounded handler. Every movement op runs
// through a cancellable MoveOp with a wall-clock watchdog and stuck detection, so no single step can
// park the run; a transient fault is tolerated and retried, and an unrecoverable one ends THIS tribe
// cleanly (the controller moves on to the next).
public sealed class AutoTribe(TribeInfo tribe) : AutoCommon
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

    private enum TribeState { Idle, Done, SwitchingJob, WrongZone, TravelToIssuer, AcceptDailies, Delegate }
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

    private async Task GoToIssuerTerritory()
    {
        arrivedAtIssuer = false;
        Status = $"Teleporting to {tribe.Name}";
        Diag($"{tribe.Name}: off-zone (in {Svc.ClientState.TerritoryType}); teleporting to {tribe.IssuerTerritoryId}");
        await TeleportToTerritory(tribe.IssuerTerritoryId, tribe.IssuerLocation, $"teleport-to-zone-{tribe.BeastTribeId}", TeleportWatchdogMs);
    }

    private async Task<ExitReason> TravelToIssuerWithRecovery()
    {
        var result = await MoveToIssuer();
        switch (result)
        {
            case IssuerMoveResult.Arrived:
                arrivedAtIssuer = true;
                consecutiveStuckRetries = 0;
                return ExitReason.Continue;

            case IssuerMoveResult.StuckInCombat:
                await ClearBlockingCombat();
                return ExitReason.Continue;

            case IssuerMoveResult.StuckRetry:
            default:
                consecutiveStuckRetries++;
                if (consecutiveStuckRetries >= MaxTravelStuckRetries)
                {
                    if (await TryTeleportNearIssuer())
                    {
                        consecutiveStuckRetries = 0;
                        return ExitReason.Continue;
                    }
                    Warning($"{tribe.Name}: cannot reach the issuer (stuck after retry + teleport recovery); skipping");
                    return ExitReason.Quit;
                }
                Diag($"{tribe.Name}: stuck en route to issuer; retrying ({consecutiveStuckRetries}/{MaxTravelStuckRetries})");
                return ExitReason.Continue;
        }
    }

    private async Task<IssuerMoveResult> MoveToIssuer()
    {
        await WaitForNavmeshReady();

        var dest = tribe.IssuerLocation;
        var config = MovementConfig.Everything.WithTolerance(3f);
        var label = $"Travelling to {tribe.Name}";
        var deadline = Environment.TickCount64 + MoveToIssuerWatchdogMs;
        var nextLogMs = Environment.TickCount64 + MoveProgressLogMs;
        var arrived = false;
        var stuckInCombat = false;

        bool StopCondition()
        {
            Status = label;
            if (Environment.TickCount64 >= deadline) return true; // backstop; arrival re-checked after the op
            var p = Svc.Objects.LocalPlayer;
            if (p is not null && Vector3.Distance(p.Position, dest) <= IssuerArrivalMeters) { arrived = true; return true; }
            return false;
        }

        var stuck = new TravelStuckTracker();
        bool AbortIfFrozen()
        {
            if (arrived) return false;

            if (Environment.TickCount64 >= nextLogMs)
            {
                nextLogMs = Environment.TickCount64 + MoveProgressLogMs;
                var pp = Svc.Objects.LocalPlayer?.Position;
                var s = pp is { } v ? $"({v.X:F0},{v.Y:F0},{v.Z:F0})" : "?";
                Diag($"{tribe.Name}: still travelling to issuer pos={s} navRun={NavmeshIPC.Instance.IsRunning()} busy={NavmeshIPC.Instance.IsBusy()} combat={Svc.Condition[ConditionFlag.InCombat]}");
            }

            var kind = stuck.Check();
            if (kind == StallKind.None) return false;

            stuckInCombat = Svc.Condition[ConditionFlag.InCombat];
            Diag(stuckInCombat
                ? $"{tribe.Name}: travel stalled in combat ({kind}); cancelling (teleport is blocked in combat)"
                : $"{tribe.Name}: travel wedged ({kind}); cancelling to retry");
            return true;
        }

        var op = new MoveOp(o => o.Move(tribe.IssuerTerritoryId, dest, config,
            allowTeleportIfFaster: true, StopCondition, allowAethernetWithinTerritory: true));

        await RunCancellable(op, MoveToIssuerWatchdogMs + MoveOpUnwindSlackMs, "move-to-issuer", AbortIfFrozen);
        if (CancelToken.IsCancellationRequested) return IssuerMoveResult.StuckRetry;

        // Re-check arrival from the live position: StopCondition may have tripped on the deadline, or a
        // flying mount routinely stops a few metres above the point (the Y gap).
        var player = Svc.Objects.LocalPlayer;
        if (player is not null && Vector3.Distance(player.Position, dest) <= IssuerArrivalMeters)
        {
            if (Svc.Condition[ConditionFlag.Mounted])
                await RunCancellable(new MoveOp(o => o.DismountNow()), DismountWatchdogMs, $"dismount-{tribe.BeastTribeId}");
            return IssuerMoveResult.Arrived;
        }

        if (stuckInCombat) return IssuerMoveResult.StuckInCombat;
        if (op.Fault is { } fault) Diag($"{tribe.Name}: move to issuer faulted: {fault.Message}; retrying");
        return IssuerMoveResult.StuckRetry;
    }

    // Tribes carry no combat automation, so we can't kill what aggroed us; dismount and wait briefly for
    // the mob to disengage, then let travel re-path (moving usually outpaces/drops the aggro). Bounded so
    // an unkillable add can't park the run. Teleport is blocked in combat, which is why this runs first.
    private async Task ClearBlockingCombat()
    {
        if (!Svc.Condition[ConditionFlag.InCombat]) return;

        Status = "Clearing combat";
        Diag($"{tribe.Name}: in combat during travel; waiting briefly before re-pathing");
        if (Svc.Condition[ConditionFlag.Mounted])
            await RunCancellable(new MoveOp(o => o.DismountNow()), DismountWatchdogMs, "dismount-combat");

        var deadline = Environment.TickCount64 + CombatClearTimeoutMs;
        while (Environment.TickCount64 < deadline)
        {
            if (CancelToken.IsCancellationRequested) return;
            if (!Svc.Condition[ConditionFlag.InCombat]) break;
            await NextFrame(30);
        }

        if (Svc.Condition[ConditionFlag.InCombat])
            Diag($"{tribe.Name}: still in combat after {CombatClearTimeoutMs / 1000}s; will retry travel");
    }

    private async Task<bool> TryTeleportNearIssuer()
    {
        var before = Svc.Objects.LocalPlayer?.Position;
        Status = $"Teleporting closer to {tribe.Name}";
        Diag($"{tribe.Name}: teleport recovery toward issuer at {tribe.IssuerLocation}");

        // Same-zone teleport to the aetheryte nearest the issuer. Idle-stall guard catches a teleport that
        // never starts casting in ~8s instead of waiting out the full watchdog.
        var tp = new MoveOp(o => o.Teleport(tribe.IssuerTerritoryId, tribe.IssuerLocation, allowSameZoneTeleport: true));
        if (!await RunCancellable(tp, TeleportWatchdogMs, $"teleport-recovery-{tribe.BeastTribeId}", IdleStallAbort(IdleStallTimeoutMs)))
            return false;

        var after = Svc.Objects.LocalPlayer?.Position;
        if (before is null || after is null) return false;

        var moved = Vector3.Distance(before.Value, after.Value);
        if (moved < TeleportRetryProgressMeters)
        {
            Diag($"{tribe.Name}: teleport moved only {moved:F1}m; treating as failed");
            return false;
        }
        return true;
    }

    // After a teleport the destination zone's navmesh is still building; a pathfind issued now races
    // vnavmesh and faults with "navmesh creation is in progress". Hold here until ready.
    private async Task WaitForNavmeshReady()
    {
        if (NavmeshIPC.Instance.IsReady()) return;

        var deadline = Environment.TickCount64 + NavmeshReadyWaitMs;
        while (!NavmeshIPC.Instance.IsReady())
        {
            if (CancelToken.IsCancellationRequested) return;
            if (Environment.TickCount64 >= deadline)
            {
                Diag($"{tribe.Name}: navmesh not ready within {NavmeshReadyWaitMs / 1000}s; proceeding anyway");
                return;
            }
            var progress = NavmeshIPC.Instance.BuildProgress();
            Status = progress is >= 0f and <= 1f
                ? $"Please wait — navmesh is loading ({progress * 100f:F0}%)"
                : "Please wait — navmesh is loading…";
            await NextFrame(60);
        }
    }

    private async Task<bool> EnsureCorrectJob()
    {
        if (JobSwitcher.CurrentJobSatisfies(tribe.Kind)) return true;

        var gearsetId = JobSwitcher.PickGearset(tribe, Plugin.Cfg);
        if (gearsetId < 0)
        {
            Warning($"{tribe.Name}: no {tribe.Kind} gearset found — create one for this tribe's job type — skipping");
            return false;
        }

        var targetJob = JobSwitcher.GearsetClassJob(gearsetId);
        Status = $"Switching to gearset {gearsetId}";
        Diag($"{tribe.Name}: equipping gearset {gearsetId} (target ClassJob {targetJob})");
        if (!JobSwitcher.EquipGearset(gearsetId))
        {
            Warning($"{tribe.Name}: EquipGearset rejected (in combat / occupied / between areas) — skipping");
            return false;
        }

        // EquipGearset returning 0 only means the request was dispatched — the swap is async. Confirm it
        // actually landed; if it never does, skip rather than run the tribe on the wrong job.
        for (var f = 0; f < AdtConstants.GearsetSwitchFrames; f++)
        {
            await NextFrame();
            if (JobSwitcher.CurrentClassJob() == targetJob) return true;
        }

        Warning($"{tribe.Name}: job did not switch to {targetJob} within time limit — skipping");
        return false;
    }

    private async Task<ExitReason> DoAcceptPass()
    {
        var before = tribe.AcceptedTodayCount;
        var remaining = Math.Min(tribe.AcceptSlotsRemaining, tribe.DailyAllowanceLeft);

        Status = $"Talking to {tribe.Name} issuer";
        await AcceptDailies(remaining);
        TribeStateReader.Refresh(tribe);

        if (tribe.AcceptedTodayCount <= before)
        {
            acceptFailPasses++;
            if (acceptFailPasses >= 2)
            {
                Warning($"{tribe.Name}: could not accept dailies at the issuer (two passes made no progress); skipping");
                return ExitReason.Quit;
            }
            // Re-path to the issuer in case we drifted out of interaction range before the next pass.
            arrivedAtIssuer = false;
        }
        else
        {
            acceptFailPasses = 0;
        }
        return ExitReason.Continue;
    }

    private async Task AcceptDailies(int slotsToFill)
    {
        var startCount = tribe.AcceptedTodayCount;
        var targetCount = Math.Min(startCount + slotsToFill, AdtConstants.MaxAcceptsPerTribe);
        Diag($"{tribe.Name}: AcceptDailies {startCount} -> {targetCount} (+{slotsToFill})");

        const int maxFrames = 3600;
        var frame = 0;
        var lastInteractFrame = -100;

        while (frame < maxFrames)
        {
            await NextFrame();
            frame++;

            if (frame % 20 == 0)
            {
                TribeStateReader.Refresh(tribe);
                Status = $"Accepted {tribe.AcceptedTodayCount}/{targetCount}";
                if (tribe.AcceptedTodayCount >= targetCount)
                {
                    Diag($"{tribe.Name}: AcceptDailies reached {tribe.AcceptedTodayCount}/{targetCount}");
                    if (AddonProbes.SelectIconStringActive()) AddonInteractions.SelectIconStringCancel();
                    return;
                }
            }

            if (AddonProbes.SelectYesnoActive())
            {
                AddonSelectYesno.Yes();
                await PauseFrames(10);
                continue;
            }
            if (AddonProbes.JournalAcceptActive())
            {
                AddonInteractions.JournalAcceptConfirm();
                await PauseFrames(10);
                continue;
            }
            if (AddonProbes.TalkActive())
            {
                AddonInteractions.ProgressTalk();
                await PauseFrames(3);
                continue;
            }
            if (AddonProbes.SelectIconStringActive())
            {
                AddonInteractions.SelectIconStringPick(0);
                await PauseFrames(10);
                continue;
            }
            if (AddonProbes.SelectStringActive())
            {
                AddonSelectString.Select(tribe.IssuerSelectStringIndex);
                await PauseFrames(10);
                continue;
            }

            // Nothing on screen — re-interact, but only if we're not already in a conversation.
            // OccupiedInQuestEvent / OccupiedInEvent are what FFXIV sets while an NPC dialog is active;
            // spamming InteractWith during that closes the dialog we just opened. Same gate Questionable uses.
            var inConversation = Svc.Condition[ConditionFlag.OccupiedInQuestEvent]
                              || Svc.Condition[ConditionFlag.OccupiedInEvent];

            if (!inConversation
                && frame - lastInteractFrame >= 180  // 3s @ ~60fps — slack for the dialog to surface
                && !Svc.Condition[ConditionFlag.Jumping]
                && !Svc.Condition[ConditionFlag.Jumping61]
                && !Svc.Condition[ConditionFlag.Casting])
            {
                var ok = AddonInteractions.InteractWith(tribe.IssuerInstanceId);
                Diag($"{tribe.Name}: frame {frame} re-interacting -> triggered={ok}");
                lastInteractFrame = frame;
            }
        }

        Warning($"{tribe.Name}: AcceptDailies timed out at {tribe.AcceptedTodayCount}/{targetCount} after {maxFrames} frames");
        if (AddonProbes.SelectIconStringActive()) AddonInteractions.SelectIconStringCancel();
    }

    private async Task PauseFrames(int n)
    {
        for (var i = 0; i < n; i++) await NextFrame();
    }

    private async Task DelegateToQuestionable(uint[] accepted)
    {
        for (var i = 0; i < accepted.Length; i++)
        {
            var quest = accepted[i];
            if (_questionable.IsQuestComplete(quest)) continue;

            Status = $"Questionable: quest {i + 1}/{accepted.Length}";
            Diag($"{tribe.Name}: StartSingleQuest {quest:X} ({QuestName(quest)})");
            ErrorIf(!_questionable.StartSingleQuest(quest),
                $"Questionable.StartSingleQuest rejected quest {quest:X} ({QuestName(quest)})");

            await RunQuestionableQuest(quest);
        }
    }

    // StartSingleQuest sets Questionable's automation type synchronously, so IsRunning should flip true
    // within a few frames. We wait for it to engage (bounded), then wait for it to finish (bounded), so
    // neither a no-op start nor a hung quest can lock the tribe forever.
    private async Task RunQuestionableQuest(uint quest)
    {
        var startFrame = 0;
        while (startFrame < AdtConstants.QuestStartFrames && !_questionable.IsRunning())
        {
            if (_questionable.IsQuestComplete(quest)) return;
            await NextFrame();
            startFrame++;
        }

        if (!_questionable.IsRunning())
        {
            Warning($"{tribe.Name}: Questionable never engaged on {quest:X} ({QuestName(quest)}) — skipping");
            return;
        }

        var runFrame = 0;
        while (_questionable.IsRunning())
        {
            ErrorIf(runFrame++ >= AdtConstants.QuestRunFrames,
                $"Questionable run on {quest:X} ({QuestName(quest)}) exceeded time limit");
            await NextFrame();
        }
    }
}
