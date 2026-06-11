using AutoDailyTribes.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using System.Numerics;

namespace AutoDailyTribes.Windows;

public sealed class ConfigWindow : Window, IDisposable
{
    private static readonly (uint id, string label)[] CrafterJobs =
    [
        (8,  "Carpenter (CRP)"),
        (9,  "Blacksmith (BSM)"),
        (10, "Armorer (ARM)"),
        (11, "Goldsmith (GSM)"),
        (12, "Leatherworker (LTW)"),
        (13, "Weaver (WVR)"),
        (14, "Alchemist (ALC)"),
        (15, "Culinarian (CUL)"),
    ];

    // Fisher (18) is intentionally omitted: Questionable has no fishing support, so fisher
    // dailies can't be automated. Gatherer tribes are run on Miner/Botanist only.
    private static readonly (uint id, string label)[] GathererJobs =
    [
        (16, "Miner (MIN)"),
        (17, "Botanist (BTN)"),
    ];

    private static readonly (uint id, string label)[] CombatJobs =
    [
        (19, "Paladin (PLD)"),
        (20, "Monk (MNK)"),
        (21, "Warrior (WAR)"),
        (22, "Dragoon (DRG)"),
        (23, "Bard (BRD)"),
        (24, "White Mage (WHM)"),
        (25, "Black Mage (BLM)"),
        (27, "Summoner (SMN)"),
        (28, "Scholar (SCH)"),
        (30, "Ninja (NIN)"),
        (31, "Machinist (MCH)"),
        (32, "Dark Knight (DRK)"),
        (33, "Astrologian (AST)"),
        (34, "Samurai (SAM)"),
        (35, "Red Mage (RDM)"),
        (36, "Blue Mage (BLU)"),
        (37, "Gunbreaker (GNB)"),
        (38, "Dancer (DNC)"),
        (39, "Reaper (RPR)"),
        (40, "Sage (SGE)"),
        (41, "Viper (VPR)"),
        (42, "Pictomancer (PCT)"),
        (43, "Beastmaster (BST)"),
    ];

    private readonly Plugin plugin;

    public ConfigWindow(Plugin plugin) : base("Auto Daily Tribes — Settings###AutoDailyTribesConfig")
    {
        this.plugin = plugin;
        Flags = ImGuiWindowFlags.NoCollapse;
        Size = new Vector2(460, 480);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(420, 320),
            MaximumSize = new Vector2(680, 900),
        };
    }

    public void Dispose() { }

    public override void Draw()
    {
        var cfg = plugin.Configuration;
        using var style = Styling.PushWindowStyle();

        WindowHeader.Draw("Settings", "How Auto Daily Tribes picks a job for each tribe type, and when it pops up.");

        using (SettingsGroup.Begin("Behavior"))
            DrawBehaviorSection(cfg);

        DrawJobSection(
            cfg, FontAwesomeIcon.Hammer, Styling.KindCrafter,
            "Crafter tribes",
            "Ixal · Moogles · Dwarves · Loporrits · Yok Huy",
            "DoH",
            cfg.CrafterJobType,
            cfg.SelectedCrafterJob,
            CrafterJobs,
            type => cfg.CrafterJobType = type,
            id   => cfg.SelectedCrafterJob = id,
            footnote: null);

        DrawJobSection(
            cfg, FontAwesomeIcon.Leaf, Styling.KindGatherer,
            "Gatherer tribes",
            "Qitari · Omicron · Mamool Ja",
            "DoL",
            cfg.GathererJobType,
            cfg.SelectedGathererJob,
            GathererJobs,
            type => cfg.GathererJobType = type,
            id   => cfg.SelectedGathererJob = id,
            footnote: "Fisher is excluded — Questionable can't automate fishing, so gatherer tribes run on Miner/Botanist and any fishing daily is skipped.");

        DrawJobSection(
            cfg, FontAwesomeIcon.Shield, Styling.KindCombat,
            "Combat tribes",
            "Amalj'aa · Sylphs · Kobolds · Sahagin · Vanu Vanu · Vath · Kojin · Ananta · Pixie · Arkasodara · Pelupelu",
            "DoW/DoM",
            cfg.CombatJobType,
            cfg.SelectedCombatJob,
            CombatJobs,
            type => cfg.CombatJobType = type,
            id   => cfg.SelectedCombatJob = id,
            footnote: null);
    }

    private static void DrawBehaviorSection(Configuration cfg)
    {
        var b = cfg.AutoShowIfDailiesAvailable;
        if (ImGui.Checkbox("Open this window when dailies are available after login", ref b))
        {
            cfg.AutoShowIfDailiesAvailable = b;
            cfg.SaveDebounced();
        }
    }

    private static void DrawJobSection(
        Configuration cfg,
        FontAwesomeIcon icon,
        Vector4 accent,
        string title,
        string scope,
        string discipline,
        JobChoice currentType,
        uint currentJobId,
        (uint id, string label)[] options,
        Action<JobChoice> setType,
        Action<uint> setJob,
        string? footnote)
    {
        using (SettingsGroup.Begin(icon, title, accent))
        {
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextMuted))
                ImGui.TextUnformatted(scope);
            ImGui.Spacing();

            DrawJobModeRadio(cfg, $"Use my currently equipped {discipline} job", JobChoice.Current, currentType, setType, discipline);
            DrawJobModeRadio(cfg, $"Use highest-leveled {discipline} job",       JobChoice.HighestXP, currentType, setType, discipline);
            DrawJobModeRadio(cfg, $"Use lowest-leveled {discipline} job",        JobChoice.LowestXP, currentType, setType, discipline);

            var specific = currentType == JobChoice.Specific;
            if (ImGui.RadioButton($"Specific {discipline} job:##{discipline}_specific", specific))
            {
                setType(JobChoice.Specific);
                cfg.SaveDebounced();
            }
            ImGui.SameLine();
            DrawJobCombo(discipline, options, currentJobId, setJob, cfg, enabled: specific);

            if (footnote is not null)
            {
                ImGui.Spacing();
                using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextMuted))
                    ImGui.TextWrapped(footnote);
            }
        }
    }

    private static void DrawJobModeRadio(Configuration cfg, string label, JobChoice mode, JobChoice current, Action<JobChoice> setter, string discipline)
    {
        if (ImGui.RadioButton($"{label}##{discipline}_{mode}", current == mode))
        {
            setter(mode);
            cfg.SaveDebounced();
        }
    }

    private static void DrawJobCombo(string discipline, (uint id, string label)[] options, uint currentId, Action<uint> setter, Configuration cfg, bool enabled)
    {
        var idx = Array.FindIndex(options, o => o.id == currentId);
        if (idx < 0) idx = 0;

        using var disabled = ImRaii.Disabled(!enabled);
        ImGui.SetNextItemWidth(200);
        var labels = options.Select(o => o.label).ToArray();
        if (ImGui.Combo($"##{discipline}_combo", ref idx, labels, labels.Length))
        {
            setter(options[idx].id);
            cfg.SaveDebounced();
        }
    }
}
