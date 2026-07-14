using ECommons.DalamudServices;
using ECommons.Reflection;

namespace AutoDailyTribes.Core.External;

// Questionable's "Prevent quest completion" makes it walk to the turn-in NPC and stop without
// interacting, which silently burns tribe allowances. There is no IPC for it, so the live
// Configuration singleton is reached through Questionable's service provider instead.
internal static class QuestionableSettings
{
    private const string PluginInternalName = "Questionable";
    private const string ConfigurationTypeName = "Questionable.Configuration";
    private const string ServiceProviderFieldName = "_serviceProvider";
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

        // ignoreCache avoids ECommons' DalamudReflector module (and its per-frame plugin monitor),
        // which ADT does not initialise; this runs once per run, so the lookup cost is irrelevant.
        if (!DalamudReflector.TryGetDalamudPlugin(PluginInternalName, out var plugin, suppressErrors: true, ignoreCache: true))
        {
            failure = "Questionable's plugin instance is not reachable";
            return false;
        }

        if (plugin.GetFoP(ServiceProviderFieldName) is not IServiceProvider serviceProvider)
        {
            failure = $"Questionable's {ServiceProviderFieldName} was not found";
            return false;
        }

        var configurationType = plugin.GetType().Assembly.GetType(ConfigurationTypeName);
        if (configurationType is null)
        {
            failure = $"{ConfigurationTypeName} was not found";
            return false;
        }

        if (serviceProvider.GetService(configurationType) is not { } configuration)
        {
            failure = $"{ConfigurationTypeName} is not registered in Questionable's service provider";
            return false;
        }

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
