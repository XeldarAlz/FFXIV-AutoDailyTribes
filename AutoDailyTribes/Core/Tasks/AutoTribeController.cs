using AutoDailyTribes.Core.External;
using AutoDailyTribes.Core.Tribes;
using clib.Services;
using System.Threading;

namespace AutoDailyTribes.Core.Tasks;

internal sealed class AutoTribeController
{
    private int runGeneration;
    private readonly TribeRunProgress progress = new();

    public bool Running => Svc.Automation.Running;
    public string Status => Svc.Automation.CurrentTask?.Status ?? "Idle";
    public TribeRunProgress Progress => progress;

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

        var list = tribes.ToList();
        if (list.Count == 0) return;

        var generation = Interlocked.Increment(ref runGeneration);
        progress.Begin(list);
        Diag($"RunAll: {list.Count} tribe(s) queued.");
        StartNext(new Queue<TribeInfo>(list), generation);
    }

    public void Stop()
    {
        Interlocked.Increment(ref runGeneration);
        Svc.Automation.Stop();
        progress.End();
    }

    private void StartNext(Queue<TribeInfo> queue, int generation)
    {
        if (Volatile.Read(ref runGeneration) != generation)
        {
            // A newer run (or Stop) bumped the generation and now owns `progress`; leave it alone.
            Diag("RunAll: generation changed (Stop or new run); not continuing the batch.");
            return;
        }
        if (queue.Count == 0)
        {
            Diag("RunAll: all queued tribes processed.");
            progress.End();
            return;
        }

        var tribe = queue.Dequeue();
        Diag($"RunAll: starting {tribe.Name} ({queue.Count} remaining after this).");
        Svc.Automation.Start(new AutoTribe(tribe, progress), OnCompleted: () =>
        {
            if (Volatile.Read(ref runGeneration) != generation) return;
            progress.CompleteTribe();
            StartNext(queue, generation);
        });
    }
}
