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
    // Idle = no movement while NOTHING legitimate is happening (no vnav, no pathfind, no cast/mount/zone
    // transition). That is a wedged op — typically a clib teleport that was issued but never started
    // casting. Long enough that the ~1-2s gap before a real teleport's cast can't trip it.
    internal  const int   IdleStallTimeoutMs = 8_000;

    protected async Task WaitUntilSkipTalk(Func<bool> condition, string scopeName)
    {
        using var scope = BeginScope(scopeName);
        while (!condition())
        {
            if (AddonProbes.TalkActive())
            {
                Log("progressing talk...");
                AddonInteractions.ProgressTalk();
            }
            await NextFrame();
        }
    }

    protected static string QuestName(uint questId)
        => Svc.Data.GetExcelSheet<Lumina.Excel.Sheets.Quest>()?.GetRowOrDefault(questId)?.Name.ToString() ?? questId.ToString();

    // Stationary-but-legitimate states. Excludes Mounted (a mount snagged on terrain is a real freeze)
    // but includes Mounting (the summon holds the character still for ~1-2s).
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

    // A reusable abort predicate: trips when the player makes no physical progress while nothing
    // legitimate is in progress — no vnav follow/pathfind, no cast/mount/zone-transition. That is a clib
    // op (usually a teleport) that accepted its command but never started. Returns a fresh stateful closure.
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

    // Resilient cross-zone teleport. clib's TeleportTo can accept the teleport yet never start casting
    // (a brief post-event teleport lock), then spin until a watchdog fires. IdleStallAbort catches that in
    // ~8s; we retry with a short backoff so the lock can clear, instead of failing on one slow timeout.
    internal async Task<bool> TeleportToTerritory(uint territoryId, Vector3 dest, string label, int perAttemptTimeoutMs, int attempts = 4)
    {
        for (var i = 1; i <= attempts && !CancelToken.IsCancellationRequested; i++)
        {
            if (Svc.ClientState.TerritoryType == territoryId) return true;
            var op = new MoveOp(o => o.Teleport(territoryId, dest, allowSameZoneTeleport: false));
            await RunCancellable(op, perAttemptTimeoutMs, $"{label}#{i}", IdleStallAbort(IdleStallTimeoutMs));
            if (Svc.ClientState.TerritoryType == territoryId) return true;
            if (op.Fault is not null) Diag($"{label}#{i} teleport faulted: {op.Fault.Message}");
            await NextFrame(120); // brief backoff so a transient teleport lock can clear before the retry
        }
        return Svc.ClientState.TerritoryType == territoryId;
    }

    // Run a single clib operation as its own cancellable AutoTask and bound it with a wall-clock cap. On
    // timeout — or when abortIf trips — it calls op.Cancel(), which cancels every await inside the op and
    // fires its registered cleanups (movement off, the MoveTo OnDispose stops vnav). The op genuinely
    // unwinds, so it can never linger and fight the next move/teleport. Returns true if the op finished on
    // its own, false if it had to be cancelled (timeout, abort, or run cancellation).
    internal async Task<bool> RunCancellable(MoveOp op, int timeoutMs, string label, Func<bool>? abortIf = null)
    {
        var tcs = new TaskCompletionSource();
        op.Run(() => tcs.TrySetResult());
        var work = tcs.Task;

        // The op runs on its own CancellationTokenSource, so cancelling the whole run (Stop) would not
        // reach it. Bind them: if our token fires, cancel the op too, so a Stop tears everything down.
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
        // The op can complete (and dispose its CTS) between our IsCompleted check and here; Cancel on a
        // disposed CTS throws. Swallow it — a completed op needs no cancelling.
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

    // Watches a single move for lack of forward progress. Partitions every non-progress case a clib
    // MoveTo can land in, so no phase is blind:
    //   • NavWedge — vnav actively following a path yet the character hasn't moved (terrain snag).
    //   • Idle     — no movement while NOTHING legitimate is happening: not following, not pathfinding,
    //                not casting/mounting/zone-transitioning (a wedged pre-pathfind phase, almost always
    //                a clib teleport that was issued but never started casting).
    // Anything legitimate (movement, vnav busy, a frozen-legit state) resets the matching timer.
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

            // lastPos is a PERSISTENT anchor, advanced only once we've actually displaced past the
            // threshold — NOT every poll. Resetting it each poll made steady travel look stationary,
            // because <1.5m moves between polls never cleared the threshold, false-firing the wedge timer.
            if (lastPos is null || Vector3.Distance(lastPos.Value, pos) > StuckMoveThresholdMeters)
            {
                lastPos = pos;
                navWedgeSinceMs = now;
                idleSinceMs = now;
                return StallKind.None;
            }

            var legitFrozen = IsPositionFrozenLegit();
            var navRunning = NavmeshIPC.Instance.IsRunning();
            var navBusy = NavmeshIPC.Instance.IsBusy(); // running OR pathfind-in-progress

            if (legitFrozen || !navRunning) navWedgeSinceMs = now;
            else if (now - navWedgeSinceMs >= HardStuckTimeoutMs) return StallKind.NavWedge;

            if (legitFrozen || navBusy) idleSinceMs = now;
            else if (now - idleSinceMs >= IdleStallTimeoutMs) return StallKind.Idle;

            return StallKind.None;
        }
    }
}
