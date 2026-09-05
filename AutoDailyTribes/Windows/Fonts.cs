using AutoDailyTribes.Core;
using AutoDailyTribes.Core.Localization;
using Dalamud;
using Dalamud.Interface;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Utility.Raii;
using ECommons.DalamudServices;
using System.IO;

namespace AutoDailyTribes.Windows;

internal static class Fonts
{
    private const float TitlePx = 24f;
    private const float HeadlinePx = 18f;
    private const float CaptionPx = 14f;
    private const float IconLargePx = 24f;
    private const float IconDisplayPx = 34f;

    private const string LatinFontFile = "NotoSans-Medium-Latin.ttf";
    private const int FirstNonAsciiCodepoint = 0x0080;
    private const int LatinBlocksEnd = 0x036F;
    private const int LatinAdditionalStart = 0x1E00;
    private const int LatinAdditionalEnd = 0x1EFF;

    private static readonly ushort[] LatinBlocks =
    [
        0x0020, 0x00FF,
        0x0100, 0x017F,
        0x0180, 0x024F,
        0x2000, 0x206F,
    ];

    private static readonly ushort[] SymbolBlocks =
    [
        0x2190, 0x21FF,
    ];

    private static readonly NoOpScope noOp = new();

    private static IFontAtlas? atlas;
    private static byte[]? latinFont;
    private static ushort[] latinRanges = [0];
    private static ushort[] mergeRanges = [0];

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
        RefreshRanges();

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

    public static void OnLanguageChanged()
    {
        RefreshRanges();
        if (atlas is not null) _ = atlas.BuildFontsAsync();
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

    // The game's AXIS font and Dalamud's Noto Sans CJK both stop at Latin-1, and merging a second font
    // only for the missing letters mixes two typefaces inside one word. The bundled Latin subset of
    // Noto Sans is therefore the primary font for every Latin letter, digit and punctuation mark, and
    // Noto Sans CJK only fills in the scripts it does not carry.
    private static byte[]? LoadLatinFont(string path)
    {
        try
        {
            if (File.Exists(path)) return File.ReadAllBytes(path);
            Svc.Log.Warning($"{AdtConstants.LogPrefix} Latin font missing at '{path}'; falling back to the Dalamud default font, Latin Extended letters will not render");
        }
        catch (Exception exception)
        {
            Svc.Log.Error(exception, $"{AdtConstants.LogPrefix} Failed to read the Latin font");
        }

        return null;
    }

    private static IFontHandle TextHandle(float sizePx)
        => atlas!.NewDelegateFontHandle(e => e.OnPreBuild(tk =>
        {
            var primary = latinFont is not null
                ? tk.AddFontFromMemory(latinFont, new SafeFontConfig { SizePx = sizePx, GlyphRanges = latinRanges }, LatinFontFile)
                : tk.AddDalamudDefaultFont(sizePx, latinRanges);
            tk.Font = primary;

            if (mergeRanges.Length <= 1) return;
            tk.AddDalamudAssetFont(DalamudAsset.NotoSansCjkRegular, new SafeFontConfig
            {
                SizePx = sizePx,
                GlyphRanges = mergeRanges,
                MergeFont = primary,
            });
        }));

    // The delegates above run again on every atlas rebuild, so refreshing these arrays and queueing a
    // rebuild is all a language switch needs to bake the new script's glyphs. Every language's native
    // name is always included so the language picker renders in any active language.
    private static void RefreshRanges()
    {
        var latin = new bool[char.MaxValue + 1];
        var merge = new bool[char.MaxValue + 1];
        var extra = new bool[char.MaxValue + 1];
        MarkRanges(latin, LatinBlocks);
        MarkRanges(merge, SymbolBlocks);
        MarkRanges(extra, Loc.Current.ExtraGlyphRanges);
        MarkRanges(extra, Loc.CatalogGlyphRanges);
        MarkNativeNames(extra);

        for (var codepoint = FirstNonAsciiCodepoint; codepoint <= char.MaxValue; codepoint++)
        {
            if (!extra[codepoint] || latin[codepoint]) continue;
            if (IsLatinCodepoint(codepoint)) latin[codepoint] = true;
            else merge[codepoint] = true;
        }

        latinRanges = ToRanges(latin);
        mergeRanges = ToRanges(merge);
    }

    private static bool IsLatinCodepoint(int codepoint)
        => codepoint <= LatinBlocksEnd || (codepoint >= LatinAdditionalStart && codepoint <= LatinAdditionalEnd);

    private static void MarkNativeNames(bool[] extra)
    {
        var languages = Languages.All;
        for (var languageIndex = 0; languageIndex < languages.Length; languageIndex++)
        {
            var name = languages[languageIndex].NativeName;
            for (var charIndex = 0; charIndex < name.Length; charIndex++)
            {
                var codepoint = name[charIndex];
                if (codepoint < FirstNonAsciiCodepoint || char.IsSurrogate(codepoint)) continue;
                extra[codepoint] = true;
            }
        }
    }

    private static void MarkRanges(bool[] target, ushort[]? ranges)
    {
        if (ranges is null) return;
        for (var index = 0; index + 1 < ranges.Length; index += 2)
        {
            if (ranges[index] == 0) return;
            for (int codepoint = ranges[index]; codepoint <= ranges[index + 1]; codepoint++) target[codepoint] = true;
        }
    }

    private static ushort[] ToRanges(bool[] present)
    {
        var ranges = new List<ushort>();
        var runStart = -1;
        for (var codepoint = 1; codepoint <= char.MaxValue; codepoint++)
        {
            if (present[codepoint])
            {
                if (runStart < 0) runStart = codepoint;
                continue;
            }

            if (runStart < 0) continue;
            ranges.Add((ushort)runStart);
            ranges.Add((ushort)(codepoint - 1));
            runStart = -1;
        }

        if (runStart >= 0)
        {
            ranges.Add((ushort)runStart);
            ranges.Add(char.MaxValue);
        }

        ranges.Add(0);
        return [.. ranges];
    }

    private sealed class NoOpScope : IDisposable
    {
        public void Dispose() { }
    }
}
