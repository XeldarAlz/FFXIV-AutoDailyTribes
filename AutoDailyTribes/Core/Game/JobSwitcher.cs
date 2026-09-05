using AutoDailyTribes.Core.Tribes;
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

    public static bool CurrentJobSatisfies(TribeKind kind)
    {
        var current = CurrentClassJob();
        return kind switch
        {
            TribeKind.Crafter => IsCrafter(current),
            TribeKind.Gatherer => IsAutoGatherer(current),
            TribeKind.Mixed => IsCrafter(current) || IsAutoGatherer(current),
            TribeKind.Combat => IsCombat(current),
            _ => false,
        };
    }

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
        switch (tribe.Kind)
        {
            case TribeKind.Crafter:
                return PickFromCategory(cfg.CrafterJobType, cfg.SelectedCrafterJob, IsCrafter);
            case TribeKind.Gatherer:
                return PickFromCategory(cfg.GathererJobType, cfg.SelectedGathererJob, IsAutoGatherer);
            case TribeKind.Mixed:
                var crafter = PickFromCategory(cfg.CrafterJobType, cfg.SelectedCrafterJob, IsCrafter);
                return crafter >= 0 ? crafter : PickFromCategory(cfg.GathererJobType, cfg.SelectedGathererJob, IsAutoGatherer);
            case TribeKind.Combat:
                return PickFromCategory(cfg.CombatJobType, cfg.SelectedCombatJob, IsCombat);
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

    private static int PickFromCategory(JobChoice mode, uint specificJob, Func<byte, bool> inCategory)
    {
        // Honor a specific job only if it is automatable for this category — guards against
        // e.g. Specific=Fisher, which Questionable cannot complete, falling through to MIN/BTN.
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
        return PickGearsetByLevel(inCategory, highest);
    }

    private static int PickGearsetByLevel(Func<byte, bool> inCategory, bool highest)
    {
        var gearsetModule = RaptureGearsetModule.Instance();
        var playerState = PlayerState.Instance();
        if (gearsetModule == null || playerState == null)
        {
            return -1;
        }

        var best = -1;
        var bestLevel = 0;
        var bestIsBaseClass = false;
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

            // shouldGetSynced defaults to true, which reports the synced level while the player
            // is level-synced and would rank gearsets against a level nobody actually has.
            int level = playerState->GetClassJobLevel(job, shouldGetSynced: false);
            var isBaseClass = IsBaseClass(job);
            if (best >= 0 && !IsBetterPick(level, isBaseClass, bestLevel, bestIsBaseClass, highest))
            {
                continue;
            }

            best = gearsetIndex;
            bestLevel = level;
            bestIsBaseClass = isBaseClass;
        }
        return best;
    }

    private static bool IsBetterPick(int level, bool isBaseClass, int bestLevel, bool bestIsBaseClass, bool highest)
        => level == bestLevel
            ? bestIsBaseClass && !isBaseClass
            : highest ? level > bestLevel : level < bestLevel;

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
