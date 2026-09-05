using AutoDailyTribes.Core.Localization;
using AutoDailyTribes.Core.Tasks;
using AutoDailyTribes.Core.Tribes;
using AutoDailyTribes.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoDailyTribes.Windows.Sections;

// Every tribe as a card, one expansion at a time behind a segmented picker. Kind chips in the header
// hide whole tribe types from both the grid and the run.
internal static class TribeLibrary
{
    private const float Gap = 8f;
    private const float SummaryRowHeight = 32f;
    private const float ListSlide = 8f;
    private const float ChipGap = 6f;
    private const float FilterGroupGap = 14f;

    private static readonly TribeEra[] ErasNewestFirst = [TribeEra.DT, TribeEra.EW, TribeEra.ShB, TribeEra.SB, TribeEra.HW, TribeEra.ARR];
    private static readonly TribeEra[] ErasOldestFirst = [TribeEra.ARR, TribeEra.HW, TribeEra.SB, TribeEra.ShB, TribeEra.EW, TribeEra.DT];
    private static readonly TribeKind[] Kinds = Enum.GetValues<TribeKind>();
    private static readonly string[] KindIds = BuildKindIds();
    private static readonly Segmented.Item[] segments = new Segmented.Item[ErasNewestFirst.Length];
    private static readonly List<TribeInfo> visible = [];

    private static TribeEra? selectedEra;

    public static void Draw(Configuration cfg, AutoTribeController ctrl)
    {
        var eras = cfg.ExpansionOrder == ExpansionOrder.OldestFirst ? ErasOldestFirst : ErasNewestFirst;
        var era = selectedEra ??= DefaultEra(eras);

        DrawHeader(cfg);
        Styling.VSpace(10f);
        era = DrawPicker(eras, era);
        Styling.VSpace(8f);

        using var reveal = Motion.PushSwitch("##adt_tribe_list", (int)era, slide: ListSlide);
        Collect(cfg, era);
        DrawSummary(cfg, era);
        DrawGrid(cfg, ctrl);
    }

    // The first expansion with something to run today, then the first with anything unlocked, so a
    // fresh session opens on the tribes that matter instead of always on the newest expansion.
    private static TribeEra DefaultEra(TribeEra[] eras)
    {
        for (var index = 0; index < eras.Length; index++)
        {
            if (Count(eras[index], static tribe => RunPlan.IsRunnable(tribe)) > 0) return eras[index];
        }

        for (var index = 0; index < eras.Length; index++)
        {
            if (Count(eras[index], static tribe => tribe.Unlocked) > 0) return eras[index];
        }

        return eras[0];
    }

    private static void DrawHeader(Configuration cfg)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var origin = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var height = Layout.LibraryHeaderHeight * scale;
        var midY = origin.Y + height * 0.5f;

        var title = Loc.T(L.Tribes.Library);
        var titleSize = TextDraw.SectionTitleSize(title);
        TextDraw.SectionTitle(title, new Vector2(origin.X, midY - titleSize.Y * 0.5f), Styling.TextStrong);

        var x = origin.X + width;
        var chipTop = midY - Layout.ChipHeight * scale * 0.5f;
        for (var kindIndex = Kinds.Length - 1; kindIndex >= 0; kindIndex--)
        {
            var kind = Kinds[kindIndex];
            if (!IsKindInPlay(cfg, kind)) continue;

            var label = Labels.Kind(kind);
            var chipWidth = PillButton.Width(label);
            x -= chipWidth;
            ImGui.SetCursorScreenPos(new Vector2(x, chipTop));

            var shown = !cfg.HiddenKinds.Contains(kind);
            var emphasis = shown ? PillButton.Emphasis.Tinted : PillButton.Emphasis.Ghost;
            var hint = shown ? Loc.T(L.Tribes.KindHide, label) : Loc.T(L.Tribes.KindShow, label);
            if (PillButton.Draw(KindIds[kindIndex], label, Styling.KindColor(kind), emphasis, height: Layout.ChipHeight, tooltip: hint))
            {
                if (shown) cfg.HiddenKinds.Add(kind);
                else cfg.HiddenKinds.Remove(kind);
                cfg.SaveDebounced();
            }

            x -= ChipGap * scale;
        }

        x -= FilterGroupGap * scale - ChipGap * scale;
        var maxedLabel = Loc.T(L.Tribes.NotMaxed);
        x -= PillButton.Width(maxedLabel);
        ImGui.SetCursorScreenPos(new Vector2(x, chipTop));
        var hideMaxed = cfg.HideMaxedTribes;
        if (PillButton.Draw("##adt_filter_not_maxed", maxedLabel, Styling.AccentTeal, hideMaxed ? PillButton.Emphasis.Tinted : PillButton.Emphasis.Ghost,
                height: Layout.ChipHeight, tooltip: Loc.T(hideMaxed ? L.Tribes.NotMaxedOn : L.Tribes.NotMaxedOff)))
        {
            cfg.HideMaxedTribes = !hideMaxed;
            cfg.SaveDebounced();
        }

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(new Vector2(width, height));
    }

    private static bool IsKindInPlay(Configuration cfg, TribeKind kind)
    {
        if (cfg.HiddenKinds.Contains(kind)) return true;

        var tribes = TribeRegistry.Tribes;
        for (var index = 0; index < tribes.Length; index++)
        {
            if (tribes[index].Kind == kind && tribes[index].Unlocked) return true;
        }

        return false;
    }

    private static TribeEra DrawPicker(TribeEra[] eras, TribeEra current)
    {
        var selected = 0;
        for (var index = 0; index < eras.Length; index++)
        {
            segments[index] = new Segmented.Item(null, Labels.Era(eras[index]));
            if (eras[index] == current) selected = index;
        }

        if (!Segmented.Draw("##adt_expansions", segments, ref selected)) return current;

        selectedEra = eras[selected];
        return eras[selected];
    }

    private static void Collect(Configuration cfg, TribeEra era)
    {
        visible.Clear();
        var tribes = TribeRegistry.ByEra(era);
        AddWhere(cfg, tribes, static tribe => RunPlan.IsRunnable(tribe));
        AddWhere(cfg, tribes, static tribe => tribe.Unlocked && tribe.MeetsRankRequirement && !RunPlan.IsRunnable(tribe));
        AddWhere(cfg, tribes, static tribe => tribe.Unlocked && !tribe.MeetsRankRequirement);
        AddWhere(cfg, tribes, static tribe => !tribe.Unlocked);
    }

    private static void AddWhere(Configuration cfg, TribeInfo[] tribes, Func<TribeInfo, bool> predicate)
    {
        for (var index = 0; index < tribes.Length; index++)
        {
            var tribe = tribes[index];
            if (predicate(tribe) && RunPlan.PassesFilters(cfg, tribe)) visible.Add(tribe);
        }
    }

    private static void DrawSummary(Configuration cfg, TribeEra era)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var origin = ImGui.GetCursorScreenPos();
        var avail = ImGui.GetContentRegionAvail().X;
        var rowHeight = SummaryRowHeight * scale;

        using (Fonts.PushCaption())
        {
            var summary = EraSummary(cfg, era);
            var summarySize = TextDraw.Measure(summary);
            TextDraw.At(summary, new Vector2(origin.X + 2f * scale, origin.Y + (rowHeight - summarySize.Y) * 0.5f), Styling.TextDim);
        }

        ImGui.Dummy(new Vector2(avail, rowHeight));
        if (visible.Count > 0) return;

        Styling.VSpace(10f);
        Styling.TextCentered(Loc.T(L.Tribes.EmptyFiltered), Styling.TextMuted);
        Styling.VSpace(10f);
    }

    private static string EraSummary(Configuration cfg, TribeEra era)
    {
        var tribes = TribeRegistry.ByEra(era);
        var unlocked = 0;
        var ready = 0;
        var picked = 0;
        for (var index = 0; index < tribes.Length; index++)
        {
            var tribe = tribes[index];
            if (tribe.Unlocked) unlocked++;
            if (RunPlan.IsRunnable(tribe)) ready++;
            if (cfg.SelectedTribes.Contains(tribe.BeastTribeId)) picked++;
        }

        return Loc.T(L.Tribes.EraSummary, unlocked, tribes.Length, ready, picked);
    }

    private static int Count(TribeEra era, Func<TribeInfo, bool> predicate)
    {
        var tribes = TribeRegistry.ByEra(era);
        var count = 0;
        for (var index = 0; index < tribes.Length; index++)
        {
            if (predicate(tribes[index])) count++;
        }

        return count;
    }

    private static void DrawGrid(Configuration cfg, AutoTribeController ctrl)
    {
        if (visible.Count == 0) return;

        var scale = ImGuiHelpers.GlobalScale;
        var gap = Gap * scale;
        var avail = ImGui.GetContentRegionAvail().X;
        var columns = Math.Max(1, (int)MathF.Floor((avail + gap) / (Layout.TribeCardMinWidth * scale + gap)));
        var cardWidth = (avail - gap * (columns - 1)) / columns;

        using var itemSpacing = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(gap, gap));
        for (var index = 0; index < visible.Count; index++)
        {
            if (index % columns != 0) ImGui.SameLine(0f, gap);
            var tribe = visible[index];
            TribeCard.Draw(tribe, cfg, ctrl, cardWidth, QueuePosition(cfg, tribe.BeastTribeId));
        }
    }

    private static int QueuePosition(Configuration cfg, uint beastTribeId)
    {
        var position = 0;
        var selected = cfg.SelectedTribes;
        for (var index = 0; index < selected.Count; index++)
        {
            if (RunPlan.Find(selected[index]) is null) continue;
            position++;
            if (selected[index] == beastTribeId) return position;
        }

        return 0;
    }

    private static string[] BuildKindIds()
    {
        var ids = new string[Kinds.Length];
        for (var index = 0; index < ids.Length; index++) ids[index] = $"##adt_kind_{Kinds[index]}";
        return ids;
    }
}
