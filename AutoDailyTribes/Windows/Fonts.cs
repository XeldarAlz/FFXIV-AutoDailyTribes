using Dalamud;
using Dalamud.Interface;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Utility.Raii;
using ECommons.DalamudServices;
using System.IO;

namespace AutoDailyTribes.Windows;

// The game font and Dalamud's Noto Sans CJK both stop at Latin-1, so the bundled Latin subset of
// Noto Sans is the primary face for every letter, digit and punctuation mark the interface shows,
// and Noto Sans CJK only fills in the arrow glyphs the subset does not carry.
internal static class Fonts
{
    private const float TitlePx = 24f;
    private const float HeadlinePx = 18f;
    private const float CaptionPx = 14f;
    private const float IconLargePx = 24f;
    private const float IconDisplayPx = 34f;

    private const string LatinFontFile = "NotoSans-Medium-Latin.ttf";

    private static readonly ushort[] LatinRanges =
    [
        0x0020, 0x00FF,
        0x0100, 0x017F,
        0x0180, 0x024F,
        0x2000, 0x206F,
        0,
    ];

    private static readonly ushort[] SymbolRanges =
    [
        0x2190, 0x21FF,
        0,
    ];

    private static readonly NoOpScope noOp = new();

    private static IFontAtlas? atlas;
    private static byte[]? latinFont;

    private static IFontHandle? body;
    private static IFontHandle? title;
    private static IFontHandle? headline;
    private static IFontHandle? caption;
    private static IFontHandle? iconLarge;
    private static IFontHandle? iconDisplay;

    public static void Initialize(IUiBuilder uiBuilder, string pluginDirectory)
    {
        atlas = uiBuilder.FontAtlas;
        latinFont = LoadLatinFont(Path.Combine(pluginDirectory, "Fonts", LatinFontFile));

        body = TextHandle(UiBuilder.DefaultFontSizePx);
        title = TextHandle(TitlePx);
        headline = TextHandle(HeadlinePx);
        caption = TextHandle(CaptionPx);
        iconLarge = atlas.NewDelegateFontHandle(e => e.OnPreBuild(tk => tk.AddFontAwesomeIconFont(new SafeFontConfig { SizePx = IconLargePx })));
        iconDisplay = atlas.NewDelegateFontHandle(e => e.OnPreBuild(tk => tk.AddFontAwesomeIconFont(new SafeFontConfig { SizePx = IconDisplayPx })));

        if (atlas.AutoRebuildMode == FontAtlasAutoRebuildMode.Disable)
        {
            _ = atlas.BuildFontsAsync();
        }
    }

    public static void Dispose()
    {
        body?.Dispose();
        title?.Dispose();
        headline?.Dispose();
        caption?.Dispose();
        iconLarge?.Dispose();
        iconDisplay?.Dispose();
        body = title = headline = caption = iconLarge = iconDisplay = null;
        atlas = null;
        latinFont = null;
    }

    public static IDisposable PushBody() => body?.Push() ?? noOp;

    public static IDisposable PushTitle() => title?.Push() ?? noOp;

    public static IDisposable PushHeadline() => headline?.Push() ?? noOp;

    public static IDisposable PushCaption() => caption?.Push() ?? noOp;

    public static IDisposable PushIconLarge() => iconLarge?.Push() ?? ImRaii.PushFont(UiBuilder.IconFont);

    public static IDisposable PushIconDisplay() => iconDisplay?.Push() ?? ImRaii.PushFont(UiBuilder.IconFont);

    public static IDisposable PushIconFor(float unscaledPx)
    {
        if (unscaledPx >= 30f) return PushIconDisplay();
        if (unscaledPx >= 20f) return PushIconLarge();
        return ImRaii.PushFont(UiBuilder.IconFont);
    }

    private static byte[]? LoadLatinFont(string path)
    {
        try
        {
            if (File.Exists(path)) return File.ReadAllBytes(path);
            Svc.Log.Warning($"[ADT] Latin font missing at {path}; falling back to the Dalamud default font");
        }
        catch (Exception exception)
        {
            Svc.Log.Error(exception, "[ADT] Failed to read the Latin font");
        }

        return null;
    }

    private static IFontHandle TextHandle(float sizePx)
        => atlas!.NewDelegateFontHandle(e => e.OnPreBuild(tk =>
        {
            var primary = latinFont is not null
                ? tk.AddFontFromMemory(latinFont, new SafeFontConfig { SizePx = sizePx, GlyphRanges = LatinRanges }, LatinFontFile)
                : tk.AddDalamudDefaultFont(sizePx, LatinRanges);
            tk.Font = primary;

            tk.AddDalamudAssetFont(DalamudAsset.NotoSansCjkRegular, new SafeFontConfig
            {
                SizePx = sizePx,
                GlyphRanges = SymbolRanges,
                MergeFont = primary,
            });
        }));

    private sealed class NoOpScope : IDisposable
    {
        public void Dispose() { }
    }
}
