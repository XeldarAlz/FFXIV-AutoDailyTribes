using Dalamud.Plugin.Ipc;
using ECommons.DalamudServices;

namespace AutoDailyTribes.Core.Ipc;

// vnavmesh state, re-subscribed because clib's own wrapper is internal. Used by the travel watchdog to
// tell a real terrain wedge (vnav following, no displacement) from a pre-pathfind idle wedge.
internal sealed class NavmeshIPC
{
    private static NavmeshIPC? instance;
    public static NavmeshIPC Instance => instance ??= new NavmeshIPC();

    private readonly ICallGateSubscriber<bool> pathIsRunning;
    private readonly ICallGateSubscriber<bool> simpleMovePathfindInProgress;
    private readonly ICallGateSubscriber<bool> navPathfindInProgress;
    private readonly ICallGateSubscriber<bool> navIsReady;
    private readonly ICallGateSubscriber<float> navBuildProgress;
    private readonly ICallGateSubscriber<object> pathStop;

    private NavmeshIPC()
    {
        pathIsRunning                = Svc.PluginInterface.GetIpcSubscriber<bool>("vnavmesh.Path.IsRunning");
        simpleMovePathfindInProgress = Svc.PluginInterface.GetIpcSubscriber<bool>("vnavmesh.SimpleMove.PathfindInProgress");
        navPathfindInProgress        = Svc.PluginInterface.GetIpcSubscriber<bool>("vnavmesh.Nav.PathfindInProgress");
        navIsReady                   = Svc.PluginInterface.GetIpcSubscriber<bool>("vnavmesh.Nav.IsReady");
        navBuildProgress             = Svc.PluginInterface.GetIpcSubscriber<float>("vnavmesh.Nav.BuildProgress");
        pathStop                     = Svc.PluginInterface.GetIpcSubscriber<object>("vnavmesh.Path.Stop");
    }

    public bool IsAvailable => pathIsRunning.HasFunction;

    // True once the navmesh for the current zone is fully built and queryable. While false, pathfind IPC
    // races vnavmesh's background build and throws "navmesh creation is in progress".
    public bool IsReady()
    {
        if (!navIsReady.HasFunction) return true; // older vnavmesh without the gate: assume ready.
        try { return navIsReady.InvokeFunc(); }
        catch (Exception ex) { Svc.Log.Warning(ex, "[ADT] NavmeshIPC.IsReady failed"); return true; }
    }

    // 0..1 while a build is in progress; -1 when idle/complete. User-facing progress hint only.
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

    public void Stop()
    {
        if (!pathStop.HasFunction) return;
        try { pathStop.InvokeAction(); }
        catch (Exception ex) { Svc.Log.Warning(ex, "[ADT] NavmeshIPC.Stop failed"); }
    }
}
