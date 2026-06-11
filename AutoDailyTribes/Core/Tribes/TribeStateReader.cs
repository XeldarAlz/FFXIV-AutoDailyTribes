using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;

namespace AutoDailyTribes.Core.Tribes;

internal static unsafe class TribeStateReader
{
    // Quest sheet row = 0x10000 | id
    private const uint QuestSheetIdFlag = 0x10000u;
    private const int JournalQuestSlots = 30;

    private static uint ToFullQuestId(uint compactId) => QuestSheetIdFlag | compactId;

    public static void Refresh(TribeInfo tribe)
    {
        var ps = PlayerState.Instance();
        var qm = QuestManager.Instance();
        if (ps == null || qm == null) return;

        var tribeId = (byte)tribe.BeastTribeId;
        tribe.Rank = ps->GetBeastTribeRank(tribeId);
        tribe.RepCur = ps->GetBeastTribeCurrentReputation(tribeId);
        tribe.RepMax = ps->GetBeastTribeNeededReputation(tribeId);

        tribe.Unlocked = tribe.Rank >= 1;

        var questSheet = Svc.Data.GetExcelSheet<Quest>();
        tribe.InProgressQuestIds = ScanInProgress(tribe.BeastTribeId, qm, questSheet);
        tribe.AcceptedTodayCount = CountAcceptedToday(tribe.BeastTribeId, qm, questSheet);
        tribe.DailyAllowanceLeft = (int)qm->GetBeastTribeAllowance();
    }

    public static int GlobalAllowanceLeft()
    {
        var qm = QuestManager.Instance();
        return qm != null ? (int)qm->GetBeastTribeAllowance() : AdtConstants.DailyAllowanceCap;
    }

    // A daily that can only be undertaken as Fisher. Questionable has no fishing support, so
    // these are never delegated — flagged for manual completion instead. Only flags FSH-exclusive
    // quests (FSH allowed, Miner/Botanist not), so a MIN/BTN-doable daily is never wrongly skipped.
    public static bool RequiresFisher(uint fullQuestId)
    {
        if (Svc.Data.GetExcelSheet<Quest>()?.GetRowOrDefault(fullQuestId) is not { } quest)
            return false;
        return quest.ClassJobCategory0.ValueNullable is { } cat && cat.FSH && !cat.MIN && !cat.BTN;
    }

    private static uint[] ScanInProgress(uint beastTribeId, QuestManager* qm, Lumina.Excel.ExcelSheet<Quest>? questSheet)
    {
        if (questSheet == null) return [];

        var matched = new List<uint>(capacity: AdtConstants.MaxAcceptsPerTribe);
        for (int i = 0; i < JournalQuestSlots; i++)
        {
            var q = qm->NormalQuests[i];
            if (q.QuestId == 0) continue;
            uint fullId = ToFullQuestId(q.QuestId);
            if (questSheet.GetRowOrDefault(fullId) is { } row
                && row.BeastTribe.RowId == beastTribeId)
            {
                matched.Add(fullId);
            }
        }
        return [.. matched];
    }

    private static int CountAcceptedToday(uint beastTribeId, QuestManager* qm, Lumina.Excel.ExcelSheet<Quest>? questSheet)
    {
        if (questSheet == null) return 0;

        var count = 0;
        var slots = qm->DailyQuests;
        for (int i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];
            if (slot.QuestId == 0) continue;
            uint fullId = ToFullQuestId(slot.QuestId);
            if (questSheet.GetRowOrDefault(fullId) is { } row
                && row.BeastTribe.RowId == beastTribeId)
            {
                count++;
            }
        }
        return count;
    }
}
