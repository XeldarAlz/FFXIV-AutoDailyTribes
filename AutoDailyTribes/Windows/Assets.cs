using ECommons.DalamudServices;
using System.IO;

namespace AutoDailyTribes.Windows;

internal static class Assets
{
    public static readonly string ImagesRoot = Path.Combine(
        Svc.PluginInterface.AssemblyLocation.DirectoryName ?? "",
        "Images");
}
