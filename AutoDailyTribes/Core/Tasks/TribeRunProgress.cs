using AutoDailyTribes.Core.Tribes;

namespace AutoDailyTribes.Core.Tasks;

public enum TribePhase { Idle, Preparing, SwitchingJob, Traveling, Accepting, Delegating, Recovering, Done }

// Live snapshot of a batch run, written by the controller (queue progress) and the running
// AutoTribe (phase), read by the UI each frame. All three run on the framework thread, so no
// locking — mirrors AutoFateController's plain snapshot.
public sealed class TribeRunProgress
{
    public IReadOnlyList<TribeInfo> RunList { get; private set; } = [];
    public int Completed { get; private set; }
    public TribePhase Phase { get; set; } = TribePhase.Idle;

    public int Total => RunList.Count;
    public int CurrentNumber => Math.Min(Completed + 1, Math.Max(Total, 1));
    public TribeInfo? Current => Completed < Total ? RunList[Completed] : null;
    public IEnumerable<TribeInfo> UpNext => RunList.Skip(Completed + 1);
    public float Fraction => Total == 0 ? 0f : (float)Completed / Total;

    public void Begin(IReadOnlyList<TribeInfo> list)
    {
        RunList = list;
        Completed = 0;
        Phase = TribePhase.Preparing;
    }

    public void CompleteTribe()
    {
        if (Completed < Total) Completed++;
    }

    public void End()
    {
        RunList = [];
        Completed = 0;
        Phase = TribePhase.Idle;
    }
}
