using ECommons.DalamudServices;

namespace AutoTribeQuests.Core.External;

// Catalog of plugins this one depends on.
//
// Adding a new dependency = add an enum value + matching Catalog entry. The
// AboutWindow's deps grid + the install pipeline pick it up automatically.
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
            RepoUrl: "https://plugins.carvel.li/",
            Purpose: "Plays out each daily quest after the plugin accepts it.",
            Required: true),
        [ExternalPlugin.Artisan] = new(
            InternalName: "Artisan",
            DisplayName: "Artisan",
            RepoUrl: "https://love.puni.sh/ment.json",
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
