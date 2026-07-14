using AutoDailyTribes.Core.Game;
using AutoDailyTribes.Core.Ipc;
using clib.TaskSystem;
using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using System.Numerics;
using System.Threading.Tasks;

namespace AutoDailyTribes.Core.Tasks;

public abstract class AutoCommon : TaskBase
{
    protected const int   SubTaskUnwindGraceMs = 5_000;
    internal  const float StuckMoveThresholdMeters = 1.5f;
    internal  const int   HardStuckTimeoutMs = 3_000;
    internal  const int   IdleStallTimeoutMs = 8_000;

    protected static string QuestName(uint questId)
        => Svc.Data.GetExcelSheet<Lumina.Excel.Sheets.Quest>()?.GetRowOrDefault(questId)?.Name.ToString() ?? questId.ToString();

    internal static bool IsPositionFrozenLegit()
        => Svc.Condition[ConditionFlag.Casting]
        || Svc.Condition[ConditionFlag.Casting87]
        || Svc.Condition[ConditionFlag.Mounting]
        || Svc.Condition[ConditionFlag.Mounting71]
        || Svc.Condition[ConditionFlag.BetweenAreas]
        || Svc.Condition[ConditionFlag.BetweenAreas51]
        || Svc.Condition[ConditionFlag.OccupiedInCutSceneEvent]
        || Svc.Condition[ConditionFlag.WatchingCutscene]
        || Svc.Condition[ConditionFlag.WatchingCutscene78];

    // OccupiedInQuestEvent/OccupiedInEvent are deliberately excluded: an NPC conversation that
    // never advances (e.g. TextAdvance disabled) latches them forever, and that wedge is the exact
    // case the stuck timeout exists to break. Only self-terminating activity counts as progress.
    internal static bool IsActivelyBusy()
        => IsPositionFrozenLegit()
        || Svc.Condition[ConditionFlag.InCombat]
        || Svc.Condition[ConditionFlag.Gathering]
        || Svc.Condition[ConditionFlag.Crafting]
        || Svc.Condition[ConditionFlag.ExecutingCraftingAction]
        || Svc.Condition[ConditionFlag.PreparingToCraft];

    internal Func<bool> IdleStallAbort(int timeoutMs)
    {
        Vector3? anchor = null;
        var idleSinceMs = Environment.TickCount64;
        return () =>
        {
            var player = Svc.Objects.LocalPlayer;
            if (player is null) return false;
            var now = Environment.TickCount64;
            var pos = player.Position;
            if (anchor is null
             || Vector3.Distance(anchor.Value, pos) > StuckMoveThresholdMeters
             || NavmeshIPC.Instance.IsBusy()
             || IsPositionFrozenLegit())
            {
                anchor = pos;
                idleSinceMs = now;
                return false;
            }
            return now - idleSinceMs >= timeoutMs;
        };
    }

    internal async Task<bool> TeleportToTerritory(uint territoryId, Vector3 dest, string label, int perAttemptTimeoutMs, int attempts = 4)
    {
        for (var i = 1; i <= attempts && !CancelToken.IsCancellationRequested; i++)
        {
            if (Svc.ClientState.TerritoryType == territoryId) return true;
            var op = new MoveOp(o => o.Teleport(territoryId, dest, allowSameZoneTeleport: false));
            await RunCancellable(op, perAttemptTimeoutMs, $"{label}#{i}", IdleStallAbort(IdleStallTimeoutMs));
            if (Svc.ClientState.TerritoryType == territoryId) return true;
            if (op.Fault is not null) Diag($"{label}#{i} teleport faulted: {op.Fault.Message}");
            await NextFrame(120);
        }
        return Svc.ClientState.TerritoryType == territoryId;
    }

    internal async Task<bool> RunCancellable(MoveOp op, int timeoutMs, string label, Func<bool>? abortIf = null)
    {
        var tcs = new TaskCompletionSource();
        op.Run(() => tcs.TrySetResult());
        var work = tcs.Task;

        using var reg = CancelToken.Register(() => TryCancel(op, label));

        var deadline = Environment.TickCount64 + timeoutMs;
        var aborted = false;
        while (!work.IsCompleted && Environment.TickCount64 < deadline)
        {
            if (CancelToken.IsCancellationRequested) break;
            if (abortIf is not null)
            {
                bool trip;
                try { trip = abortIf(); }
                catch (Exception ex) { Diag($"RunCancellable '{label}' abortIf threw: {ex.Message}"); trip = false; }
                if (trip) { aborted = true; break; }
            }
            await NextFrame(4);
        }

        if (work.IsCompleted) return true;

        if (CancelToken.IsCancellationRequested)
        {
            TryCancel(op, label);
            return false;
        }

        Diag(aborted
            ? $"RunCancellable '{label}' aborting sub-task (abort condition met)"
            : $"WATCHDOG: '{label}' exceeded {timeoutMs / 1000}s; cancelling sub-task");
        TryCancel(op, label);

        var grace = Environment.TickCount64 + SubTaskUnwindGraceMs;
        while (!work.IsCompleted && Environment.TickCount64 < grace)
            await NextFrame(4);

        if (!work.IsCompleted)
            Diag($"WATCHDOG: '{label}' did not unwind {SubTaskUnwindGraceMs / 1000}s after Cancel (unexpected)");

        return false;
    }

    private void TryCancel(MoveOp op, string label)
    {
        try { op.Cancel(); }
        catch (ObjectDisposedException) { }
        catch (Exception ex) { Diag($"RunCancellable '{label}' Cancel threw: {ex.Message}"); }
    }

    protected async Task<bool> WaitUntilTimed(Func<bool> condition, int timeoutMs, string scope, int checkMs = 30)
    {
        var deadline = Environment.TickCount64 + timeoutMs;
        while (Environment.TickCount64 < deadline)
        {
            if (CancelToken.IsCancellationRequested) return false;
            bool ok;
            try { ok = condition(); }
            catch (Exception ex) { Diag($"WaitUntilTimed '{scope}' condition threw: {ex.Message}"); ok = false; }
            if (ok) return true;
            await NextFrame(checkMs);
        }
        Diag($"WAIT TIMEOUT: '{scope}' not satisfied within {timeoutMs / 1000}s");
        return false;
    }

    protected void Diag(string message) => Svc.Log.Info($"[ADT] {message}");

    internal enum StallKind { None, NavWedge, Idle }

    internal sealed class TravelStuckTracker
    {
        private Vector3? lastPos;
        private long navWedgeSinceMs = Environment.TickCount64;
        private long idleSinceMs = Environment.TickCount64;

        public StallKind Check()
        {
            var player = Svc.Objects.LocalPlayer;
            if (player is null) return StallKind.None;

            var now = Environment.TickCount64;
            var pos = player.Position;

            if (lastPos is null || Vector3.Distance(lastPos.Value, pos) > StuckMoveThresholdMeters)
            {
                lastPos = pos;
                navWedgeSinceMs = now;
                idleSinceMs = now;
                return StallKind.None;
            }

            var legitFrozen = IsPositionFrozenLegit();
            var navRunning = NavmeshIPC.Instance.IsRunning();
            var navBusy = NavmeshIPC.Instance.IsBusy();

            if (legitFrozen || !navRunning) navWedgeSinceMs = now;
            else if (now - navWedgeSinceMs >= HardStuckTimeoutMs) return StallKind.NavWedge;

            if (legitFrozen || navBusy) idleSinceMs = now;
            else if (now - idleSinceMs >= IdleStallTimeoutMs) return StallKind.Idle;

            return StallKind.None;
        }
    }
}
