using ECommons.DalamudServices;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace AutoDailyTribes.Core.Tribes;

// Some reputation ranks only roll over once a story quest is turned in: reputation sits at the
// rank's cap until then, and the hand-in refreshes the day's three daily offers. The Quest sheet
// marks those quests with the tribe, the rank they belong to and a reputation value of 65535 (the
// cap itself); every other rank rolls over on its own. The ARR tribes gate Neutral through Friendly
// this way (the Ixal every rank up to Honored), the later tribes Friendly through Sworn.
internal static class TribeRankUpQuests
{
    private const ushort ReputationCap = ushort.MaxValue;
    private const byte JoinAtLeastOne = 2;
    private const int PrerequisiteDepth = 3;

    private static uint[][]? questsByTribeRank;

    // The story quest that lifts the tribe past this rank, 0 when the rank rolls over on its own.
    public static uint RankUpQuest(uint beastTribeId, int rank)
    {
        var table = questsByTribeRank ??= Build();
        if (beastTribeId >= (uint)table.Length || rank < 1 || rank > AdtConstants.MaxTribeRank)
        {
            return 0;
        }

        var quests = table[beastTribeId];
        return quests == null ? 0 : quests[rank];
    }

    // The quest to hand to Questionable next on the way past the current rank: the rank-up quest
    // itself once its prerequisites are complete, else the first prerequisite the tribe hands out
    // that can be started right now (the Qitari ask for one of two stela quests before each
    // rank-up). 0 when the rank rolls over on its own or nothing can be started.
    public static uint NextStep(uint beastTribeId, int rank)
    {
        var rankUp = RankUpQuest(beastTribeId, rank);
        if (rankUp == 0 || TribeStateReader.IsQuestComplete(rankUp))
        {
            return 0;
        }

        var questSheet = Svc.Data.GetExcelSheet<Quest>();
        return questSheet == null ? 0 : NextStep(questSheet, rankUp, beastTribeId, rank, PrerequisiteDepth);
    }

    private static uint NextStep(ExcelSheet<Quest> questSheet, uint questId, uint beastTribeId, int rank, int depth)
    {
        if (questSheet.GetRowOrDefault(questId) is not { } quest || IsLocked(quest))
        {
            return 0;
        }

        if (PrerequisitesComplete(quest))
        {
            return questId;
        }

        if (depth == 0)
        {
            return 0;
        }

        var previous = quest.PreviousQuest;
        for (var index = 0; index < previous.Count; index++)
        {
            var previousId = previous[index].RowId;
            if (previousId == 0 || TribeStateReader.IsQuestComplete(previousId))
            {
                continue;
            }

            if (questSheet.GetRowOrDefault(previousId) is not { } prerequisite
                || prerequisite.BeastTribe.RowId != beastTribeId
                || prerequisite.BeastReputationRank.RowId > (uint)rank)
            {
                continue;
            }

            var step = NextStep(questSheet, previousId, beastTribeId, rank, depth - 1);
            if (step != 0)
            {
                return step;
            }
        }

        return 0;
    }

    private static bool PrerequisitesComplete(Quest quest)
    {
        var previous = quest.PreviousQuest;
        var required = 0;
        var completed = 0;
        for (var index = 0; index < previous.Count; index++)
        {
            var previousId = previous[index].RowId;
            if (previousId == 0)
            {
                continue;
            }

            required++;
            if (TribeStateReader.IsQuestComplete(previousId))
            {
                completed++;
            }
        }

        if (required == 0)
        {
            return true;
        }

        return quest.PreviousQuestJoin == JoinAtLeastOne ? completed > 0 : completed == required;
    }

    // A quest whose lock quests are complete is gone for good: each Qitari stela quest locks its
    // sibling, so only the one the player did not pick is ever locked.
    private static bool IsLocked(Quest quest)
    {
        var locks = quest.QuestLock;
        var present = 0;
        var completed = 0;
        for (var index = 0; index < locks.Count; index++)
        {
            var lockId = locks[index].RowId;
            if (lockId == 0)
            {
                continue;
            }

            present++;
            if (TribeStateReader.IsQuestComplete(lockId))
            {
                completed++;
            }
        }

        if (present == 0)
        {
            return false;
        }

        return quest.QuestLockJoin == JoinAtLeastOne ? completed > 0 : completed == present;
    }

    private static uint[][] Build()
    {
        var questSheet = Svc.Data.GetExcelSheet<Quest>();
        if (questSheet == null)
        {
            return [];
        }

        var table = new uint[TribeRegistry.HighestTribeId + 1][];
        for (var rowIndex = 0; rowIndex < questSheet.Count; rowIndex++)
        {
            var quest = questSheet.GetRowAt(rowIndex);
            if (quest.IsRepeatable || quest.BeastReputationValue != ReputationCap)
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

            var quests = table[tribeId] ??= new uint[AdtConstants.MaxTribeRank + 1];
            if (quests[rank] == 0)
            {
                quests[rank] = quest.RowId;
            }
        }

        return table;
    }
}
