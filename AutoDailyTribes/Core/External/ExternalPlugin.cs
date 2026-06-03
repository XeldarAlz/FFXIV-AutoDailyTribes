using AutoDailyTribes.Core.Ipc;
using ECommons.DalamudServices;

namespace AutoDailyTribes.Core.External;

public enum ExternalPlugin
{
    Vnavmesh,
    Questionable,
    TextAdvance,
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
            RepoUrl: "https://puni.sh/api/plugins",
            Purpose: "Plays out each daily quest after the plugin accepts it.",
            Required: true),
        [ExternalPlugin.TextAdvance] = new(
            InternalName: "TextAdvance",
            DisplayName: "TextAdvance",
            RepoUrl: "https://raw.githubusercontent.com/NightmareXIV/MyDalamudPlugins/main/pluginmaster.json",
            Purpose: "Auto-advances quest dialogue and cutscenes so Questionable can complete each daily.",
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

    public static bool IsInstalledButDisabled(ExternalPlugin plugin)
        => plugin == ExternalPlugin.TextAdvance
           && IsInstalled(plugin)
           && !TextAdvanceIPC.IsPluginEnabled();
}
