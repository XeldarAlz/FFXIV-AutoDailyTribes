using clib.TaskSystem;
using System.Numerics;
using System.Threading.Tasks;
using AutoTask = clib.Services.AutoTask;

namespace AutoDailyTribes.Core.Tasks;

// Each operation owns cancellation, but no native hooks. Travel runs through vnavmesh.
internal sealed partial class MoveOp(System.Func<MoveOp, Task> body) : AutoTask
{
    public System.Exception? Fault { get; private set; }

    protected override async Task Execute()
    {
        try { await body(this); }
        catch (System.OperationCanceledException) when (CancelToken.IsCancellationRequested) { }
        catch (System.Exception ex) { Fault = ex; }
    }

    public Task Move(uint territoryId, Vector3 dest, MovementConfig config, bool allowTeleportIfFaster,
                     System.Func<bool>? stopCondition, bool allowAethernetWithinTerritory)
        => Travel(territoryId, dest, config, allowTeleportIfFaster, stopCondition, allowAethernetWithinTerritory);

    public Task Teleport(uint territoryId, Vector3 dest, bool allowSameZoneTeleport)
        => TeleportToTerritory(territoryId, dest, allowSameZoneTeleport);

    public Task DismountNow() => Dismount();
}
