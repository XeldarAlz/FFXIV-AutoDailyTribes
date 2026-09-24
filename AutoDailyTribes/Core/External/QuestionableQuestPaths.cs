using AutoDailyTribes.Core.Ipc;
using ECommons.DalamudServices;
using ECommons.Reflection;
using System.Reflection;

namespace AutoDailyTribes.Core.External;

internal static class QuestionableQuestPaths
{
    private const string QuestRegistryTypeName = "Questionable.Controller.QuestRegistry";
    private const string TryGetQuestMethodName = "TryGetQuest";
    private const string ElementIdFromStringMethodName = "FromString";
    private const string RootPropertyName = "Root";
    private const string DisabledPropertyName = "Disabled";
    private const int TryGetQuestParameterCount = 2;

    public static uint[] DisabledAmong(uint[] questIds)
    {
        if (questIds.Length == 0)
        {
            return [];
        }

        try
        {
            return FindDisabled(questIds);
        }
        catch (Exception ex)
        {
            RunLog.Warning(ex, "Could not read Questionable's disabled quest paths");
            return [];
        }
    }

    private static uint[] FindDisabled(uint[] questIds)
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

        var disabled = new List<uint>(questIds.Length);
        for (var questIndex = 0; questIndex < questIds.Length; questIndex++)
        {
            var questId = questIds[questIndex];
            var arguments = new[] { elementIdFromString.Invoke(null, [QuestionableIPC.Compact(questId)]), null };
            if (tryGetQuest.Invoke(questRegistry, arguments) is not true || arguments[1] is not { } quest)
            {
                continue;
            }

            if (quest.GetFoP(RootPropertyName)?.GetFoP(DisabledPropertyName) is true)
            {
                disabled.Add(questId);
            }
        }
        return [.. disabled];
    }
}
