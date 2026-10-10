using clib.Extensions;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace AutoDailyTribes.Core.Game;

// With Aetheryte Tickets in the bag and the game's Aetheryte Ticket Usage on a Confirm mode, Telepo.Teleport
// opens a SelectYesno ("spend a ticket?") instead of casting, and refuses every further call while it is open.
// The invoker that owns the dialog records its addon id, which identifies the prompt without reading its text.
internal static unsafe class TeleportTicketPrompt
{
    public static bool IsOpen
    {
        get
        {
            var promptAddonId = Telepo.Instance()->UseTicketInvoker.AddonId;
            if (promptAddonId == 0) return false;
            var addon = AddonProbes.Get("SelectYesno");
            return addon != null && addon->Id == promptAddonId && AddonProbes.Ready(addon);
        }
    }

    public static void PayGil() => AddonSelectYesno.No();
}
