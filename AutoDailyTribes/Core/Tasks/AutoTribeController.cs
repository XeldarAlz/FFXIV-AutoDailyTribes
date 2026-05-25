using AutoDailyTribes.Core.Tribes;
using clib.Services;
using System.Threading;

namespace AutoDailyTribes.Core.Tasks;

internal sealed class AutoTribeController
{
    private int _runGeneration;

    public bool Running => Svc.Automation.Running;
    public string Status => Svc.Automation.CurrentTask?.Status ?? "Idle";

    public void Run(TribeInfo tribe)
    {
        Interlocked.Increment(ref _runGeneration);
        Svc.Automation.Start(new AutoTribe(tribe));
    }

    public void RunAll(IEnumerable<TribeInfo> tribes)
    {
        var queue = new Queue<TribeInfo>(tribes);
        if (queue.Count == 0) return;

        var generation = Interlocked.Increment(ref _runGeneration);
        StartNext(queue, generation);
    }

    public void Stop()
    {
        Interlocked.Increment(ref _runGeneration);
        Svc.Automation.Stop();
    }

    private void StartNext(Queue<TribeInfo> queue, int generation)
    {
        if (queue.Count == 0) return;
        var tribe = queue.Dequeue();
        Svc.Automation.Start(new AutoTribe(tribe), OnCompleted: () =>
        {
            if (Volatile.Read(ref _runGeneration) != generation) return;
            StartNext(queue, generation);
        });
    }
}
