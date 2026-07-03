using AutoDailyTribes.Core.Tribes;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using ECommons.DalamudServices;
using System.IO;
using System.Numerics;

namespace AutoDailyTribes.Windows.Components;

internal static class TribeIcon
{
    private static readonly string TribesRoot = Path.Combine(Assets.ImagesRoot, "Tribes");

    private static readonly Dictionary<string, string?> ResolvedPaths = new();

    public static void Draw(TribeInfo tribe, float size = 0)
    {
        if (size <= 0) size = ImGui.GetTextLineHeight() * 1.6f;

        if (tribe.IconFile is { Length: > 0 } file && ResolvePath(file) is { } path)
        {
            var tex = Svc.Texture.GetFromFile(path).GetWrapOrEmpty();
            if (tex != null)
            {
                ImGui.Image(tex.Handle, new Vector2(size, size));
                return;
            }
        }

        var cursor = ImGui.GetCursorPos();
        KindIcon.Draw(tribe.Kind);
        ImGui.SetCursorPos(cursor + new Vector2(size, 0));
    }

    private static string? ResolvePath(string file)
    {
        if (ResolvedPaths.TryGetValue(file, out var cached)) return cached;
        var path = Path.Combine(TribesRoot, file);
        var resolved = File.Exists(path) ? path : null;
        ResolvedPaths[file] = resolved;
        return resolved;
    }
}
