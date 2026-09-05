using AutoDailyTribes.Core.Tribes;
using Dalamud.Bindings.ImGui;
using ECommons.DalamudServices;
using System.IO;
using System.Numerics;

namespace AutoDailyTribes.Windows.Components;

internal static class TribeIcon
{
    private static readonly string TribesRoot = Path.Combine(Assets.ImagesRoot, "Tribes");
    private static readonly Dictionary<string, string?> resolvedPaths = new(StringComparer.Ordinal);

    public static void Draw(ImDrawListPtr dl, TribeInfo tribe, Vector2 min, float size, float alpha = 1f)
    {
        var path = Resolve(tribe.IconFile);
        if (path is not null)
        {
            var texture = Svc.Texture.GetFromFile(path).GetWrapOrEmpty();
            dl.AddImage(texture.Handle, min, min + new Vector2(size, size), Vector2.Zero, Vector2.One, Paint.Col(new Vector4(1f, 1f, 1f, alpha)));
            return;
        }

        var center = min + new Vector2(size * 0.5f, size * 0.5f);
        ProgressRing.CenterIcon(center, KindIcon.Icon(tribe.Kind), Styling.WithAlpha(Styling.KindColor(tribe.Kind), alpha), size * 0.6f);
    }

    private static string? Resolve(string? file)
    {
        if (string.IsNullOrEmpty(file)) return null;
        if (resolvedPaths.TryGetValue(file, out var cached)) return cached;

        var path = Path.Combine(TribesRoot, file);
        var resolved = File.Exists(path) ? path : null;
        resolvedPaths[file] = resolved;
        return resolved;
    }
}
