using Dalamud.Plugin.Ipc;
using ECommons.DalamudServices;

namespace AutoDailyTribes.Core.Ipc;

internal static class TextAdvanceIPC
{
    private static ICallGateSubscriber<bool>? isEnabled;
    private static bool initialized;

    private static void EnsureInit()
    {
        if (initialized) return;
        initialized = true;
        try
        {
            isEnabled = Svc.PluginInterface.GetIpcSubscriber<bool>("TextAdvance.IsEnabled");
        }
        catch (Exception ex)
        {
            Svc.Log.Warning(ex, "[TextAdvanceIPC] subscribe failed");
        }
    }

    // TextAdvance's own "Enable plugin" toggle. Questionable leans on this global toggle to advance
    // dialogue/cutscenes, so off = quests stall. Returns true when the gate is absent/errors so we
    // never raise a false warning when the IPC simply isn't reachable.
    public static bool IsPluginEnabled()
    {
        EnsureInit();
        try { return isEnabled?.HasFunction != true || isEnabled.InvokeFunc(); }
        catch (Exception ex) { Svc.Log.Warning(ex, "[TextAdvanceIPC] IsEnabled failed"); return true; }
    }
}
