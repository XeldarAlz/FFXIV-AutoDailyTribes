using ECommons.DalamudServices;
using ECommons.Reflection;
using System.Threading.Tasks;

namespace AutoTribeQuests.Core.External;

// Wraps ECommons' DalamudReflector.AddPlugin — same pattern AutoDuty's
// PluginInstaller helper uses. Adds the repo URL if it isn't already present
// and triggers a Dalamud-internal InstallPluginAsync.
//
// Maintains an in-flight set so the UI can show a "Installing..." button state
// without overlapping installs.
public static class PluginInstaller
{
    private static readonly HashSet<ExternalPlugin> InFlight = [];

    public static bool IsInstalling(ExternalPlugin plugin) => InFlight.Contains(plugin);

    public static async Task<bool> Install(ExternalPlugin plugin)
    {
        if (!InFlight.Add(plugin)) return false;
        try
        {
            var info = ExternalPlugins.Catalog[plugin];
            Svc.Log.Info($"[ExternalPlugin] Installing {info.DisplayName} from {info.RepoUrl}");
            var ok = await DalamudReflector.AddPlugin(info.RepoUrl, info.InternalName);
            Svc.Log.Info(ok
                ? $"[ExternalPlugin] {info.DisplayName} installed."
                : $"[ExternalPlugin] {info.DisplayName} install reported failure — repo may need to be added manually.");
            return ok;
        }
        catch (Exception ex)
        {
            Svc.Log.Warning(ex, "[ExternalPlugin] install threw");
            return false;
        }
        finally
        {
            InFlight.Remove(plugin);
        }
    }
}
