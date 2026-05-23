using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;

namespace AutoTribeQuests.Core.Tribes;

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
        // Avoids hardcoding intro quest IDs per tribe.
        tribe.Unlocked = tribe.Rank >= 1;

        tribe.AlreadyAcceptedToday = ScanAcceptedDailies(tribe.BeastTribeId, qm);
        tribe.DailyAllowanceLeft = (int)qm->GetBeastTribeAllowance();
    }

    public static int GlobalAllowanceLeft()
    {
        var qm = QuestManager.Instance();
        return qm != null ? (int)qm->GetBeastTribeAllowance() : AtqConstants.DailyAllowanceCap;
    }

    private static uint[] ScanAcceptedDailies(uint beastTribeId, QuestManager* qm)
    {
        var questSheet = Svc.Data.GetExcelSheet<Quest>();
        if (questSheet == null) return [];

        var matched = new List<uint>(capacity: AtqConstants.MaxAcceptsPerTribe);
        for (int i = 0; i < 30; i++)
        {
            var q = qm->NormalQuests[i];
            if (q.QuestId == 0) continue;
            // QuestWork.QuestId is the compact 16-bit form; Quest sheet uses 0x10000 | id.
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
