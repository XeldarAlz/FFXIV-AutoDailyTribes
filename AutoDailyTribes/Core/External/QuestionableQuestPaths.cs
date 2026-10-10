using AutoDailyTribes.Core.Ipc;
using AutoDailyTribes.Core.Tribes;
using ECommons.Reflection;
using System.Collections;
using System.Reflection;

namespace AutoDailyTribes.Core.External;

internal static class QuestionableQuestPaths
{
    private const string QuestRegistryTypeName = "Questionable.Controller.QuestRegistry";
    private const string TryGetQuestMethodName = "TryGetQuest";
    private const string ElementIdFromStringMethodName = "FromString";
    private const string RootPropertyName = "Root";
    private const string DisabledPropertyName = "Disabled";
    private const string SequencesPropertyName = "QuestSequence";
    private const string SequenceNumberPropertyName = "Sequence";
    private const string StepsPropertyName = "Steps";
    private const string InteractionTypePropertyName = "InteractionType";
    private const string PositionPropertyName = "Position";
    private const string FlyPropertyName = "Fly";
    private const string AetheryteShortcutPropertyName = "AetheryteShortcut";
    private const string AethernetShortcutPropertyName = "AethernetShortcut";
    private const string SkipConditionsPropertyName = "SkipConditions";
    private const string StepIfPropertyName = "StepIf";
    private const string HasSkipConditionsMethodName = "HasSkipConditions";
    private const string CompleteQuestInteraction = "CompleteQuest";
    private const string GatherInteraction = "Gather";
    private const int TryGetQuestParameterCount = 2;

    public static uint[] DisabledAmong(uint[] questIds)
        => Matching(questIds, IsDisabled, "disabled quest paths");

    // Questionable restarts a quest it is handed again at step 0 of the game's current sequence, so
    // every step a path runs before its CompleteQuest step repeats from wherever the player stands by
    // then. A hand-in that opens with a plain ride or walk (the Sahagin ferry back from the Spawning
    // Grounds, a walk out of a Pixie pond) only works from the objective site; one that teleports,
    // flies, skips itself when moot, or gathers first can be picked up from anywhere.
    public static uint[] TurnInsBoundToObjectiveSite(uint[] questIds)
        => Matching(questIds, HasSiteBoundTurnIn, "hand-in paths");

    private static uint[] Matching(uint[] questIds, Func<object, bool> predicate, string subject)
    {
        if (questIds.Length == 0)
        {
            return [];
        }

        try
        {
            return FindMatching(questIds, predicate);
        }
        catch (Exception ex)
        {
            RunLog.Warning(ex, $"Could not read Questionable's {subject}");
            return [];
        }
    }

    private static uint[] FindMatching(uint[] questIds, Func<object, bool> predicate)
    {
        if (!QuestionableServices.TryResolve(QuestRegistryTypeName, out var questRegistry, out _))
        {
            return [];
        }

        var tryGetQuest = questRegistry.GetType().GetMethod(TryGetQuestMethodName, ReflectionHelper.InstanceFlags);
        var parameters = tryGetQuest?.GetParameters();
        if (tryGetQuest is null || parameters is null || parameters.Length != TryGetQuestParameterCount)
        {
            return [];
        }

        var elementIdFromString = parameters[0].ParameterType.GetMethod(
            ElementIdFromStringMethodName, BindingFlags.Public | BindingFlags.Static, [typeof(string)]);
        if (elementIdFromString is null)
        {
            return [];
        }

        var matching = new List<uint>(questIds.Length);
        for (var questIndex = 0; questIndex < questIds.Length; questIndex++)
        {
            var questId = questIds[questIndex];
            var arguments = new[] { elementIdFromString.Invoke(null, [QuestionableIPC.Compact(questId)]), null };
            if (tryGetQuest.Invoke(questRegistry, arguments) is not true || arguments[1] is not { } quest)
            {
                continue;
            }

            if (predicate(quest))
            {
                matching.Add(questId);
            }
        }
        return [.. matching];
    }

    private static bool IsDisabled(object quest)
        => quest.GetFoP(RootPropertyName)?.GetFoP(DisabledPropertyName) is true;

    private static bool HasSiteBoundTurnIn(object quest)
    {
        if (FindSequenceSteps(quest, TribeStateReader.ReadyToTurnIn) is not { } steps)
        {
            return false;
        }

        for (var stepIndex = 0; stepIndex < steps.Count; stepIndex++)
        {
            var step = steps[stepIndex];
            if (step is null)
            {
                return false;
            }

            var interaction = step.GetFoP(InteractionTypePropertyName)?.ToString();
            if (interaction == CompleteQuestInteraction || interaction == GatherInteraction)
            {
                return false;
            }

            if (step.GetFoP(PositionPropertyName) is null)
            {
                continue;
            }

            return step.GetFoP(FlyPropertyName) is not true
                && step.GetFoP(AetheryteShortcutPropertyName) is null
                && step.GetFoP(AethernetShortcutPropertyName) is null
                && !SkipsItselfWhenMoot(step);
        }
        return false;
    }

    private static IList? FindSequenceSteps(object quest, int sequenceNumber)
    {
        if (quest.GetFoP(RootPropertyName)?.GetFoP(SequencesPropertyName) is not IList sequences)
        {
            return null;
        }

        for (var sequenceIndex = 0; sequenceIndex < sequences.Count; sequenceIndex++)
        {
            var sequence = sequences[sequenceIndex];
            if (sequence?.GetFoP(SequenceNumberPropertyName) is byte number && number == sequenceNumber)
            {
                return sequence.GetFoP(StepsPropertyName) as IList;
            }
        }
        return null;
    }

    private static bool SkipsItselfWhenMoot(object step)
        => step.GetFoP(SkipConditionsPropertyName)?.GetFoP(StepIfPropertyName) is { } stepIf
        && stepIf.Call(HasSkipConditionsMethodName, []) is true;
}
