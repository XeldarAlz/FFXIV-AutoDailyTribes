using AutoTribeQuests.Core.Tribes;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using System.Numerics;

namespace AutoTribeQuests.Windows;

public sealed class ConfigWindow : Window, IDisposable
{
    private readonly Plugin plugin;

    public ConfigWindow(Plugin plugin) : base("Allied Tribes — Settings###AutoTribeQuestsConfig")
    {
        this.plugin = plugin;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(380, 260),
            MaximumSize = new Vector2(620, 800),
        };
        Size = new Vector2(420, 360);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public void Dispose() { }

    public override void Draw()
    {
        var cfg = plugin.Configuration;
        using var style = Styling.PushWindowStyle();

        Styling.SectionLabel("Behavior");

        var b = cfg.AutoShowIfDailiesAvailable;
        if (ImGui.Checkbox("Open window automatically when dailies are available", ref b))
        { cfg.AutoShowIfDailiesAvailable = b; cfg.SaveDebounced(); }

        b = cfg.StopAtAllowanceCap;
        if (ImGui.Checkbox("Stop when 12/day cap is reached", ref b))
        { cfg.StopAtAllowanceCap = b; cfg.SaveDebounced(); }

        b = cfg.ShowDebugUI;
        if (ImGui.Checkbox("Show debug UI", ref b))
        { cfg.ShowDebugUI = b; cfg.SaveDebounced(); }

        ImGui.Spacing();
        Styling.SectionLabel("Tribes");
        DrawTribeToggles(cfg);

        ImGui.Spacing();
        Styling.SectionLabel("Crafter tribes");
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
            ImGui.TextWrapped("Job selection for Ixal / Moogles / Dwarves / Loporrits will land here. For now they use the highest-XP DoH job available.");
    }

    private static void DrawTribeToggles(Configuration cfg)
    {
        foreach (var era in Enum.GetValues<TribeEra>())
        {
            var tribes = TribeRegistry.ByEra(era).ToArray();
            if (tribes.Length == 0) continue;

            using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextMuted))
                ImGui.TextUnformatted(era.DisplayName());

            foreach (var tribe in tribes)
            {
                var enabled = !cfg.DisabledTribes.Contains(tribe.BeastTribeId);
                if (ImGui.Checkbox(tribe.Name + "##" + tribe.BeastTribeId, ref enabled))
                {
                    if (enabled) cfg.DisabledTribes.Remove(tribe.BeastTribeId);
                    else cfg.DisabledTribes.Add(tribe.BeastTribeId);
                    cfg.SaveDebounced();
                }
            }
        }
    }
}
