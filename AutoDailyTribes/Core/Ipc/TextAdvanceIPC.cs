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
            RunLog.Warning(ex, "subscribe failed");
        }
    }

    public static bool IsPluginEnabled()
    {
        EnsureInit();
        try { return isEnabled?.HasFunction != true || isEnabled.InvokeFunc(); }
        catch (Exception ex) { RunLog.Warning(ex, "IsEnabled failed"); return true; }
    }
}
