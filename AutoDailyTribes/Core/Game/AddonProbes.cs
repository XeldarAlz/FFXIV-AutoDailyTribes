using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace AutoDailyTribes.Core.Game;

internal static unsafe class AddonProbes
{
    public static AtkUnitBase* Get(string name)
        => RaptureAtkUnitManager.Instance()->GetAddonByName(name);

    // Match ECommons GenericHelpers.IsAddonReady — what Questionable's `TryGetAddonByName`
    // resolves to. AtkUnitBase.IsReady alone misses addons that are visible but mid-load.
    public static bool Ready(string name)
    {
        var a = Get(name);
        return a != null
            && a->IsVisible
            && a->UldManager.LoadedState == AtkLoadState.Loaded
            && a->IsFullyLoaded();
    }

    public static bool TalkActive() => Ready("Talk");
    public static bool SelectStringActive() => Ready("SelectString");
    public static bool SelectIconStringActive() => Ready("SelectIconString");
    public static bool SelectYesnoActive() => Ready("SelectYesno");
    public static bool JournalAcceptActive() => Ready("JournalAccept");

    // AtkValue layout per WigglyMuffin/Questionable: count at [5].Int, option text at [7 + 3*i].
    public static int SelectIconStringOptionCount()
    {
        var a = Get("SelectIconString");
        if (a == null || a->AtkValuesCount < 6) return 0;
        return a->AtkValues[5].Int;
    }

    public static string AllVisibleAddons()
    {
        var mgr = RaptureAtkUnitManager.Instance();
        if (mgr == null) return "";
        var list = new List<string>();
        var span = mgr->AllLoadedUnitsList.Entries;
        for (var i = 0; i < span.Length; i++)
        {
            var unit = span[i].Value;
            if (unit == null) continue;
            if (!unit->IsVisible) continue;
            var n = unit->NameString;
            if (!string.IsNullOrEmpty(n)) list.Add(n);
        }
        return string.Join(", ", list);
    }

    public static string[] SelectIconStringOptions()
    {
        var a = Get("SelectIconString");
        if (a == null) return [];
        var count = SelectIconStringOptionCount();
        var result = new string[count];
        for (var i = 0; i < count; i++)
        {
            var idx = 7 + 3 * i;
            if (idx >= a->AtkValuesCount) { result[i] = "?"; continue; }
            var v = a->AtkValues[idx];
            result[i] = (v.Type == FFXIVClientStructs.FFXIV.Component.GUI.AtkValueType.String
                       || v.Type == FFXIVClientStructs.FFXIV.Component.GUI.AtkValueType.ManagedString)
                       && v.String.Value != null
                ? System.Text.Encoding.UTF8.GetString(System.Runtime.InteropServices.MemoryMarshal.CreateReadOnlySpanFromNullTerminated(v.String.Value))
                : "?";
        }
        return result;
    }
}
