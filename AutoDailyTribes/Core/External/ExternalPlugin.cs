using ECommons.DalamudServices;

namespace AutoDailyTribes.Core.External;

public enum ExternalPlugin
{
    Vnavmesh,
    Questionable,
    Artisan,
}

public sealed record ExternalPluginInfo(
    string InternalName,
    string DisplayName,
    string RepoUrl,
    string Purpose,
    bool Required);

public static class ExternalPlugins
{
    // Repo URLs as of 2026-05. plugins.carvel.li's cert is broken; PunishXIV
    // took over both Questionable and Artisan and serves them via the unified
    // puni.sh meta repo. vnavmesh stays on the per-author endpoint.
    public static readonly IReadOnlyDictionary<ExternalPlugin, ExternalPluginInfo> Catalog
        = new Dictionary<ExternalPlugin, ExternalPluginInfo>
    {
        [ExternalPlugin.Vnavmesh] = new(
            InternalName: "vnavmesh",
            DisplayName: "vnavmesh",
            RepoUrl: "https://puni.sh/api/repository/veyn",
            Purpose: "Pathfinding and walking to NPCs.",
            Required: true),
        [ExternalPlugin.Questionable] = new(
            InternalName: "Questionable",
            DisplayName: "Questionable",
            RepoUrl: "https://puni.sh/api/plugins",
            Purpose: "Plays out each daily quest after the plugin accepts it.",
            Required: true),
        [ExternalPlugin.Artisan] = new(
            InternalName: "Artisan",
            DisplayName: "Artisan",
            RepoUrl: "https://puni.sh/api/plugins",
            Purpose: "Crafter tribes — invoked by Questionable's internal pipeline.",
            Required: false),
    };

    public static IEnumerable<ExternalPlugin> All => Catalog.Keys;

    public static bool IsInstalled(ExternalPlugin plugin)
    {
        var info = Catalog[plugin];
        return Svc.PluginInterface.InstalledPlugins
            .Any(p => p.InternalName == info.InternalName && p.IsLoaded);
    }

    public static bool AllRequiredInstalled()
        => All.Where(p => Catalog[p].Required).All(IsInstalled);
}
