using ECommons.DalamudServices;
using Lumina.Excel.Sheets;

namespace AutoDailyTribes.Core.Tribes;

// Allied society dailies carry a level requirement that steps up with reputation rank: Amalj'aa ask
// 43 at Neutral, 46 at Recognized and 48 from Friendly on, Vanu Vanu ask 50 flat, Ixal ask 1. A job
// under the bar equips and travels fine and is then refused every offer at the issuer, which reads
// as a wedged run. The requirement is sheet data, so it is read once and handed to the gearset
// picker instead of being hardcoded per tribe.
internal static class TribeDailyLevels
{
    private static byte[][]? floorsByTribe;

    // Highest level asked by any daily the tribe has unlocked at this rank — a job at or above it
    // can accept every offer the issuer can make. 0 when the tribe has no level-gated dailies.
    public static int Required(uint beastTribeId, int rank)
    {
        var table = floorsByTribe ??= Build();
        if (beastTribeId >= (uint)table.Length)
        {
            return 0;
        }

        var floors = table[beastTribeId];
        return floors == null ? 0 : floors[Math.Clamp(rank, 1, AdtConstants.MaxTribeRank)];
    }

    private static byte[][] Build()
    {
        var questSheet = Svc.Data.GetExcelSheet<Quest>();
        if (questSheet == null)
        {
            return [];
        }

        var table = new byte[HighestRegisteredTribeId() + 1][];
        for (var rowIndex = 0; rowIndex < questSheet.Count; rowIndex++)
        {
            var quest = questSheet.GetRowAt(rowIndex);
            if (!quest.IsRepeatable)
            {
                continue;
            }

            var tribeId = quest.BeastTribe.RowId;
            if (tribeId == 0 || tribeId >= (uint)table.Length)
            {
                continue;
            }

            var rank = (int)quest.BeastReputationRank.RowId;
            if (rank < 1 || rank > AdtConstants.MaxTribeRank)
            {
                continue;
            }

            var level = quest.ClassJobLevel.Count > 0 ? quest.ClassJobLevel[0] : 0;
            if (level == 0)
            {
                continue;
            }

            var floors = table[tribeId] ??= new byte[AdtConstants.MaxTribeRank + 1];
            if (level > floors[rank])
            {
                floors[rank] = (byte)level;
            }
        }

        // Reaching a rank never retires the earlier tiers, so each rank inherits the toughest
        // requirement unlocked so far. Most later tribes hang every tier off Friendly and above and
        // leave the first ranks empty — those carry the entry tier rather than reading as ungated,
        // which would leave a Kojin run at Neutral with no level check at all.
        for (var tribeIndex = 0; tribeIndex < table.Length; tribeIndex++)
        {
            var floors = table[tribeIndex];
            if (floors == null)
            {
                continue;
            }

            var carried = EntryTier(floors);
            for (var rank = 1; rank < floors.Length; rank++)
            {
                if (floors[rank] > carried)
                {
                    carried = floors[rank];
                }
                floors[rank] = carried;
            }
        }
        return table;
    }

    private static byte EntryTier(byte[] floors)
    {
        for (var rank = 1; rank < floors.Length; rank++)
        {
            if (floors[rank] != 0)
            {
                return floors[rank];
            }
        }
        return 0;
    }

    private static uint HighestRegisteredTribeId()
    {
        var tribes = TribeRegistry.Tribes;
        var highest = 0u;
        for (var tribeIndex = 0; tribeIndex < tribes.Length; tribeIndex++)
        {
            if (tribes[tribeIndex].BeastTribeId > highest)
            {
                highest = tribes[tribeIndex].BeastTribeId;
            }
        }
        return highest;
    }
}
