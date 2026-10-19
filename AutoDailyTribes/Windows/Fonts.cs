using AutoDailyTribes.Core;
using AutoDailyTribes.Core.Localization;
using AutoDailyTribes.Core.Tribes;
using Dalamud;
using Dalamud.Game;
using Dalamud.Interface;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ECommons.DalamudServices;
using Lumina.Excel.Sheets;
using System.IO;

namespace AutoDailyTribes.Windows;

internal static class Fonts
{
    // Every tier is a multiple of the font size chosen in Dalamud settings, so the shell follows that
    // setting instead of pinning pixels. Caption equals the Dalamud size, so nothing here renders
    // smaller than any other plugin's body text.
    private const float CaptionScale = 1.0f;
    private const float BodyScale = 1.125f;
    private const float HeadlineScale = 1.25f;
    private const float TitleScale = 1.625f;
    private const float IconScale = BodyScale;
    private const float IconLargeScale = 1.625f;
    private const float IconDisplayScale = 2.25f;

    private const string LatinFontFile = "NotoSans-Medium-Latin.ttf";
    private const int LatinBlocksEnd = 0x036F;
    private const int LatinAdditionalStart = 0x1E00;
    private const int LatinAdditionalEnd = 0x1EFF;

    // Face indices inside Dalamud's NotoSansCJK-Regular.ttc collection.
    private const int NotoCjkJapaneseFace = 0;
    private const int NotoCjkSimplifiedChineseFace = 2;

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
        0x2200, 0x22FF,
    ];

    private static readonly NoOpScope noOp = new();

    private static IUiBuilder? builder;
    private static IFontAtlas? atlas;
    private static byte[]? latinFont;
    private static ushort[] latinRanges = [0];
    private static ushort[] mergeRanges = [0];
    private static ushort[]? questGlyphRanges;
    private static int cjkFace = NotoCjkJapaneseFace;
    private static float unitPx = UiBuilder.DefaultFontSizePx;

    private static IFontHandle? body;
    private static IFontHandle? title;
    private static IFontHandle? headline;
    private static IFontHandle? caption;
    private static IFontHandle? icon;
    private static IFontHandle? iconLarge;
    private static IFontHandle? iconDisplay;

    public static void Initialize(IUiBuilder uiBuilder, string pluginDirectory)
    {
        builder = uiBuilder;
        atlas = uiBuilder.FontAtlas;
        latinFont = LoadLatinFont(Path.Combine(pluginDirectory, "Fonts", LatinFontFile));
        RefreshRanges();

        body = TextHandle(BodyScale);
        title = TextHandle(TitleScale);
        headline = TextHandle(HeadlineScale);
        caption = TextHandle(CaptionScale);
        icon = IconHandle(IconScale);
        iconLarge = IconHandle(IconLargeScale);
        iconDisplay = IconHandle(IconDisplayScale);
        uiBuilder.DefaultFontChanged += Rebuild;

        if (atlas.AutoRebuildMode == FontAtlasAutoRebuildMode.Disable)
        {
            _ = atlas.BuildFontsAsync();
        }
    }

    public static void OnLanguageChanged() => Rebuild();

    public static void Dispose()
    {
        if (builder is not null)
        {
            builder.DefaultFontChanged -= Rebuild;
        }

        body?.Dispose();
        title?.Dispose();
        headline?.Dispose();
        caption?.Dispose();
        icon?.Dispose();
        iconLarge?.Dispose();
        iconDisplay?.Dispose();
        body = title = headline = caption = icon = iconLarge = iconDisplay = null;
        builder = null;
        atlas = null;
        latinFont = null;
        questGlyphRanges = null;
    }

    public static IDisposable PushBody() => body?.Push() ?? noOp;

    public static IDisposable PushTitle() => title?.Push() ?? noOp;

    public static IDisposable PushHeadline() => headline?.Push() ?? noOp;

    public static IDisposable PushCaption() => caption?.Push() ?? noOp;

    public static IDisposable PushIcon() => icon?.Push() ?? ImRaii.PushFont(UiBuilder.IconFont);

    public static IDisposable PushIconLarge() => iconLarge?.Push() ?? ImRaii.PushFont(UiBuilder.IconFont);

    public static IDisposable PushIconDisplay() => iconDisplay?.Push() ?? ImRaii.PushFont(UiBuilder.IconFont);

    // The smallest tier that still covers the target, so a glyph fitted to a shape only ever shrinks.
    public static IDisposable PushIconFor(float targetHeight)
    {
        var unit = unitPx * ImGuiHelpers.GlobalScale;
        if (targetHeight > unit * IconLargeScale)
        {
            return PushIconDisplay();
        }

        if (targetHeight > unit * IconScale)
        {
            return PushIconLarge();
        }

        return PushIcon();
    }

    // The game's AXIS font and Dalamud's Noto Sans CJK both stop at Latin-1, and merging a second font
    // only for the missing letters mixes two typefaces inside one word. The bundled Latin subset of
    // Noto Sans is therefore the primary font for every Latin letter, digit and punctuation mark, and
    // Noto Sans CJK only fills in the scripts it does not carry.
    private static byte[]? LoadLatinFont(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                return File.ReadAllBytes(path);
            }

            RunLog.Warning($"Latin font missing at '{path}'; falling back to the Dalamud default font, Latin Extended letters will not render");
        }
        catch (Exception exception)
        {
            RunLog.Error(exception, $"Failed to read the Latin font");
        }

        return null;
    }

    private static IFontHandle TextHandle(float scale)
        => atlas!.NewDelegateFontHandle(e => e.OnPreBuild(tk =>
        {
            var sizePx = unitPx * scale;
            var primary = latinFont is not null
                ? tk.AddFontFromMemory(latinFont, new SafeFontConfig { SizePx = sizePx, GlyphRanges = latinRanges }, LatinFontFile)
                : tk.AddDalamudDefaultFont(sizePx, latinRanges);
            tk.Font = primary;

            // Quest names can begin with game-specific characters such as the synced-quest icon.
            // Add game symbols before the CJK face so its private-use glyphs keep priority.
            tk.AddGameSymbol(new SafeFontConfig
            {
                SizePx = sizePx,
                MergeFont = primary,
            });

            if (mergeRanges.Length > 1)
            {
                tk.AddDalamudAssetFont(DalamudAsset.NotoSansCjkRegular, new SafeFontConfig
                {
                    SizePx = sizePx,
                    GlyphRanges = mergeRanges,
                    MergeFont = primary,
                    FontNo = cjkFace,
                });
            }
        }));

    private static IFontHandle IconHandle(float scale)
        => atlas!.NewDelegateFontHandle(e => e.OnPreBuild(tk => tk.AddFontAwesomeIconFont(new SafeFontConfig { SizePx = unitPx * scale })));

    private static void Rebuild()
    {
        RefreshRanges();
        if (atlas is not null)
        {
            _ = atlas.BuildFontsAsync();
        }
    }

    // The delegates above run again on every atlas rebuild, so refreshing these values and queueing a
    // rebuild is all a language switch or a Dalamud font change needs. Every language's native name is
    // always included so the language picker renders in any active language.
    private static void RefreshRanges()
    {
        unitPx = builder?.DefaultFontSpec.SizePx ?? UiBuilder.DefaultFontSizePx;

        var latin = new bool[GlyphRanges.CodepointCount];
        var merge = new bool[GlyphRanges.CodepointCount];
        var extra = new bool[GlyphRanges.CodepointCount];
        GlyphRanges.MarkRanges(latin, LatinBlocks);
        GlyphRanges.MarkRanges(merge, SymbolBlocks);
        GlyphRanges.MarkRanges(extra, Loc.Current.ExtraGlyphRanges);
        GlyphRanges.MarkRanges(extra, Loc.CatalogGlyphRanges);
        GlyphRanges.MarkRanges(extra, questGlyphRanges ??= ScanQuestGlyphRanges());
        MarkNativeNames(extra);
        MarkTribeNames(extra);

        for (var codepoint = GlyphRanges.FirstNonAsciiCodepoint; codepoint < GlyphRanges.CodepointCount; codepoint++)
        {
            if (latin[codepoint] || !extra[codepoint])
            {
                continue;
            }

            if (IsLatinCodepoint(codepoint))
            {
                latin[codepoint] = true;
            }
            else
            {
                merge[codepoint] = true;
            }
        }

        latinRanges = GlyphRanges.ToRanges(latin);
        mergeRanges = GlyphRanges.ToRanges(merge);
        // The game data language can differ from the plugin UI language. Give Japanese and CN
        // game text the matching glyph variants; CN uses language value 4 in Dalamud.
        cjkFace = Svc.Data.Language switch
        {
            ClientLanguage.Japanese => NotoCjkJapaneseFace,
            (ClientLanguage)4 => NotoCjkSimplifiedChineseFace,
            _ => ReferenceEquals(Loc.Current, Languages.Chinese)
                ? NotoCjkSimplifiedChineseFace : NotoCjkJapaneseFace,
        };
    }

    // Quest names come from the game sheet, not our translation catalog. Include the names of
    // every tribe quest so a newly accepted daily or rank-up can render without another rebuild.
    private static ushort[]? ScanQuestGlyphRanges()
    {
        var quests = Svc.Data.GetExcelSheet<Quest>();

        var present = new bool[GlyphRanges.CodepointCount];
        for (var index = 0; index < quests.Count; index++)
        {
            var quest = quests.GetRowAt(index);
            var tribeId = quest.BeastTribe.RowId;
            if (tribeId == 0 || tribeId > TribeRegistry.HighestTribeId)
            {
                continue;
            }

            GlyphRanges.MarkText(present, quest.Name.ToString());
        }

        return GlyphRanges.ToRanges(present);
    }

    private static bool IsLatinCodepoint(int codepoint)
        => codepoint <= LatinBlocksEnd || (codepoint >= LatinAdditionalStart && codepoint <= LatinAdditionalEnd);

    private static void MarkNativeNames(bool[] extra)
    {
        var languages = Languages.All;
        for (var languageIndex = 0; languageIndex < languages.Length; languageIndex++)
        {
            GlyphRanges.MarkText(extra, languages[languageIndex].NativeName);
        }
    }

    private static void MarkTribeNames(bool[] extra)
    {
        var tribes = TribeRegistry.Tribes;
        for (var index = 0; index < tribes.Length; index++)
        {
            GlyphRanges.MarkText(extra, tribes[index].Name);
        }
    }

    private sealed class NoOpScope : IDisposable
    {
        public void Dispose() { }
    }
}
