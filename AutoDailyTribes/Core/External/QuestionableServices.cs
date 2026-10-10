using ECommons.Reflection;

namespace AutoDailyTribes.Core.External;

internal static class QuestionableServices
{
    // WigglyQuest is a Questionable fork that keeps Questionable's type names, so every lookup works on
    // either. It comes first because it is also the one QuestionableIPC drives when both are loaded.
    private static readonly string[] PluginInternalNames = ["WigglyQuest", "Questionable"];
    private const string ServiceProviderFieldName = "_serviceProvider";

    public static bool TryResolve(string serviceTypeName, out object service, out string failure)
    {
        service = null!;

        if (!TryGetPlugin(out var plugin))
        {
            failure = "Questionable's plugin instance is not reachable";
            return false;
        }

        if (plugin.GetFoP(ServiceProviderFieldName) is not IServiceProvider serviceProvider)
        {
            failure = $"Questionable's {ServiceProviderFieldName} was not found";
            return false;
        }

        var serviceType = plugin.GetType().Assembly.GetType(serviceTypeName);
        if (serviceType is null)
        {
            failure = $"{serviceTypeName} was not found";
            return false;
        }

        if (serviceProvider.GetService(serviceType) is not { } resolved)
        {
            failure = $"{serviceTypeName} is not registered in Questionable's service provider";
            return false;
        }

        service = resolved;
        failure = string.Empty;
        return true;
    }

    private static bool TryGetPlugin(out object plugin)
    {
        // ignoreCache avoids ECommons' DalamudReflector module (and its per-frame plugin monitor),
        // which ADT does not initialise; this runs once per run, so the lookup cost is irrelevant.
        for (var nameIndex = 0; nameIndex < PluginInternalNames.Length; nameIndex++)
        {
            if (DalamudReflector.TryGetDalamudPlugin(PluginInternalNames[nameIndex], out plugin, suppressErrors: true, ignoreCache: true))
            {
                return true;
            }
        }

        plugin = null!;
        return false;
    }
}
