using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Data.Files;
using Lumina.Data.Parsing.Layer;
using Lumina.Excel.Sheets;
using System.Numerics;

namespace AutoTribeQuests;

// Static tribe table + planevent.lgb resolver to locate the issuer NPC's live world instance.
//
// IMPORTANT: every (BeastTribeId, IssuerENpcBaseId, UnlockQuestId, IssuerTerritoryId) tuple below needs
// to be verified against the Lumina sheets at first run. Reads are at:
//   BeastTribe          - tribe metadata (name, currency, ranks, intro quest)
//   BeastReputationRank - rank thresholds
//   BeastTribeQuest     - the actual daily quest IDs per tribe
//   ENpcResident        - NPC names
//   Quest               - intro quest IDs
// I've seeded Amalj'aa with my best guesses so the wiring compiles. Replace ??? entries once verified.
public static class TribeData
{
    // BeastTribe row IDs (1 = Amalj'aa, 2 = Sylphs, ...). See BeastTribe sheet.
    public static readonly TribeInfo[] Tribes =
    [
        new()
        {
            BeastTribeId = 1,
            Name = "Amalj'aa",
            Kind = TribeJobKind.Combat,
            UnlockQuestId = 65602,                  // VERIFY: "Peace for Thanalan"
            MinRankForDailies = 1,
            IssuerTerritoryId = 145,                // Eastern Thanalan
            IssuerENpcBaseId = 1006722,             // VERIFY: Bartholomew (daily issuer)
            IssuerSelectStringIndex = 0,
        },
        new()
        {
            BeastTribeId = 2,
            Name = "Sylphs",
            Kind = TribeJobKind.Combat,
            UnlockQuestId = 65741,                  // VERIFY
            MinRankForDailies = 1,
            IssuerTerritoryId = 152,                // East Shroud
            IssuerENpcBaseId = 1006821,             // VERIFY: Komuxio
            IssuerSelectStringIndex = 0,
        },
        new()
        {
            BeastTribeId = 3,
            Name = "Kobolds",
            Kind = TribeJobKind.Combat,
            UnlockQuestId = 65643,                  // VERIFY
            MinRankForDailies = 1,
            IssuerTerritoryId = 180,                // Outer La Noscea
            IssuerENpcBaseId = 1006747,             // VERIFY: Drekkenfrau
            IssuerSelectStringIndex = 0,
        },
        new()
        {
            BeastTribeId = 4,
            Name = "Sahagin",
            Kind = TribeJobKind.Combat,
            UnlockQuestId = 65644,                  // VERIFY
            MinRankForDailies = 1,
            IssuerTerritoryId = 138,                // Western La Noscea
            IssuerENpcBaseId = 1006750,             // VERIFY: Novv
            IssuerSelectStringIndex = 0,
        },
        new()
        {
            BeastTribeId = 5,
            Name = "Ixal",
            Kind = TribeJobKind.Crafter,
            UnlockQuestId = 65970,                  // VERIFY
            MinRankForDailies = 1,
            IssuerTerritoryId = 154,                // North Shroud
            IssuerENpcBaseId = 1007599,             // VERIFY: Scarlet (Ixal envoy in NW Coerthas)
            IssuerSelectStringIndex = 0,
        },

        // TODO: HW Vanu Vanu (territory 399), Vath (400), Moogles (401)
        // TODO: SB Kojin (613), Ananta (614), Namazu (622 — has clan selector quirk)
        // TODO: ShB Pixies (816), Qitari (818), Dwarves (820)
        // TODO: EW Arkasodara (957), Omicrons (958), Loporrits (959)
        // TODO: DT Pelupelu (1191) — verify Questionable coverage first
    ];

    /// Resolve the issuer's live world InstanceId + Position from the territory's planevent.lgb.
    /// Mirrors vsatisfy/CraftTurnin.cs.
    public static unsafe void ResolveIssuerLocation(TribeInfo tribe)
    {
        if (tribe.IssuerInstanceId != 0) return; // already resolved

        if (Service.LuminaRow<TerritoryType>(tribe.IssuerTerritoryId) is not { Bg.IsEmpty: false } territory)
        {
            Service.Log.Warning($"[{tribe.Name}] No TerritoryType row for {tribe.IssuerTerritoryId}");
            return;
        }

        var scene = territory.Bg.ToString();
        var filenameStart = scene.LastIndexOf('/') + 1;
        var planeventLgb = "bg/" + scene[..filenameStart] + "planevent.lgb";
        Service.Log.Debug($"[{tribe.Name}] scanning {planeventLgb} for ENpc {tribe.IssuerENpcBaseId}");

        var lgb = Service.DataManager.GetFile<LgbFile>(planeventLgb);
        if (lgb == null)
        {
            Service.Log.Warning($"[{tribe.Name}] failed to load {planeventLgb}");
            return;
        }

        foreach (var layer in lgb.Layers)
        {
            foreach (var instance in layer.InstanceObjects)
            {
                if (instance.AssetType != LayerEntryType.EventNPC) continue;
                var baseId = ((LayerCommon.ENPCInstanceObject)instance.Object).ParentData.ParentData.BaseId;
                if (baseId != tribe.IssuerENpcBaseId && !tribe.AltIssuerENpcBaseIds.Contains(baseId))
                    continue;

                tribe.IssuerInstanceId = (1ul << 32) | instance.InstanceId;
                tribe.IssuerLocation = new(
                    instance.Transform.Translation.X,
                    instance.Transform.Translation.Y,
                    instance.Transform.Translation.Z);
                Service.Log.Info($"[{tribe.Name}] resolved issuer at {tribe.IssuerLocation} ({tribe.IssuerInstanceId:X})");
                return;
            }
        }

        Service.Log.Warning($"[{tribe.Name}] issuer ENpc {tribe.IssuerENpcBaseId} not found in {planeventLgb}");
    }

    /// Refresh per-tribe live state (unlock, rank, daily allowance, accepted-today set).
    public static unsafe void RefreshLiveState(TribeInfo tribe)
    {
        tribe.Unlocked = QuestManager.IsQuestComplete(tribe.UnlockQuestId);

        // TODO: rank + reputation from BeastReputation in PlayerState. Stub for now:
        tribe.Rank = 0;
        tribe.RepCur = 0;
        tribe.RepMax = 0;

        // TODO: count accepted dailies for this tribe from the active quest list:
        //   QuestManager.NormalQuests[i].QuestId, cross-referenced with BeastTribeQuest sheet
        tribe.AlreadyAcceptedToday = [];

        // TODO: read remaining global allowance (12/day cap)
        tribe.DailyAllowanceLeft = 12;
    }

    public static bool IsAvailableNow(TribeInfo tribe)
        => tribe.Unlocked
        && tribe.MeetsRankRequirement
        && tribe.AlreadyAcceptedToday.Length < 3
        && tribe.DailyAllowanceLeft > 0
        && !Plugin.Config.DisabledTribes.Contains(tribe.BeastTribeId);
}
