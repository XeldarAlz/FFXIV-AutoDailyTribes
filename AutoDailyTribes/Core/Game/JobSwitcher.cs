using AutoDailyTribes.Core.Tribes;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace AutoDailyTribes.Core.Game;

internal static unsafe class JobSwitcher
{
    private const int MaxGearsets = 100;

    private const byte NoClassJob = 0;

    private const byte GladiatorId   = 1;
    private const byte PugilistId    = 2;
    private const byte MarauderId    = 3;
    private const byte LancerId      = 4;
    private const byte ArcherId      = 5;
    private const byte ConjurerId    = 6;
    private const byte ThaumaturgeId = 7;
    private const byte ArcanistId    = 26;
    private const byte RogueId       = 29;

    private const byte PaladinId   = 19;
    private const byte MonkId      = 20;
    private const byte WarriorId   = 21;
    private const byte DragoonId   = 22;
    private const byte BardId      = 23;
    private const byte WhiteMageId = 24;
    private const byte BlackMageId = 25;
    private const byte SummonerId  = 27;
    private const byte ScholarId   = 28;
    private const byte NinjaId     = 30;

    private const byte DohFirst = 8;
    private const byte DohLast  = 15;
    private const byte DolFirst = 16;
    private const byte DolLast  = 18;
    private const byte MinerId    = 16;
    private const byte BotanistId = 17;
    private const byte MaxClassJobId = 43; // Beastmaster — highest ClassJob id (incl. limited jobs)

    public static bool IsCrafter(byte job) => job >= DohFirst && job <= DohLast;
    public static bool IsGatherer(byte job) => job >= DolFirst && job <= DolLast;

    // Questionable has no fishing support, so Fisher (18) is never an automatable gathering target.
    public static bool IsAutoGatherer(byte job) => job == MinerId || job == BotanistId;

    public static bool IsCombat(byte job) => job > 0 && !IsCrafter(job) && !IsGatherer(job) && job <= MaxClassJobId;

    // A gearset stores the ClassJob it resolves to, so a job gearset saved without its soul
    // crystal reads as the base class and equipping it takes the crystal off. A class and its
    // job always share one level, so those two gearsets tie on level and the class would win on
    // gearset order alone — hence the explicit preference in IsBetterPick.
    public static bool IsBaseClass(byte job)
        => job is GladiatorId or PugilistId or MarauderId or LancerId or ArcherId
               or ConjurerId or ThaumaturgeId or ArcanistId or RogueId;

    private static byte BaseClassOf(byte job) => job switch
    {
        PaladinId               => GladiatorId,
        MonkId                  => PugilistId,
        WarriorId               => MarauderId,
        DragoonId               => LancerId,
        BardId                  => ArcherId,
        WhiteMageId             => ConjurerId,
        BlackMageId             => ThaumaturgeId,
        SummonerId or ScholarId => ArcanistId,
        NinjaId                 => RogueId,
        _                       => NoClassJob,
    };

    public static byte CurrentClassJob()
    {
        var playerState = PlayerState.Instance();
        return playerState == null ? NoClassJob : playerState->CurrentClassJobId;
    }

    // shouldGetSynced defaults to true, which reports the synced level while the player is
    // level-synced and would rank jobs against a level nobody actually has.
    public static int JobLevel(byte job)
    {
        var playerState = PlayerState.Instance();
        return playerState == null ? 0 : playerState->GetClassJobLevel(job, shouldGetSynced: false);
    }

    public static string JobName(byte job)
        => Svc.Data.GetExcelSheet<Lumina.Excel.Sheets.ClassJob>()?.GetRowOrDefault(job)?.Abbreviation.ToString()
           ?? job.ToString();

    public static bool InCategory(byte job, TribeKind kind) => kind switch
    {
        TribeKind.Crafter  => IsCrafter(job),
        TribeKind.Gatherer => IsAutoGatherer(job),
        TribeKind.Mixed    => IsCrafter(job) || IsAutoGatherer(job),
        TribeKind.Combat   => IsCombat(job),
        _                  => false,
    };

    public static bool CurrentJobSatisfies(TribeInfo tribe, Configuration cfg)
    {
        var current = CurrentClassJob();
        return InCategory(current, tribe.Kind)
            && JobLevel(current) >= tribe.RequiredLevel
            && KeepsCurrentJob(tribe.Kind, cfg, current);
    }

    // Only the "currently equipped" choice pins the job the player is already wearing. The other
    // three are documented as switching, so an equipped in-category job must not quietly stand in
    // for the lowest-level job someone asked the run to level.
    private static bool KeepsCurrentJob(TribeKind kind, Configuration cfg, byte current) => kind switch
    {
        TribeKind.Crafter  => cfg.CrafterJobType == JobChoice.Current,
        TribeKind.Gatherer => cfg.GathererJobType == JobChoice.Current,
        TribeKind.Combat   => cfg.CombatJobType == JobChoice.Current,
        TribeKind.Mixed    => IsCrafter(current)
                                  ? cfg.CrafterJobType == JobChoice.Current
                                  : cfg.GathererJobType == JobChoice.Current,
        _                  => false,
    };

    public static byte GearsetClassJob(int gearsetId)
    {
        var gearsetModule = RaptureGearsetModule.Instance();
        if (gearsetModule == null)
        {
            return NoClassJob;
        }

        var entry = gearsetModule->GetGearset(gearsetId);
        return entry == null ? NoClassJob : entry->ClassJob;
    }

    public static int PickGearset(TribeInfo tribe, Configuration cfg)
    {
        var required = tribe.RequiredLevel;
        switch (tribe.Kind)
        {
            case TribeKind.Crafter:
                return PickFromCategory(cfg.CrafterJobType, cfg.SelectedCrafterJob, IsCrafter, required);
            case TribeKind.Gatherer:
                return PickFromCategory(cfg.GathererJobType, cfg.SelectedGathererJob, IsAutoGatherer, required);
            case TribeKind.Mixed:
                // Namazu dailies take a crafter or a gatherer, so the crafter preference only holds
                // while a crafter can actually accept them — otherwise a levelled gatherer wins.
                var crafter = PickFromCategory(cfg.CrafterJobType, cfg.SelectedCrafterJob, IsCrafter, required);
                if (MeetsRequirement(crafter, required))
                {
                    return crafter;
                }

                var gatherer = PickFromCategory(cfg.GathererJobType, cfg.SelectedGathererJob, IsAutoGatherer, required);
                if (MeetsRequirement(gatherer, required))
                {
                    return gatherer;
                }
                return crafter >= 0 ? crafter : gatherer;
            case TribeKind.Combat:
                return PickFromCategory(cfg.CombatJobType, cfg.SelectedCombatJob, IsCombat, required);
            default:
                return -1;
        }
    }

    public static bool EquipGearset(int gearsetId)
    {
        var gearsetModule = RaptureGearsetModule.Instance();
        if (gearsetModule == null)
        {
            return false;
        }

        return gearsetModule->EquipGearset(gearsetId, 0) == 0;  // 0 = success
    }

    private static bool MeetsRequirement(int gearsetId, int requiredLevel)
        => gearsetId >= 0 && JobLevel(GearsetClassJob(gearsetId)) >= requiredLevel;

    private static int PickFromCategory(JobChoice mode, uint specificJob, Func<byte, bool> inCategory, int requiredLevel)
    {
        // Honor a specific job only if it is automatable for this category — guards against
        // e.g. Specific=Fisher, which Questionable cannot complete, falling through to MIN/BTN.
        // An under-levelled specific job is honored too: the run then names it in the skip
        // message rather than silently swapping to a job the player never asked for.
        if (mode == JobChoice.Specific && inCategory((byte)specificJob))
        {
            var exact = FindGearsetForJob((byte)specificJob);
            if (exact >= 0)
            {
                return exact;
            }

            // The picker only offers jobs, so a gearset saved without its soul crystal never
            // matches by job id; fall back to that job's class before giving up on the choice.
            var baseClass = BaseClassOf((byte)specificJob);
            if (baseClass != NoClassJob)
            {
                var crystalless = FindGearsetForJob(baseClass);
                if (crystalless >= 0)
                {
                    return crystalless;
                }
            }
        }

        var highest = mode != JobChoice.LowestXP;
        return PickGearsetByLevel(inCategory, highest, requiredLevel);
    }

    private static int PickGearsetByLevel(Func<byte, bool> inCategory, bool highest, int requiredLevel)
    {
        var gearsetModule = RaptureGearsetModule.Instance();
        if (gearsetModule == null || PlayerState.Instance() == null)
        {
            return -1;
        }

        var best = -1;
        var bestLevel = 0;
        var bestIsBaseClass = false;
        var bestMeetsRequirement = false;
        for (var gearsetIndex = 0; gearsetIndex < MaxGearsets; gearsetIndex++)
        {
            if (!gearsetModule->IsValidGearset(gearsetIndex))
            {
                continue;
            }

            var entry = gearsetModule->GetGearset(gearsetIndex);
            if (entry == null)
            {
                continue;
            }

            var job = entry->ClassJob;
            if (!inCategory(job))
            {
                continue;
            }

            var level = JobLevel(job);
            var isBaseClass = IsBaseClass(job);
            var meetsRequirement = level >= requiredLevel;
            if (best >= 0 && !IsBetterPick(level, isBaseClass, meetsRequirement,
                                           bestLevel, bestIsBaseClass, bestMeetsRequirement, highest))
            {
                continue;
            }

            best = gearsetIndex;
            bestLevel = level;
            bestIsBaseClass = isBaseClass;
            bestMeetsRequirement = meetsRequirement;
        }
        return best;
    }

    // Clearing the tribe's level requirement outranks the XP preference: a job that cannot accept
    // the dailies is worth no XP at all. When nothing clears it the highest job wins either way,
    // so the run can name the closest one the player has.
    private static bool IsBetterPick(int level, bool isBaseClass, bool meetsRequirement,
                                     int bestLevel, bool bestIsBaseClass, bool bestMeetsRequirement, bool highest)
    {
        if (meetsRequirement != bestMeetsRequirement)
        {
            return meetsRequirement;
        }

        if (level != bestLevel)
        {
            return highest || !meetsRequirement ? level > bestLevel : level < bestLevel;
        }
        return bestIsBaseClass && !isBaseClass;
    }

    private static int FindGearsetForJob(byte classJobId)
    {
        var gearsetModule = RaptureGearsetModule.Instance();
        if (gearsetModule == null)
        {
            return -1;
        }

        for (var gearsetIndex = 0; gearsetIndex < MaxGearsets; gearsetIndex++)
        {
            if (!gearsetModule->IsValidGearset(gearsetIndex))
            {
                continue;
            }

            var entry = gearsetModule->GetGearset(gearsetIndex);
            if (entry != null && entry->ClassJob == classJobId)
            {
                return gearsetIndex;
            }
        }
        return -1;
    }
}
