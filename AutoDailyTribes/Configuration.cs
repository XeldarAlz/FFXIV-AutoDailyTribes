using Dalamud.Configuration;
using ECommons.Throttlers;

namespace AutoDailyTribes;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public bool AutoShowIfDailiesAvailable { get; set; } = true;

    public JobChoice CrafterJobType { get; set; } = JobChoice.HighestXP;
    public uint SelectedCrafterJob { get; set; } = 8;

    public JobChoice GathererJobType { get; set; } = JobChoice.HighestXP;
    public uint SelectedGathererJob { get; set; } = 16;

    public JobChoice CombatJobType { get; set; } = JobChoice.Current;
    public uint SelectedCombatJob { get; set; } = 19;

    public List<uint> SelectedTribes { get; set; } = [];

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);

    public void SaveDebounced()
    {
        if (EzThrottler.Throttle(Core.AdtConstants.ThrottleKeys.Save, Core.AdtConstants.SaveThrottleMs))
            Save();
    }
}

public enum JobChoice
{
    Specific,
    Current,
    LowestXP,
    HighestXP,
}
