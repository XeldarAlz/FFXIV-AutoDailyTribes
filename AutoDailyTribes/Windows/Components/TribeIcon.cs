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

    public static void Draw(TribeInfo tribe, float size = 0)
    {
        if (size <= 0) size = ImGui.GetTextLineHeight() * 1.6f;

        if (tribe.IconFile is { Length: > 0 } file)
        {
            var path = Path.Combine(TribesRoot, file);
            if (File.Exists(path))
            {
                var tex = Svc.Texture.GetFromFile(path).GetWrapOrEmpty();
                if (tex != null)
                {
                    ImGui.Image(tex.Handle, new Vector2(size, size));
                    return;
                }
            }
        }

        var cursor = ImGui.GetCursorPos();
        KindIcon.Draw(tribe.Kind);
        ImGui.SetCursorPos(cursor + new Vector2(size, 0));
    }
}
