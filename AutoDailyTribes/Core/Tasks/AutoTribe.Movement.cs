using AutoDailyTribes.Core.Ipc;
using clib.Extensions;
using clib.TaskSystem;
using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using System.Numerics;
using System.Threading.Tasks;

namespace AutoDailyTribes.Core.Tasks;

public sealed partial class AutoTribe
{
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
}
