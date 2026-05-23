using AutoTribeQuests.Core.Tribes;
using clib.Services;

namespace AutoTribeQuests.Core.Tasks;

// Thin orchestrator above clib's Automation. Provides high-level methods
// the UI binds to: "run this tribe" / "run all unlocked tribes".
//
// Automation itself is single-slot — Start() cancels the current task. Use
// the queue parameter to chain multiple tribes back-to-back.
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
