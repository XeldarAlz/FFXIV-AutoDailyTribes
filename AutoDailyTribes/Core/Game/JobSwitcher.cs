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

    private static readonly byte[] CombatJobIds =
    [
        1, 2, 3, 4, 5, 6, 7,
        19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30,
        31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42,
    ];

    public static bool IsCrafter(byte job) => job >= DohFirst && job <= DohLast;
    public static bool IsGatherer(byte job) => job >= DolFirst && job <= DolLast;
    public static bool IsCombat(byte job) => job > 0 && !IsCrafter(job) && !IsGatherer(job) && job <= 42;

    public static byte CurrentClassJob()
    {
        var ps = PlayerState.Instance();
        return ps == null ? (byte)0 : ps->CurrentClassJobId;
    }

    public static byte? ResolveTargetJob(TribeInfo tribe, Configuration cfg)
    {
        var ps = PlayerState.Instance();
        if (ps == null) return null;
        var current = ps->CurrentClassJobId;

        switch (tribe.Kind)
        {
            case TribeKind.Crafter:
                if (IsCrafter(current)) return null;
                return PickJobFromSetting(cfg.CrafterJobType, cfg.SelectedCrafterJob, DohFirst, DohLast);

            case TribeKind.Gatherer:
                if (IsGatherer(current)) return null;
                return PickJobFromSetting(cfg.GathererJobType, cfg.SelectedGathererJob, DolFirst, DolLast);

            case TribeKind.Mixed:
                if (IsCrafter(current) || IsGatherer(current)) return null;
                return PickJobFromSetting(cfg.CrafterJobType, cfg.SelectedCrafterJob, DohFirst, DohLast)
                    ?? PickJobFromSetting(cfg.GathererJobType, cfg.SelectedGathererJob, DolFirst, DolLast);

            case TribeKind.Combat:
                if (IsCombat(current)) return null;
                return PickCombatJobFromSetting(cfg.CombatJobType, cfg.SelectedCombatJob);

            default:
                return null;
        }
    }

    public static int FindGearsetForJob(byte classJobId)
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

    public static bool EquipGearset(int gearsetId)
    {
        var gm = RaptureGearsetModule.Instance();
        if (gm == null) return false;
        // 0 = success per FFXIVClientStructs; non-zero = error code.
        return gm->EquipGearset(gearsetId, 0) == 0;
    }

    private static byte? PickJobFromSetting(JobChoice mode, uint specificJob, byte first, byte last)
    {
        var ps = PlayerState.Instance();
        if (ps == null) return null;

        switch (mode)
        {
            case JobChoice.Specific:
                return (byte)specificJob;

            case JobChoice.Current:
                return PickByLevel(first, last, highest: true);

            case JobChoice.HighestXP:
                return PickByLevel(first, last, highest: true);

            case JobChoice.LowestXP:
                return PickByLevel(first, last, highest: false);

            default:
                return null;
        }
    }

    private static byte? PickByLevel(byte first, byte last, bool highest)
    {
        var ps = PlayerState.Instance();
        if (ps == null) return null;

        byte best = 0;
        int bestLevel = highest ? -1 : int.MaxValue;
        for (byte job = first; job <= last; job++)
        {
            int lvl = ps->GetClassJobLevel(job);
            if (lvl <= 0) continue;
            var better = highest ? lvl > bestLevel : lvl < bestLevel;
            if (better)
            {
                best = job;
                bestLevel = lvl;
            }
        }
        return best == 0 ? null : best;
    }

    private static byte? PickCombatJobFromSetting(JobChoice mode, uint specificJob)
    {
        var ps = PlayerState.Instance();
        if (ps == null) return null;

        switch (mode)
        {
            case JobChoice.Specific:
                return (byte)specificJob;
            case JobChoice.Current:
            case JobChoice.HighestXP:
                return PickCombatByLevel(highest: true);
            case JobChoice.LowestXP:
                return PickCombatByLevel(highest: false);
            default:
                return null;
        }
    }

    private static byte? PickCombatByLevel(bool highest)
    {
        var ps = PlayerState.Instance();
        if (ps == null) return null;

        byte best = 0;
        int bestLevel = highest ? -1 : int.MaxValue;
        foreach (var job in CombatJobIds)
        {
            int lvl = ps->GetClassJobLevel(job);
            if (lvl <= 0) continue;
            var better = highest ? lvl > bestLevel : lvl < bestLevel;
            if (better)
            {
                best = job;
                bestLevel = lvl;
            }
        }
        return best == 0 ? null : best;
    }
}
