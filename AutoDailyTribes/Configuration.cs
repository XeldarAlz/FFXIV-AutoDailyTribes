using Dalamud.Configuration;
using ECommons.Throttlers;

namespace AutoDailyTribes;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public bool AutoShowIfDailiesAvailable { get; set; } = true;

    public JobChoice CrafterJobType { get; set; } = JobChoice.HighestXP;
    public uint SelectedCrafterJob { get; set; } = 8;  // CRP

    public JobChoice GathererJobType { get; set; } = JobChoice.HighestXP;
    public uint SelectedGathererJob { get; set; } = 16; // MIN

    public HashSet<uint> SelectedTribes { get; set; } = [];

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);

    // Slider/drag callbacks fire every frame; debounce so we don't hammer disk.
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
