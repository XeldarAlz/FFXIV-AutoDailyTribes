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

    public static bool IsCrafter(byte job) => job >= DohFirst && job <= DohLast;
    public static bool IsGatherer(byte job) => job >= DolFirst && job <= DolLast;
    public static bool IsCombat(byte job) => job > 0 && !IsCrafter(job) && !IsGatherer(job) && job <= 42;

    public static byte CurrentClassJob()
    {
        var ps = PlayerState.Instance();
        return ps == null ? (byte)0 : ps->CurrentClassJobId;
    }

    // True when the job we're already on can do this tribe's dailies — no switch needed.
    public static bool CurrentJobSatisfies(TribeKind kind)
    {
        var current = CurrentClassJob();
        return kind switch
        {
            TribeKind.Crafter => IsCrafter(current),
            TribeKind.Gatherer => IsGatherer(current),
            TribeKind.Mixed => IsCrafter(current) || IsGatherer(current),
            TribeKind.Combat => IsCombat(current),
            _ => true,
        };
    }

    public static byte GearsetClassJob(int gearsetId)
    {
        var gm = RaptureGearsetModule.Instance();
        if (gm == null) return 0;
        var entry = gm->GetGearset(gearsetId);
        return entry == null ? (byte)0 : entry->ClassJob;
    }

    // Pick a gearset to equip for this tribe, chosen ONLY from gearsets the player actually owns in
    // the right category. This is why HighestXP no longer skips the tribe: we never target a job
    // without a gearset. Returns the gearset index, or -1 if the player has no suitable gearset.
    public static int PickGearset(TribeInfo tribe, Configuration cfg)
    {
        switch (tribe.Kind)
        {
            case TribeKind.Crafter:
                return PickFromCategory(cfg.CrafterJobType, cfg.SelectedCrafterJob, IsCrafter);
            case TribeKind.Gatherer:
                return PickFromCategory(cfg.GathererJobType, cfg.SelectedGathererJob, IsGatherer);
            case TribeKind.Mixed:
                var crafter = PickFromCategory(cfg.CrafterJobType, cfg.SelectedCrafterJob, IsCrafter);
                return crafter >= 0 ? crafter : PickFromCategory(cfg.GathererJobType, cfg.SelectedGathererJob, IsGatherer);
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
        // 0 = success (request dispatched) per FFXIVClientStructs; -1 = rejected.
        return gm->EquipGearset(gearsetId, 0) == 0;
    }

    private static int PickFromCategory(JobChoice mode, uint specificJob, Func<byte, bool> inCategory)
    {
        // Specific: take the player's gearset for exactly that job if they own one; otherwise fall
        // through to a level-based pick within the category rather than failing the whole tribe.
        if (mode == JobChoice.Specific)
        {
            var exact = FindGearsetForJob((byte)specificJob);
            if (exact >= 0) return exact;
        }

        // Current maps to "highest level I own in this category" — when we're picking at all, the
        // current job is by definition not in the category (CurrentJobSatisfies gated that out).
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
