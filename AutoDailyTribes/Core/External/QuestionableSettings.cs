using ECommons.DalamudServices;
using ECommons.Reflection;

namespace AutoDailyTribes.Core.External;

// Questionable's "Prevent quest completion" makes it walk to the turn-in NPC and stop without
// interacting, which silently burns tribe allowances. There is no IPC for it, so the live
// Configuration singleton is reached through Questionable's service provider instead.
internal static class QuestionableSettings
{
    private const string ConfigurationTypeName = "Questionable.Configuration";
    private const string AdvancedPropertyName = "Advanced";
    private const string PreventQuestCompletionPropertyName = "PreventQuestCompletion";

    public const string SettingDisplayName = "Prevent quest completion";

    private static object? borrowedAdvancedConfiguration;

    public static bool TryBorrowQuestCompletion(out string failure)
    {
        if (borrowedAdvancedConfiguration is not null)
        {
            failure = string.Empty;
            return true;
        }

        if (!TryResolveAdvancedConfiguration(out var advancedConfiguration, out failure)) return false;

        var property = advancedConfiguration.GetType()
            .GetProperty(PreventQuestCompletionPropertyName, ReflectionHelper.InstanceFlags);

        // Questionable builds that dropped the setting have nothing to borrow, and a run is safe.
        if (property is null || property.PropertyType != typeof(bool)) return true;

        if (!property.CanRead || !property.CanWrite)
        {
            failure = $"'{SettingDisplayName}' is not readable/writable";
            return false;
        }

        if (property.GetValue(advancedConfiguration) is not true) return true;

        property.SetValue(advancedConfiguration, false);
        borrowedAdvancedConfiguration = advancedConfiguration;
        Svc.Log.Info($"[ADT] Questionable's '{SettingDisplayName}' was on — disabled for this run, restoring afterwards.");
        return true;
    }

    public static void RestoreQuestCompletion()
    {
        if (borrowedAdvancedConfiguration is null) return;

        var advancedConfiguration = borrowedAdvancedConfiguration;
        borrowedAdvancedConfiguration = null;

        try
        {
            advancedConfiguration.GetType()
                .GetProperty(PreventQuestCompletionPropertyName, ReflectionHelper.InstanceFlags)
                ?.SetValue(advancedConfiguration, true);
            Svc.Log.Info($"[ADT] Restored Questionable's '{SettingDisplayName}'.");
        }
        catch (Exception ex)
        {
            Svc.Log.Warning(ex, $"[ADT] Could not restore Questionable's '{SettingDisplayName}'");
        }
    }

    private static bool TryResolveAdvancedConfiguration(out object advancedConfiguration, out string failure)
    {
        advancedConfiguration = null!;

        if (!QuestionableServices.TryResolve(ConfigurationTypeName, out var configuration, out failure)) return false;

        if (configuration.GetFoP(AdvancedPropertyName) is not { } advanced)
        {
            failure = $"Questionable's Configuration.{AdvancedPropertyName} was not found";
            return false;
        }

        advancedConfiguration = advanced;
        failure = string.Empty;
        return true;
    }
}
