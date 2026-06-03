using AutoDailyTribes.Core.External;
using AutoDailyTribes.Core.Tribes;
using clib.Services;
using System.Threading;

namespace AutoDailyTribes.Core.Tasks;

internal sealed class AutoTribeController
{
    private int runGeneration;

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

    public void RunAll(IEnumerable<TribeInfo> tribes)
    {
        if (!RequiredPluginsReady()) return;

        var queue = new Queue<TribeInfo>(tribes);
        if (queue.Count == 0) return;

        var generation = Interlocked.Increment(ref runGeneration);
        Diag($"RunAll: {queue.Count} tribe(s) queued.");
        StartNext(queue, generation);
    }

    public void Stop()
    {
        Interlocked.Increment(ref runGeneration);
        Svc.Automation.Stop();
    }

    private void StartNext(Queue<TribeInfo> queue, int generation)
    {
        if (Volatile.Read(ref runGeneration) != generation)
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
            if (Volatile.Read(ref runGeneration) != generation) return;
            StartNext(queue, generation);
        });
    }
}
