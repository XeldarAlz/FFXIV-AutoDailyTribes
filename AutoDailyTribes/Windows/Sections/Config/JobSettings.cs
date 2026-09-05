using AutoDailyTribes.Windows.Components;

namespace AutoDailyTribes.Windows.Sections.Config;

internal static class JobSettings
{
    private readonly record struct Job(uint Id, string Label);

    private sealed class KindSection
    {
        public required string Title;
        public required string Scope;
        public required string Help;
        public required string ChoiceId;
        public required string JobId;
        public required string SectionId;
        public required Job[] Jobs;
        public required string[] JobLabels;
        public string? Footnote;
    }

    private const string JobRow = "Job to use";
    private const string SpecificRow = "Which job";
    private const string SpecificHelp = "The gearset for this job is equipped before the tribe's dailies are accepted.";

    private static readonly JobChoice[] ChoiceOrder = [JobChoice.Current, JobChoice.HighestXP, JobChoice.LowestXP, JobChoice.Specific];
    private static readonly string[] ChoiceNames = ["Currently equipped", "Highest level", "Lowest level", "Specific job"];
    private static readonly string[] ChoiceDetails =
    [
        "Keeps whatever job of this type you are wearing when the run starts.",
        "Switches to the gearset of your highest-level job of this type.",
        "Switches to the gearset of your lowest-level job of this type, handy for levelling.",
        "Always switches to the job picked below.",
    ];

    private static readonly KindSection Crafter = new()
    {
        Title = "Crafter tribes",
        Scope = "Ixal · Moogles · Dwarves · Loporrits · Yok Huy",
        Help = "Which Disciple of the Hand job runs crafter tribe dailies.",
        ChoiceId = "##adt_job_crafter",
        JobId = "##adt_job_crafter_pick",
        SectionId = "##adt_job_crafter_section",
        Jobs =
        [
            new(8,  "Carpenter (CRP)"),
            new(9,  "Blacksmith (BSM)"),
            new(10, "Armorer (ARM)"),
            new(11, "Goldsmith (GSM)"),
            new(12, "Leatherworker (LTW)"),
            new(13, "Weaver (WVR)"),
            new(14, "Alchemist (ALC)"),
            new(15, "Culinarian (CUL)"),
        ],
        JobLabels = [],
    };

    // Fisher is intentionally omitted: Questionable has no fishing support, so fisher dailies
    // cannot be automated and gatherer tribes run on Miner or Botanist only.
    private static readonly KindSection Gatherer = new()
    {
        Title = "Gatherer tribes",
        Scope = "Qitari · Omicron · Mamool Ja",
        Help = "Which Disciple of the Land job runs gatherer tribe dailies.",
        ChoiceId = "##adt_job_gatherer",
        JobId = "##adt_job_gatherer_pick",
        SectionId = "##adt_job_gatherer_section",
        Jobs =
        [
            new(16, "Miner (MIN)"),
            new(17, "Botanist (BTN)"),
        ],
        JobLabels = [],
        Footnote = "Fisher is excluded. Questionable cannot automate fishing, so gatherer tribes run on Miner or Botanist and any fishing daily is skipped.",
    };

    private static readonly KindSection Combat = new()
    {
        Title = "Combat tribes",
        Scope = "Amalj'aa · Sylphs · Kobolds · Sahagin · Vanu Vanu · Vath · Kojin · Ananta · Pixies · Arkasodara · Pelupelu",
        Help = "Which combat job runs battle tribe dailies.",
        ChoiceId = "##adt_job_combat",
        JobId = "##adt_job_combat_pick",
        SectionId = "##adt_job_combat_section",
        Jobs =
        [
            new(19, "Paladin (PLD)"),
            new(20, "Monk (MNK)"),
            new(21, "Warrior (WAR)"),
            new(22, "Dragoon (DRG)"),
            new(23, "Bard (BRD)"),
            new(24, "White Mage (WHM)"),
            new(25, "Black Mage (BLM)"),
            new(27, "Summoner (SMN)"),
            new(28, "Scholar (SCH)"),
            new(30, "Ninja (NIN)"),
            new(31, "Machinist (MCH)"),
            new(32, "Dark Knight (DRK)"),
            new(33, "Astrologian (AST)"),
            new(34, "Samurai (SAM)"),
            new(35, "Red Mage (RDM)"),
            new(36, "Blue Mage (BLU)"),
            new(37, "Gunbreaker (GNB)"),
            new(38, "Dancer (DNC)"),
            new(39, "Reaper (RPR)"),
            new(40, "Sage (SGE)"),
            new(41, "Viper (VPR)"),
            new(42, "Pictomancer (PCT)"),
            new(43, "Beastmaster (BST)"),
        ],
        JobLabels = [],
    };

    static JobSettings()
    {
        Crafter.JobLabels = Labels(Crafter.Jobs);
        Gatherer.JobLabels = Labels(Gatherer.Jobs);
        Combat.JobLabels = Labels(Combat.Jobs);
    }

    public static void Draw(Configuration cfg)
    {
        DrawSection(cfg, Crafter, cfg.CrafterJobType, cfg.SelectedCrafterJob,
            choice => cfg.CrafterJobType = choice, job => cfg.SelectedCrafterJob = job);
        DrawSection(cfg, Gatherer, cfg.GathererJobType, cfg.SelectedGathererJob,
            choice => cfg.GathererJobType = choice, job => cfg.SelectedGathererJob = job);
        DrawSection(cfg, Combat, cfg.CombatJobType, cfg.SelectedCombatJob,
            choice => cfg.CombatJobType = choice, job => cfg.SelectedCombatJob = job);
    }

    private static void DrawSection(Configuration cfg, KindSection section, JobChoice current, uint currentJob,
        Action<JobChoice> setChoice, Action<uint> setJob)
    {
        using (SettingsGroup.Begin(section.Title))
        {
            var selected = Math.Max(0, Array.IndexOf(ChoiceOrder, current));
            SettingsRow.Draw(JobRow, section.Help, SettingsControls.RowComboWidth,
                () => SettingsControls.DrawChoices(section.ChoiceId, ChoiceNames, ChoiceDetails, selected, choice =>
                {
                    setChoice(ChoiceOrder[choice]);
                    cfg.SaveDebounced();
                }));
            SettingsRow.Caption(section.Scope);

            using var specific = Motion.PushSection(section.SectionId, current == JobChoice.Specific);
            if (specific is not null) DrawJobPicker(cfg, section, currentJob, setJob);
        }

        if (section.Footnote is not null) SettingsGroup.Footnote(section.Footnote);
    }

    private static void DrawJobPicker(Configuration cfg, KindSection section, uint currentJob, Action<uint> setJob)
    {
        var selected = 0;
        for (var index = 0; index < section.Jobs.Length; index++)
        {
            if (section.Jobs[index].Id == currentJob) selected = index;
        }

        SettingsRow.Draw(SpecificRow, SpecificHelp, SettingsControls.RowComboWidth, () =>
        {
            var picked = selected;
            if (!Dropdown.Draw(section.JobId, section.JobLabels, ref picked, SettingsControls.RowComboWidth,
                    SettingsControls.RowComboWidth, searchable: section.Jobs.Length > 8)) return;

            setJob(section.Jobs[picked].Id);
            cfg.SaveDebounced();
        });
    }

    private static string[] Labels(Job[] jobs)
    {
        var labels = new string[jobs.Length];
        for (var index = 0; index < jobs.Length; index++) labels[index] = jobs[index].Label;
        return labels;
    }
}
