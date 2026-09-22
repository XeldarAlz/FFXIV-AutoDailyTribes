using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace AutoDailyTribes.Core.Game;

// Telepo keeps ActiveTeleportRequest raised from the moment a teleport is requested until the
// server answers. A request the server drops (issued while the character was still finishing a
// quest event, for instance) leaves it raised for good, and every later Teleport call is refused
// with "Unable to teleport. Another teleport is already underway." until it is lowered again.
internal static unsafe class TeleportProbe
{
    private const uint TeleportActionId = 5;

    public static bool RequestPending => Telepo.Instance()->ActiveTeleportRequest;

    public static uint ActionStatus => ActionManager.Instance()->GetActionStatus(ActionType.Action, TeleportActionId);

    public static bool AnimationLocked => ActionManager.Instance()->AnimationLock > 0f;

    public static void DropPendingRequest() => Telepo.Instance()->ActiveTeleportRequest = false;
}
