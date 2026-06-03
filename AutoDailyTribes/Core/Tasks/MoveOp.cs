using clib.TaskSystem;
using System.Numerics;
using System.Threading.Tasks;

namespace AutoDailyTribes.Core.Tasks;

internal sealed class MoveOp(System.Func<MoveOp, Task> body) : TaskBase
{
    public System.Exception? Fault { get; private set; }

    protected override async Task Execute()
    {
        try { await body(this); }
        catch (System.OperationCanceledException) { }
        catch (System.Exception ex) { Fault = ex; }
    }

    public Task Move(uint territoryId, Vector3 dest, MovementConfig config, bool allowTeleportIfFaster,
                     System.Func<bool>? stopCondition, bool allowAethernetWithinTerritory)
        => MoveTo(territoryId, dest, config, allowTeleportIfFaster, stopCondition, null, allowAethernetWithinTerritory);

    public Task Teleport(uint territoryId, Vector3 dest, bool allowSameZoneTeleport)
        => TeleportTo(territoryId, dest, allowSameZoneTeleport);

    public Task DismountNow() => Dismount();
}
