using ECommons.Reflection;

namespace AutoDailyTribes.Core.External;

internal static class QuestionableServices
{
    private const string PluginInternalName = "Questionable";
    private const string ServiceProviderFieldName = "_serviceProvider";

    public static bool TryResolve(string serviceTypeName, out object service, out string failure)
    {
        service = null!;

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
}
