using AutoDailyTribes.Core.Tribes;
using clib.Services;

namespace AutoDailyTribes.Core.Tasks;

internal sealed class AutoTribeController
{
    public bool Running => Svc.Automation.Running;
    public string Status => Svc.Automation.CurrentTask?.Status ?? "Idle";

    public void Run(TribeInfo tribe)
        => Svc.Automation.Start(new AutoTribe(tribe));

    public void RunAll(IEnumerable<TribeInfo> tribes)
    {
        bool first = true;
        foreach (var t in tribes)
        {
            Svc.Automation.Start(new AutoTribe(t), queue: !first);
            first = false;
        }
    }

    public void Stop() => Svc.Automation.Stop();
}
