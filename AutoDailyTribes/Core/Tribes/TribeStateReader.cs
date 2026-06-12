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

        var cid = ps->ContentId;
        if (cid != tribe.LastSeenCid)
        {
            tribe.LastSeenCid = cid;
            HydrateCycle(tribe, cid);
        }

        var tribeId = (byte)tribe.BeastTribeId;
        var prevRank = tribe.LastSeenRank;
        var prevBaseline = tribe.RankCycleBaseline;
        tribe.Rank = ps->GetBeastTribeRank(tribeId);
        tribe.RepCur = ps->GetBeastTribeCurrentReputation(tribeId);
        tribe.RepMax = ps->GetBeastTribeNeededReputation(tribeId);

        tribe.Unlocked = tribe.Rank >= 1;

        var questSheet = Svc.Data.GetExcelSheet<Quest>();
        tribe.InProgressQuestIds = ScanInProgress(tribe.BeastTribeId, qm, questSheet);
        var slotEntries = CountDailySlotEntries(tribe.BeastTribeId, qm, questSheet);
        tribe.DailyAllowanceLeft = (int)qm->GetBeastTribeAllowance();

        // Ranking up mid-day refreshes the tribe's three daily offers, but the completed ones stay
        // in the daily-done slots until reset — baseline them so they stop counting as "today's".
        if (prevRank >= 0 && tribe.Rank > prevRank)
            tribe.RankCycleBaseline = slotEntries;

        var nowUtc = DateTime.UtcNow;
        if (slotEntries < tribe.RankCycleBaseline || CrossedDailyReset(tribe.LastRefreshUtc, nowUtc))
            tribe.RankCycleBaseline = 0;

        tribe.LastSeenRank = tribe.Rank;
        tribe.LastRefreshUtc = nowUtc;
        tribe.AcceptedTodayCount = Math.Max(0, slotEntries - tribe.RankCycleBaseline);

        if (tribe.Rank != prevRank || tribe.RankCycleBaseline != prevBaseline)
            PersistCycle(tribe, cid, nowUtc);
    }

    private static string CycleKey(ulong cid, uint beastTribeId) => $"{cid}:{beastTribeId}";

    // The rank-cycle baseline only lives until the next daily reset, so anything saved before
    // the last reset is discarded on hydrate.
    private static void HydrateCycle(TribeInfo tribe, ulong cid)
    {
        tribe.LastSeenRank = -1;
        tribe.RankCycleBaseline = 0;
        tribe.LastRefreshUtc = default;

        if (Plugin.Cfg is not { } cfg) return;
        if (!cfg.RankCycles.TryGetValue(CycleKey(cid, tribe.BeastTribeId), out var saved)) return;
        if (CrossedDailyReset(saved.SavedUtc, DateTime.UtcNow)) return;

        tribe.LastSeenRank = saved.LastSeenRank;
        tribe.RankCycleBaseline = saved.Baseline;
        tribe.LastRefreshUtc = saved.SavedUtc;
    }

    // Rank/baseline changes are rare (a few per day at most), so save immediately rather than
    // debounced — a throttled save here could be lost to a quick plugin reload.
    private static void PersistCycle(TribeInfo tribe, ulong cid, DateTime nowUtc)
    {
        if (Plugin.Cfg is not { } cfg) return;
        cfg.RankCycles[CycleKey(cid, tribe.BeastTribeId)] = new TribeCycleState
        {
            LastSeenRank = tribe.Rank,
            Baseline = tribe.RankCycleBaseline,
            SavedUtc = nowUtc,
        };
        cfg.Save();
    }

    private static bool CrossedDailyReset(DateTime lastUtc, DateTime nowUtc)
    {
        if (lastUtc == default) return false;
        var reset = new DateTime(nowUtc.Year, nowUtc.Month, nowUtc.Day, 15, 0, 0, DateTimeKind.Utc);
        if (reset > nowUtc) reset = reset.AddDays(-1);
        return lastUtc < reset;
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

    // Rank-up and story quests carry the tribe's BeastTribe id too, but only dailies are
    // repeatable — without this filter an accepted rank-up quest counts as an in-progress
    // daily (skewing the card math) and gets delegated to Questionable.
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
                && row.BeastTribe.RowId == beastTribeId
                && row.IsRepeatable)
            {
                matched.Add(fullId);
            }
        }
        return [.. matched];
    }

    private static int CountDailySlotEntries(uint beastTribeId, QuestManager* qm, Lumina.Excel.ExcelSheet<Quest>? questSheet)
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
