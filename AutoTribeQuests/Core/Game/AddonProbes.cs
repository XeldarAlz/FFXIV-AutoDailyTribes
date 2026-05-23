using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace AutoTribeQuests.Core.Game;

// Read-only probes against the AtkUnitManager. No mutations. Cheap enough to
// poll every frame inside a coroutine wait condition.
internal static unsafe class AddonProbes
{
    public static AtkUnitBase* Get(string name)
        => RaptureAtkUnitManager.Instance()->GetAddonByName(name);

    public static bool Ready(string name)
    {
        var a = Get(name);
        return a != null && a->IsVisible && a->IsReady;
    }

    public static bool TalkActive() => Ready("Talk");
    public static bool SelectStringActive() => Ready("SelectString");
    public static bool SelectIconStringActive() => Ready("SelectIconString");
    public static bool SelectYesnoActive() => Ready("SelectYesno");
    public static bool JournalAcceptActive() => Ready("JournalAccept");

    // SelectIconString stores its option count in AtkValues[0]. Verify in-game
    // once we test against a real issuer; the layout may differ slightly.
    public static int SelectIconStringOptionCount()
    {
        var a = Get("SelectIconString");
        if (a == null || a->AtkValuesCount < 1) return 0;
        return a->AtkValues[0].Int;
    }
}
