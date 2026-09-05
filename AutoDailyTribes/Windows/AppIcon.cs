using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using ECommons.DalamudServices;
using System.IO;
using System.Numerics;

namespace AutoDailyTribes.Windows;

internal static class AppIcon
{
    private const string FileName = "Icon.png";

    private static string? path;
    private static bool? exists;

    public static void Draw(ImDrawListPtr dl, Vector2 min, Vector2 max, float rounding, float alpha = 1f)
    {
        path ??= Path.Combine(Assets.ImagesRoot, FileName);
        exists ??= File.Exists(path);

        if (exists.Value)
        {
            var texture = Svc.Texture.GetFromFile(path).GetWrapOrEmpty();
            dl.AddImageRounded(texture.Handle, min, max, Vector2.Zero, Vector2.One,
                Paint.Col(new Vector4(1f, 1f, 1f, alpha)), rounding, ImDrawFlags.RoundCornersAll);
            return;
        }

        Paint.Gradient(dl, min, max, Styling.AccentTealSoft, Styling.AccentTeal, rounding);
        TextDraw.IconCentered(FontAwesomeIcon.Handshake, (min + max) * 0.5f, Styling.WithAlpha(Styling.TextStrong, alpha));
    }
}
