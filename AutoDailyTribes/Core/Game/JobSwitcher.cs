using AutoDailyTribes.Core.Tribes;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace AutoDailyTribes.Core.Game;

internal static unsafe class JobSwitcher
{
    private const int MaxGearsets = 100;

    private const byte DohFirst = 8;   // CRP
    private const byte DohLast  = 15;  // CUL
    private const byte DolFirst = 16;  // MIN
    private const byte DolLast  = 18;  // FSH

    public static bool IsCrafter(byte job) => job >= DohFirst && job <= DohLast;
    public static bool IsGatherer(byte job) => job >= DolFirst && job <= DolLast;

    public static byte CurrentClassJob()
    {
        var ps = PlayerState.Instance();
        return ps == null ? (byte)0 : ps->CurrentClassJobId;
    }

    // null = current class already qualifies / no swap configured. Combat tribes always return null.
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
                // Only reachable when current isn't in this category — fall back to highest in range.
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
}
