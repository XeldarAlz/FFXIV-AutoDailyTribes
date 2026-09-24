using AutoDailyTribes.Core.Changelog;
using AutoDailyTribes.Core.Tribes;
using Dalamud.Configuration;
using ECommons.Throttlers;

namespace AutoDailyTribes;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    // Empty means "not chosen yet"; the plugin detects one from Dalamud, the game client or the OS
    // on first run and writes it back.
    public string Language { get; set; } = "";

    public bool AutoShowIfDailiesAvailable { get; set; } = true;

    public JobChoice CrafterJobType { get; set; } = JobChoice.HighestXP;
    public uint SelectedCrafterJob { get; set; } = 8;

    public JobChoice GathererJobType { get; set; } = JobChoice.HighestXP;
    public uint SelectedGathererJob { get; set; } = 16;

    public JobChoice CombatJobType { get; set; } = JobChoice.Current;
    public uint SelectedCombatJob { get; set; } = 19;

    public List<uint> SelectedTribes { get; set; } = [];

    public List<TribeKind> HiddenKinds { get; set; } = [];

    public bool HideMaxedTribes { get; set; }

    public TurnInMode TurnInMode { get; set; } = TurnInMode.EachQuest;

    // Chat commands (one per line, each starting with '/') dispatched after a batch run
    // finishes naturally — e.g. "/ays m" to hand off to AutoRetainer. See issue #17.
    public string PostRunCommands { get; set; } = string.Empty;

    // Per character+tribe rank-cycle memory (key "{contentId}:{beastTribeId}") so a plugin
    // reload mid-day doesn't forget that a rank-up already refreshed the daily offers.
    public Dictionary<string, TribeCycleState> RankCycles { get; set; } = [];

    public string LastSeenChangelogVersion { get; set; } = string.Empty;

    [Newtonsoft.Json.JsonIgnore]
    public bool HasUnseenChangelog => !string.Equals(LastSeenChangelogVersion, ChangelogData.LatestVersion, StringComparison.Ordinal);

    public void MarkChangelogSeen()
    {
        if (!HasUnseenChangelog)
        {
            return;
        }

        LastSeenChangelogVersion = ChangelogData.LatestVersion;
        Save();
    }

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);

    public void SaveDebounced()
    {
        if (EzThrottler.Throttle(Core.AdtConstants.ThrottleKeys.Save, Core.AdtConstants.SaveThrottleMs))
            Save();
    }
}

[Serializable]
public sealed class TribeCycleState
{
    public int LastSeenRank { get; set; } = -1;
    public int Baseline { get; set; }
    public DateTime SavedUtc { get; set; }
}

// How a tribe's accepted dailies reach their hand-in: one at a time as each finishes, or every
// objective first and then all the hand-ins in a row, the way Questionable runs them on its own.
public enum TurnInMode
{
    EachQuest,
    AllAtOnce,
}

public enum JobChoice
{
    Specific,
    Current,
    LowestXP,
    HighestXP,
}
