using AutoDailyTribes.Core.External;
using AutoDailyTribes.Core.Tribes;
using clib.Services;
using System.Threading;

namespace AutoDailyTribes.Core.Tasks;

internal sealed class AutoTribeController
{
    private int _runGeneration;

    public bool Running => Svc.Automation.Running;
    public string Status => Svc.Automation.CurrentTask?.Status ?? "Idle";

    private static void Diag(string message) => ECommons.DalamudServices.Svc.Log.Info($"[ADT] {message}");

    private static bool RequiredPluginsReady()
    {
        if (ExternalPlugins.AllRequiredInstalled()) return true;

        var missing = string.Join(", ", ExternalPlugins.All
            .Where(p => ExternalPlugins.Catalog[p].Required && !ExternalPlugins.IsInstalled(p))
            .Select(p => ExternalPlugins.Catalog[p].DisplayName));
        Diag($"Start aborted: required plugins missing ({missing}).");
        ECommons.DalamudServices.Svc.Chat.PrintError($"[ADT] Cannot start — install all required plugins first: {missing}.");
        return false;
    }

    public void Run(TribeInfo tribe)
    {
        if (!RequiredPluginsReady()) return;
        Interlocked.Increment(ref _runGeneration);
        Svc.Automation.Start(new AutoTribe(tribe));
    }

    public void RunAll(IEnumerable<TribeInfo> tribes)
    {
        if (!RequiredPluginsReady()) return;

        var queue = new Queue<TribeInfo>(tribes);
        if (queue.Count == 0) return;

        var generation = Interlocked.Increment(ref _runGeneration);
        Diag($"RunAll: {queue.Count} tribe(s) queued.");
        StartNext(queue, generation);
    }

    public void Stop()
    {
        Interlocked.Increment(ref _runGeneration);
        Svc.Automation.Stop();
    }

    // clib invokes OnCompleted whether the tribe finished cleanly or threw, so a tribe that fails (and
    // ends its own run via the supervisor) does NOT halt the batch — we always advance to the next one.
    // The generation guard is the only thing that stops the chain: a Stop or a new run bumps it.
    private void StartNext(Queue<TribeInfo> queue, int generation)
    {
        if (Volatile.Read(ref _runGeneration) != generation)
        {
            Diag("RunAll: generation changed (Stop or new run); not continuing the batch.");
            return;
        }
        if (queue.Count == 0)
        {
            Diag("RunAll: all queued tribes processed.");
            return;
        }

        var tribe = queue.Dequeue();
        Diag($"RunAll: starting {tribe.Name} ({queue.Count} remaining after this).");
        Svc.Automation.Start(new AutoTribe(tribe), OnCompleted: () =>
        {
            if (Volatile.Read(ref _runGeneration) != generation) return;
            StartNext(queue, generation);
        });
    }
}
