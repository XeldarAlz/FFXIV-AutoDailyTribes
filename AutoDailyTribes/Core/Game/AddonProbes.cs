using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace AutoDailyTribes.Core.Game;

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

    // Per WigglyMuffin/Questionable: option count is at AtkValues[5].Int,
    // option text at AtkValues[7 + 3*i].
    public static int SelectIconStringOptionCount()
    {
        var a = Get("SelectIconString");
        if (a == null || a->AtkValuesCount < 6) return 0;
        return a->AtkValues[5].Int;
    }
}
