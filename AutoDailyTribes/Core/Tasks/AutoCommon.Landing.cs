using AutoDailyTribes.Core.Ipc;
using clib.Extensions;
using clib.TaskSystem;
using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using System.Numerics;
using System.Threading.Tasks;

namespace AutoDailyTribes.Core.Tasks;

public abstract partial class AutoCommon
{
    private const int   MaxLandingSpots = 3;
    private const float LandingRingRadiusMeters = 6f;
    private const float LandingFloorHalfExtentMeters = 3f;
    private const float LandingFloorHalfHeightMeters = 5f;
    private const float LandingArriveMeters = 1.5f;
    private const int   LandingFlightWatchdogMs = 20_000;
    private const int   LandingDismountWatchdogMs = 6_000;
    private const int   GroundDismountWatchdogMs = 8_000;
    private const float LandingBackOffMeters = 8f;
    private const int   LandingBackOffMs = 1_500;
    private const int   AirborneFreezeMs = 2_000;
    private const float AirborneFreezeMeters = 0.25f;
    private const uint  JumpGeneralActionId = 2;

    // Sixths of a turn from the bearing that points away from the character: the far side first, the near side last.
    private static readonly int[] LandingRingStepsAwayFirst = [0, 1, -1, 2, -2, 3];

    // No mount options: the pathfinder keeps flying because the character is airborne, and the operation skips its own landing.
    private static readonly MovementConfig LandingFlight = MovementConfig.Default.WithTolerance(LandingArriveMeters);

    private readonly Vector3[] landingSpots = new Vector3[MaxLandingSpots];

    // The movement operation's dismount descends straight down from wherever the flight ended, and beside a quest giver
    // that can be a fence the mount hovers on for good (Yadovv Gah). So the flight is aimed at reachable floor first,
    // and a descent that freezes is cut short and tried again from the next spot.
    protected async Task<bool> LandAndDismount(Vector3 groundPoint, string scope)
    {
        if (!Svc.Condition[ConditionFlag.InFlight])
        {
            return await GroundDismount(scope);
        }

        var from = Svc.Objects.LocalPlayer?.Position ?? groundPoint;
        var found = FindLandingSpots(groundPoint, from, landingSpots);
        if (found == 0)
        {
            Diag($"{scope}: no reachable floor around {FormatPosition(groundPoint)}; descending where the flight ended");
            return await DescendAndDismount(scope);
        }

        for (var spotIndex = 0; spotIndex < found; spotIndex++)
        {
            if (CancelToken.IsCancellationRequested)
            {
                return false;
            }

            var spot = landingSpots[spotIndex];
            var legScope = $"{scope}-land#{spotIndex + 1}";
            Diag($"{legScope}: flying {DistanceTo(spot):F1}m to the floor at {FormatPosition(spot)}");
            var stuck = new TravelStuckTracker();
            var flight = new MoveOp(move => move.MoveInZone(spot, LandingFlight, null));
            await RunCancellable(flight, LandingFlightWatchdogMs, legScope, () => stuck.Check() != StallKind.None);
            if (flight.Fault is { } fault)
            {
                Diag($"{legScope}: the landing flight faulted: {fault.Message}");
            }

            if (await DescendAndDismount(legScope))
            {
                return true;
            }

            if (CancelToken.IsCancellationRequested)
            {
                return false;
            }

            await BackOffInFlight(legScope);
        }

        Diag($"{scope}: could not land near {FormatPosition(groundPoint)} after {found} spot(s)");
        return false;
    }

    protected static float DistanceTo(Vector3 target)
        => Svc.Objects.LocalPlayer is { } player ? Vector3.Distance(player.Position, target) : float.MaxValue;

    private async Task<bool> DescendAndDismount(string scope)
    {
        if (!Svc.Condition[ConditionFlag.InFlight])
        {
            return await GroundDismount(scope);
        }

        await RunCancellable(new MoveOp(move => move.DismountNow()), LandingDismountWatchdogMs, scope, AirborneFreezeAbort(scope));
        if (!Svc.Condition[ConditionFlag.Mounted])
        {
            return true;
        }

        Diag($"{scope}: the descent did not land");
        CancelDescent();
        return false;
    }

    private async Task<bool> GroundDismount(string scope)
    {
        if (Svc.Condition[ConditionFlag.Mounted])
        {
            await RunCancellable(new MoveOp(move => move.DismountNow()), GroundDismountWatchdogMs, scope);
        }

        return !Svc.Condition[ConditionFlag.Mounted];
    }

    // Jumping in the air ends the game's own descent, which otherwise carries on after the operation is cancelled and
    // reads as player input to the pathfinder, so the next path would be dropped the moment it is queued.
    private static void CancelDescent()
    {
        if (Svc.Condition[ConditionFlag.InFlight])
        {
            ActionManager.UseAction(ActionType.GeneralAction, JumpGeneralActionId);
        }
    }

    // Straight up retraces the descent, so it is the one direction known to be clear.
    private async Task BackOffInFlight(string scope)
    {
        if (Svc.Objects.LocalPlayer is not { } player || !Svc.Condition[ConditionFlag.InFlight])
        {
            return;
        }

        var above = player.Position with { Y = player.Position.Y + LandingBackOffMeters };
        Diag($"{scope}: backing off {LandingBackOffMeters:F0}m upward");
        NavmeshIPC.Instance.MoveAlong([above], fly: true);
        try
        {
            await DelayMs(LandingBackOffMs);
        }
        finally
        {
            NavmeshIPC.Instance.Stop();
        }
    }

    // The floor at the point first, then a ring around it from the side facing away from the character, where the
    // flight's arrival slack falls over the point's own floor and not back over whatever stopped the first descent.
    // The reachable floor nearest each probe keeps the spots on the point's own level.
    private static int FindLandingSpots(Vector3 groundPoint, Vector3 from, Vector3[] spots)
    {
        var navmesh = NavmeshIPC.Instance;
        var found = 0;
        if (navmesh.ReachableFloorNear(groundPoint, LandingFloorHalfExtentMeters, LandingFloorHalfHeightMeters) is { } center)
        {
            spots[found++] = center;
        }

        var awayAngle = MathF.Atan2(groundPoint.Z - from.Z, groundPoint.X - from.X);
        var steps = LandingRingStepsAwayFirst;
        for (var stepIndex = 0; stepIndex < steps.Length && found < spots.Length; stepIndex++)
        {
            var angle = awayAngle + MathF.Tau * steps[stepIndex] / steps.Length;
            var probe = groundPoint + new Vector3(MathF.Cos(angle) * LandingRingRadiusMeters, 0f, MathF.Sin(angle) * LandingRingRadiusMeters);
            if (navmesh.ReachableFloorNear(probe, LandingFloorHalfExtentMeters, LandingFloorHalfHeightMeters) is { } floor)
            {
                spots[found++] = floor;
            }
        }

        return found;
    }

    private static Func<bool> AirborneFreezeAbort(string label)
    {
        Vector3? anchor = null;
        var frozenSinceMs = Environment.TickCount64;
        return () =>
        {
            var now = Environment.TickCount64;
            if (Svc.Objects.LocalPlayer is not { } player || !Svc.Condition[ConditionFlag.InFlight])
            {
                anchor = null;
                frozenSinceMs = now;
                return false;
            }

            var position = player.Position;
            if (anchor is null || Vector3.Distance(anchor.Value, position) > AirborneFreezeMeters)
            {
                anchor = position;
                frozenSinceMs = now;
                return false;
            }

            if (now - frozenSinceMs < AirborneFreezeMs)
            {
                return false;
            }

            RunLog.Info($"{label}: froze in the air at {FormatPosition(position)} for {AirborneFreezeMs / 1000}s; cutting the descent short");
            return true;
        };
    }

    private static string FormatPosition(Vector3 position)
        => $"({position.X:F1},{position.Y:F1},{position.Z:F1})";
}
