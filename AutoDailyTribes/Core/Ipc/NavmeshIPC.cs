using Dalamud.Plugin.Ipc;
using ECommons.DalamudServices;

namespace AutoDailyTribes.Core.Ipc;

internal sealed class NavmeshIPC
{
    private static NavmeshIPC? instance;
    public static NavmeshIPC Instance => instance ??= new NavmeshIPC();

    private readonly ICallGateSubscriber<bool> pathIsRunning;
    private readonly ICallGateSubscriber<bool> simpleMovePathfindInProgress;
    private readonly ICallGateSubscriber<bool> navPathfindInProgress;
    private readonly ICallGateSubscriber<bool> navIsReady;
    private readonly ICallGateSubscriber<float> navBuildProgress;

    private NavmeshIPC()
    {
        pathIsRunning                = Svc.PluginInterface.GetIpcSubscriber<bool>("vnavmesh.Path.IsRunning");
        simpleMovePathfindInProgress = Svc.PluginInterface.GetIpcSubscriber<bool>("vnavmesh.SimpleMove.PathfindInProgress");
        navPathfindInProgress        = Svc.PluginInterface.GetIpcSubscriber<bool>("vnavmesh.Nav.PathfindInProgress");
        navIsReady                   = Svc.PluginInterface.GetIpcSubscriber<bool>("vnavmesh.Nav.IsReady");
        navBuildProgress             = Svc.PluginInterface.GetIpcSubscriber<float>("vnavmesh.Nav.BuildProgress");
    }

    public bool IsAvailable => pathIsRunning.HasFunction;

    public bool IsReady()
    {
        if (!navIsReady.HasFunction) return true;
        try { return navIsReady.InvokeFunc(); }
        catch (Exception ex) { Svc.Log.Warning(ex, "[ADT] NavmeshIPC.IsReady failed"); return true; }
    }

    // 0..1 while building; -1 when idle/complete
    public float BuildProgress()
    {
        if (!navBuildProgress.HasFunction) return -1f;
        try { return navBuildProgress.InvokeFunc(); }
        catch (Exception ex) { Svc.Log.Warning(ex, "[ADT] NavmeshIPC.BuildProgress failed"); return -1f; }
    }

    public bool IsRunning()
    {
        if (!pathIsRunning.HasFunction) return false;
        try { return pathIsRunning.InvokeFunc(); }
        catch (Exception ex) { Svc.Log.Warning(ex, "[ADT] NavmeshIPC.IsRunning failed"); return false; }
    }

    public bool IsBusy()
    {
        if (IsRunning()) return true;
        if (simpleMovePathfindInProgress.HasFunction)
        {
            try { if (simpleMovePathfindInProgress.InvokeFunc()) return true; }
            catch (Exception ex) { Svc.Log.Warning(ex, "[ADT] NavmeshIPC.PathfindInProgress(SimpleMove) failed"); }
        }
        if (navPathfindInProgress.HasFunction)
        {
            try { if (navPathfindInProgress.InvokeFunc()) return true; }
            catch (Exception ex) { Svc.Log.Warning(ex, "[ADT] NavmeshIPC.PathfindInProgress(Nav) failed"); }
        }
        return false;
    }
}
