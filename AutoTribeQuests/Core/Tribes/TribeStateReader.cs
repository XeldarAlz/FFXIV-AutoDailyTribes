using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;

namespace AutoTribeQuests.Core.Tribes;

// Pulls live tribe state — unlock, rank, daily allowance, accepted-today set —
// off the in-memory game structs and into the TribeInfo.
//
// Cheap enough to call every frame in the Draw() loop:
//   GetBeastTribeRank/Reputation are O(1) game functions,
//   the active-quest scan is bounded by 30 entries.
internal static unsafe class TribeStateReader
{
    public static void Refresh(TribeInfo tribe)
    {
        var ps = PlayerState.Instance();
        var qm = QuestManager.Instance();
        if (ps == null || qm == null) return;

        var tribeId = (byte)tribe.BeastTribeId;
        tribe.Rank = ps->GetBeastTribeRank(tribeId);
        tribe.RepCur = ps->GetBeastTribeCurrentReputation(tribeId);
        tribe.RepMax = ps->GetBeastTribeNeededReputation(tribeId);

        // Rank ≥ 1 ⇔ intro quest done; the game refuses to award reputation otherwise.
        // Avoids hardcoding intro quest IDs per tribe (would be one more VERIFY: per row).
        tribe.Unlocked = tribe.Rank >= 1;

        tribe.AlreadyAcceptedToday = ScanAcceptedDailies(tribe.BeastTribeId, qm);
        tribe.DailyAllowanceLeft = (int)qm->GetBeastTribeAllowance();
    }

    public static int GlobalAllowanceLeft()
    {
        var qm = QuestManager.Instance();
        return qm != null ? (int)qm->GetBeastTribeAllowance() : AtqConstants.DailyAllowanceCap;
    }

    // Walks the player's NormalQuests slots and matches each active quest's
    // BeastTribe reference against this tribe's id. Avoids depending on the
    // BeastTribeQuest sheet (not directly exposed in Lumina's bindings).
    //
    // QuestWork.QuestId is the compact 16-bit form; the Quest sheet uses
    // 0x10000 + that value as the full row id.
    private static uint[] ScanAcceptedDailies(uint beastTribeId, QuestManager* qm)
    {
        var questSheet = Svc.Data.GetExcelSheet<Quest>();
        if (questSheet == null) return [];

        var matched = new List<uint>(capacity: AtqConstants.MaxAcceptsPerTribe);
        for (int i = 0; i < 30; i++)
        {
            var q = qm->NormalQuests[i];
            if (q.QuestId == 0) continue;
            uint fullId = 0x10000u | q.QuestId;
            if (questSheet.GetRowOrDefault(fullId) is { } row
                && row.BeastTribe.RowId == beastTribeId)
            {
                matched.Add(fullId);
            }
        }
        return [.. matched];
    }
}
