using AutoDailyTribes.Core.Tribes;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace AutoDailyTribes.Core.Game;

internal static unsafe class JobSwitcher
{
    private const int MaxGearsets = 100;

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

    public static byte CurrentClassJob()
    {
        var ps = PlayerState.Instance();
        return ps == null ? (byte)0 : ps->CurrentClassJobId;
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
        var gm = RaptureGearsetModule.Instance();
        if (gm == null) return 0;
        var entry = gm->GetGearset(gearsetId);
        return entry == null ? (byte)0 : entry->ClassJob;
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
        var gm = RaptureGearsetModule.Instance();
        if (gm == null) return false;
        return gm->EquipGearset(gearsetId, 0) == 0;  // 0 = success
    }

    private static int PickFromCategory(JobChoice mode, uint specificJob, Func<byte, bool> inCategory)
    {
        // Honor a specific job only if it's automatable for this category — guards against
        // e.g. Specific=Fisher, which Questionable can't complete, falling through to MIN/BTN.
        if (mode == JobChoice.Specific && inCategory((byte)specificJob))
        {
            var exact = FindGearsetForJob((byte)specificJob);
            if (exact >= 0) return exact;
        }

        var highest = mode != JobChoice.LowestXP;
        return PickGearsetByLevel(inCategory, highest);
    }

    private static int PickGearsetByLevel(Func<byte, bool> inCategory, bool highest)
    {
        var gm = RaptureGearsetModule.Instance();
        var ps = PlayerState.Instance();
        if (gm == null || ps == null) return -1;

        var best = -1;
        var bestLevel = highest ? -1 : int.MaxValue;
        for (var i = 0; i < MaxGearsets; i++)
        {
            if (!gm->IsValidGearset(i)) continue;
            var entry = gm->GetGearset(i);
            if (entry == null) continue;

            var job = entry->ClassJob;
            if (!inCategory(job)) continue;

            int lvl = ps->GetClassJobLevel(job);
            var better = highest ? lvl > bestLevel : lvl < bestLevel;
            if (better)
            {
                best = i;
                bestLevel = lvl;
            }
        }
        return best;
    }

    private static int FindGearsetForJob(byte classJobId)
    {
        var gm = RaptureGearsetModule.Instance();
        if (gm == null) return -1;
        for (int i = 0; i < MaxGearsets; i++)
        {
            if (!gm->IsValidGearset(i)) continue;
            var entry = gm->GetGearset(i);
            if (entry != null && entry->ClassJob == classJobId)
                return i;
        }
        return -1;
    }
}
