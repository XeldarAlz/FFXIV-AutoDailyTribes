using Dalamud.Plugin.Ipc;
using ECommons.DalamudServices;
using System.Numerics;

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
    private readonly ICallGateSubscriber<object> pathStop;
    private readonly ICallGateSubscriber<float> pathGetTolerance;
    private readonly ICallGateSubscriber<Vector3, bool, float, bool> pathfindAndMoveCloseTo;
    private readonly ICallGateSubscriber<Vector3, float, float, Vector3?> nearestPointReachable;
    private readonly ICallGateSubscriber<List<Vector3>, bool, object> pathMoveTo;

    private NavmeshIPC()
    {
        pathIsRunning                = Svc.PluginInterface.GetIpcSubscriber<bool>("vnavmesh.Path.IsRunning");
        simpleMovePathfindInProgress = Svc.PluginInterface.GetIpcSubscriber<bool>("vnavmesh.SimpleMove.PathfindInProgress");
        navPathfindInProgress        = Svc.PluginInterface.GetIpcSubscriber<bool>("vnavmesh.Nav.PathfindInProgress");
        navIsReady                   = Svc.PluginInterface.GetIpcSubscriber<bool>("vnavmesh.Nav.IsReady");
        navBuildProgress             = Svc.PluginInterface.GetIpcSubscriber<float>("vnavmesh.Nav.BuildProgress");
        pathStop                     = Svc.PluginInterface.GetIpcSubscriber<object>("vnavmesh.Path.Stop");
        pathGetTolerance             = Svc.PluginInterface.GetIpcSubscriber<float>("vnavmesh.Path.GetTolerance");
        pathfindAndMoveCloseTo        = Svc.PluginInterface.GetIpcSubscriber<Vector3, bool, float, bool>("vnavmesh.SimpleMove.PathfindAndMoveCloseTo");
        nearestPointReachable        = Svc.PluginInterface.GetIpcSubscriber<Vector3, float, float, Vector3?>("vnavmesh.Query.Mesh.NearestPointReachable");
        pathMoveTo                   = Svc.PluginInterface.GetIpcSubscriber<List<Vector3>, bool, object>("vnavmesh.Path.MoveTo");
    }

    public bool IsAvailable => pathIsRunning.HasFunction;

    // Movement calls propagate IPC failures to the operation's fault handler.
    public bool Ready => navIsReady.InvokeFunc();
    public float LoadingProgress => navBuildProgress.InvokeFunc();
    public bool IsPathfinding => navPathfindInProgress.InvokeFunc();
    public bool IsMovePending => simpleMovePathfindInProgress.InvokeFunc();
    public float GetTolerance() => pathGetTolerance.InvokeFunc();
    public bool PathfindAndMoveCloseTo(Vector3 destination, bool fly, float tolerance)
        => pathfindAndMoveCloseTo.InvokeFunc(destination, fly, tolerance);
    public Vector3? NearestPointReachable(Vector3 position)
        => nearestPointReachable.InvokeFunc(position, 5f, 5f);

    public void Stop()
    {
        if (pathStop.HasAction) pathStop.InvokeAction();
    }

    // Landing picks its spots outside any movement operation, so a failed query reads as no floor instead of throwing.
    public Vector3? ReachableFloorNear(Vector3 position, float halfExtentXZ, float halfExtentY)
    {
        if (!nearestPointReachable.HasFunction)
        {
            return null;
        }

        try { return nearestPointReachable.InvokeFunc(position, halfExtentXZ, halfExtentY); }
        catch (Exception ex) { RunLog.Warning(ex, "NavmeshIPC.ReachableFloorNear failed"); return null; }
    }

    // Follows the waypoints as given, with no path search, so the caller has to know the way is clear.
    public void MoveAlong(List<Vector3> waypoints, bool fly)
    {
        if (!pathMoveTo.HasAction)
        {
            return;
        }

        try { pathMoveTo.InvokeAction(waypoints, fly); }
        catch (Exception ex) { RunLog.Warning(ex, "NavmeshIPC.MoveAlong failed"); }
    }

    public bool IsReady()
    {
        if (!navIsReady.HasFunction) return true;
        try { return navIsReady.InvokeFunc(); }
        catch (Exception ex) { RunLog.Warning(ex, "NavmeshIPC.IsReady failed"); return true; }
    }

    // 0..1 while building; -1 when idle/complete
    public float BuildProgress()
    {
        if (!navBuildProgress.HasFunction) return -1f;
        try { return navBuildProgress.InvokeFunc(); }
        catch (Exception ex) { RunLog.Warning(ex, "NavmeshIPC.BuildProgress failed"); return -1f; }
    }

    public bool IsRunning()
    {
        if (!pathIsRunning.HasFunction) return false;
        try { return pathIsRunning.InvokeFunc(); }
        catch (Exception ex) { RunLog.Warning(ex, "NavmeshIPC.IsRunning failed"); return false; }
    }

    public bool IsBusy()
    {
        if (IsRunning()) return true;
        if (simpleMovePathfindInProgress.HasFunction)
        {
            try { if (simpleMovePathfindInProgress.InvokeFunc()) return true; }
            catch (Exception ex) { RunLog.Warning(ex, "NavmeshIPC.PathfindInProgress(SimpleMove) failed"); }
        }
        if (navPathfindInProgress.HasFunction)
        {
            try { if (navPathfindInProgress.InvokeFunc()) return true; }
            catch (Exception ex) { RunLog.Warning(ex, "NavmeshIPC.PathfindInProgress(Nav) failed"); }
        }
        return false;
    }
}
