using AutoDailyTribes.Core.External;
using AutoDailyTribes.Core.Game;
using AutoDailyTribes.Core.Ipc;
using AutoDailyTribes.Core.Tribes;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using Lumina.Excel.Sheets;
using System.Numerics;
using System.Threading.Tasks;

namespace AutoDailyTribes.Core.Tasks;

// Reputation sitting at a rank's cap is wasted on every further daily until the rank-up quest is
// turned in, and that hand-in also refreshes the tribe's three daily offers. So a capped tribe
// plays its rank-up quest before accepting anything, through Questionable's single-quest mode,
// which stops by itself at the hand-in instead of wandering onto the MSQ, and then picks up the
// fresh dailies the new rank unlocks.
public sealed partial class AutoTribe
{
    private const int MaxRankUpSteps = 3;

    private int  rankUpSteps;
    private bool rankUpBlocked;

    private bool ShouldRankUp()
        => !rankUpBlocked && rankUpSteps < MaxRankUpSteps && tribe.CanAutoRankUp && !tribe.HasInProgressQuests;

    private async Task RunRankUp()
    {
        var questId = tribe.RankUpQuestId;
        var name = QuestName(questId);
        var rankBefore = tribe.Rank;
        rankUpSteps++;

        if (!RankUpQuestRunnable(questId, name))
        {
            rankUpBlocked = true;
            return;
        }

        Diag($"{tribe.Name}: reputation is capped at rank {rankBefore}; playing {questId:X} ({name}) through Questionable [{rankUpSteps}/{MaxRankUpSteps}]");
        var completed = await DriveRankUpQuest(questId, name);
        TribeStateReader.Refresh(tribe);
        if (!completed)
        {
            rankUpBlocked = true;
            Warn($"{tribe.Name}: the rank-up quest {name} was not finished; complete it in game to unlock 3 more dailies today");
            return;
        }

        Diag(tribe.Rank > rankBefore
            ? $"{tribe.Name}: ranked up to {tribe.Rank}; the daily offers are refreshed"
            : $"{tribe.Name}: {name} turned in; still rank {tribe.Rank}");

        // Questionable moved the player and may have swapped jobs, and the new rank can unlock a
        // better-paying quest giver, so the run starts over from the job check.
        jobResolved = false;
        arrivedAtIssuer = false;
        delegated = false;
        issuerIndex = 0;
        announcedIssuer = -1;
        Array.Clear(issuerFailPasses);
    }

    private bool RankUpQuestRunnable(uint questId, string name)
    {
        if (Svc.Data.GetExcelSheet<Quest>()?.GetRowOrDefault(questId) is not { } quest) return false;

        const string manualHint = "; finish it in game to unlock 3 more dailies today";
        var job = JobSwitcher.CurrentClassJob();
        var requiredLevel = quest.ClassJobLevel.Count > 0 ? quest.ClassJobLevel[0] : 0;
        var jobLevel = JobSwitcher.JobLevel(job);
        if (requiredLevel > jobLevel)
        {
            Warn($"{tribe.Name}: the rank-up quest {name} needs level {requiredLevel}, but {JobSwitcher.JobName(job)} is level {jobLevel}{manualHint}");
            return false;
        }

        if (quest.ClassJobCategory0.ValueNullable is { } category && !category.IsJobInCategory((Job)job))
        {
            Warn($"{tribe.Name}: the rank-up quest {name} cannot be taken as {JobSwitcher.JobName(job)} ({category.Name.ExtractText()}){manualHint}");
            return false;
        }

        if (QuestionableQuestPaths.DisabledAmong([questId]).Length > 0)
        {
            Warn($"{tribe.Name}: Questionable has no working path for the rank-up quest {name}{manualHint}");
            return false;
        }

        var accepted = TribeStateReader.QuestSequence(questId) != TribeStateReader.NotInJournal;
        if (accepted || questionable.IsReadyToAcceptQuest(questId)) return true;

        var reason = questionable.LockedReason(questId);
        Warn($"{tribe.Name}: Questionable reports the rank-up quest {name} cannot be accepted" + (reason.Length > 0 ? $" ({reason})" : "") + manualHint);
        return false;
    }

    // Single-quest mode is the right fit here: the quest sits alone on the priority list, so
    // Questionable keeps it as its current quest from accept to hand-in, then goes idle the moment
    // the game's focus moves to anything else. Polled every frame so a stop is never late.
    private async Task<bool> DriveRankUpQuest(uint questId, string name)
    {
        questionable.ClearQuestPriority();
        questionable.AddQuestPriority(questId);
        if (!questionable.StartSingleQuest(questId))
        {
            Diag($"{tribe.Name}: Questionable.StartSingleQuest rejected {questId:X} ({name})");
            return false;
        }

        var deadline = Environment.TickCount64 + AdtConstants.QuestCompleteTimeoutMs;
        var progressSinceMs = Environment.TickCount64;
        long? idleSinceMs = null;
        var restarts = 0;
        var lastSequence = TribeStateReader.QuestSequence(questId);
        var moveAnchor = Svc.Objects.LocalPlayer?.Position;

        try
        {
            while (Environment.TickCount64 < deadline && !CancelToken.IsCancellationRequested)
            {
                if (TribeStateReader.IsQuestComplete(questId))
                {
                    Diag($"{tribe.Name}: {name} turned in");
                    return true;
                }

                var now = Environment.TickCount64;
                var sequence = TribeStateReader.QuestSequence(questId);
                var playerPos = Svc.Objects.LocalPlayer?.Position;
                var moved = playerPos is { } pos
                         && (moveAnchor is null || Vector3.Distance(moveAnchor.Value, pos) > StuckMoveThresholdMeters);
                if (sequence != lastSequence || moved || IsActivelyBusy() || NavmeshIPC.Instance.IsBusy())
                {
                    lastSequence = sequence;
                    moveAnchor = playerPos ?? moveAnchor;
                    progressSinceMs = now;
                }

                Status = sequence switch
                {
                    TribeStateReader.NotInJournal  => $"Accepting {name}",
                    TribeStateReader.ReadyToTurnIn => $"Turning in {name}",
                    _                              => $"Playing {name}",
                };

                if (now - lastHeartbeatAtMs >= HeartbeatMs)
                {
                    lastHeartbeatAtMs = now;
                    Diag($"HEARTBEAT {tribe.Name} rank-up: questionable running={questionable.IsRunning()} " +
                         $"current={questionable.CurrentQuestId() ?? "none"} sequence={sequence} noProgressFor={(now - progressSinceMs) / 1000}s");
                }

                if (now - progressSinceMs >= AdtConstants.QuestStuckMs)
                {
                    Diag($"{tribe.Name}: Questionable made no progress on {name} for {AdtConstants.QuestStuckMs / 1000}s");
                    return false;
                }

                if (questionable.IsRunning())
                {
                    idleSinceMs = null;
                }
                else
                {
                    idleSinceMs ??= now;
                    if (now - idleSinceMs.Value >= AdtConstants.QuestIdleRestartMs)
                    {
                        if (restarts >= AdtConstants.MaxQuestRestarts)
                        {
                            Diag($"{tribe.Name}: Questionable keeps stopping on {name}");
                            return false;
                        }

                        restarts++;
                        Diag($"{tribe.Name}: Questionable went idle on {name}; restarting [{restarts}/{AdtConstants.MaxQuestRestarts}]");
                        questionable.StartSingleQuest(questId);
                        idleSinceMs = null;
                    }
                }

                await NextFrame();
            }

            Diag($"{tribe.Name}: {name} did not finish within {AdtConstants.QuestCompleteTimeoutMs / 60_000} minutes");
            return false;
        }
        finally
        {
            questionable.Stop("ADT: rank-up quest");
            questionable.ClearQuestPriority();
        }
    }
}
