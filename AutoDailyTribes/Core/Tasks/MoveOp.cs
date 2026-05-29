using clib.TaskSystem;
using System.Numerics;
using System.Threading.Tasks;

namespace AutoDailyTribes.Core.Tasks;

// A single clib movement/teleport operation run as its OWN AutoTask, so it owns its own
// CancellationTokenSource. The parent loop can Cancel() exactly one operation without tearing down the
// whole run — clib's Cancel() fires the task's registered cleanups (movement override off, the MoveTo
// OnDispose that stops vnav) and cancels every await, so the operation unwinds instead of leaking.
// clib's MoveTo/TeleportTo expose no per-call cancellation of their own, which is why abandoning them
// would leave a zombie flow that keeps re-issuing teleports and stops the next move's navigation.
internal sealed class MoveOp(System.Func<MoveOp, Task> body) : TaskBase
{
    // clib's task runner awaits Execute with SuppressThrowing, so a clib ErrorIf (e.g. "Failed to start
    // pathfinding") would otherwise vanish and look like a clean completion. Capture it so the caller can
    // tell a genuine arrival from a faulted move and recover instead of treating the spot as reached.
    public System.Exception? Fault { get; private set; }

    protected override async Task Execute()
    {
        try { await body(this); }
        catch (System.OperationCanceledException) { /* cancelled by watchdog/Stop — expected */ }
        catch (System.Exception ex) { Fault = ex; }
    }

    public Task Move(uint territoryId, Vector3 dest, MovementConfig config, bool allowTeleportIfFaster,
                     System.Func<bool>? stopCondition, bool allowAethernetWithinTerritory)
        => MoveTo(territoryId, dest, config, allowTeleportIfFaster, stopCondition, null, allowAethernetWithinTerritory);

    public Task Teleport(uint territoryId, Vector3 dest, bool allowSameZoneTeleport)
        => TeleportTo(territoryId, dest, allowSameZoneTeleport);

    public Task DismountNow() => Dismount();
}
