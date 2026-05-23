using Dalamud.Bindings.ImGui;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AutoTribeQuests;

public sealed class Config
{
    public enum JobChoice { Specific, Current, LowestXP, HighestXP }

    public event Action? Modified;

    public bool AutoShowIfDailiesAvailable { get; set; } = true;
    public bool StopAtAllowanceCap { get; set; } = true;
    public bool ShowDebugUI { get; set; } = false;

    public JobChoice CrafterJobType { get; set; } = JobChoice.HighestXP;
    public uint SelectedCrafterJob { get; set; } = 8; // CRP

    public HashSet<uint> DisabledTribes { get; set; } = [];

    private const int CurrentVersion = 1;
    public int Version { get; set; } = CurrentVersion;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public void NotifyChange() => Modified?.Invoke();

    public void Load(FileInfo file)
    {
        if (!file.Exists) return;
        try
        {
            var json = File.ReadAllText(file.FullName);
            var loaded = JsonSerializer.Deserialize<Config>(json, JsonOpts);
            if (loaded == null) return;
            AutoShowIfDailiesAvailable = loaded.AutoShowIfDailiesAvailable;
            StopAtAllowanceCap = loaded.StopAtAllowanceCap;
            ShowDebugUI = loaded.ShowDebugUI;
            CrafterJobType = loaded.CrafterJobType;
            SelectedCrafterJob = loaded.SelectedCrafterJob;
            DisabledTribes = loaded.DisabledTribes ?? [];
            Version = loaded.Version;
        }
        catch (Exception ex)
        {
            Service.Log.Warning($"Failed to load config: {ex}");
        }
    }

    public void Save(FileInfo file)
    {
        try
        {
            var json = JsonSerializer.Serialize(this, JsonOpts);
            File.WriteAllText(file.FullName, json);
        }
        catch (Exception ex)
        {
            Service.Log.Warning($"Failed to save config: {ex}");
        }
    }

    public void Draw()
    {
        var changed = false;
        var b = AutoShowIfDailiesAvailable;
        if (ImGui.Checkbox("Auto-open window when dailies are available", ref b)) { AutoShowIfDailiesAvailable = b; changed = true; }
        b = StopAtAllowanceCap;
        if (ImGui.Checkbox("Stop when 12/day cap is reached", ref b)) { StopAtAllowanceCap = b; changed = true; }
        b = ShowDebugUI;
        if (ImGui.Checkbox("Show debug UI", ref b)) { ShowDebugUI = b; changed = true; }

        ImGui.Separator();
        ImGui.TextUnformatted("Crafter tribes:");
        // TODO: enum combo for CrafterJobType + class job picker mirroring vsatisfy/Config.cs
        if (changed) NotifyChange();
    }
}
