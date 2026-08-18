using AutoDailyTribes.Core.Tasks;
using AutoDailyTribes.Core.Tribes;
using AutoDailyTribes.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;
using System.Text;

namespace AutoDailyTribes.Windows.Sections;

internal static class TribeList
{
    private static readonly TribeEra[] ErasOldestFirst = Enum.GetValues<TribeEra>();
    private static readonly TribeEra[] ErasNewestFirst = BuildNewestFirst();
    private static readonly string[] EraHeaders = BuildEraHeaders();
    private static readonly string[] EraHeaderIds = BuildIds("##era_");
    private static readonly string[] EraGridIds = BuildIds("##grid_");

    private static readonly List<TribeInfo> Cards = [];
    private static readonly List<TribeInfo> UnderRank = [];
    private static readonly List<TribeEra> LockedEras = [];

    private static int lockedEraSignature = -1;
    private static string lockedEraNames = string.Empty;

    public static bool IsRunnable(TribeInfo tribe)
        => tribe.Unlocked && tribe.MeetsRankRequirement && (tribe.AcceptSlotsRemaining > 0 || tribe.HasInProgressQuests);

    public static void Draw(AutoTribeController controller, Configuration cfg)
    {
        var eras = cfg.ExpansionOrder == ExpansionOrder.OldestFirst ? ErasOldestFirst : ErasNewestFirst;
        LockedEras.Clear();

        var populatedEras = 0;
        var drawnEras = 0;

        for (var eraIndex = 0; eraIndex < eras.Length; eraIndex++)
        {
            var era = eras[eraIndex];
            var tribes = TribeRegistry.ByEra(era);
            if (tribes.Length == 0) continue;

            populatedEras++;

            if (IsFullyLocked(tribes))
            {
                LockedEras.Add(era);
                if (cfg.HideLockedExpansions) continue;
            }

            if (DrawEra(era, tribes, controller, cfg)) drawnEras++;
        }

        if (drawnEras == 0) DrawEmptyState(LockedEras.Count == populatedEras);
        if (LockedEras.Count > 0) DrawLockedExpansions(cfg);
    }

    private static bool IsFullyLocked(TribeInfo[] tribes)
    {
        for (var tribeIndex = 0; tribeIndex < tribes.Length; tribeIndex++)
        {
            if (tribes[tribeIndex].Unlocked) return false;
        }
        return true;
    }

    private static bool DrawEra(TribeEra era, TribeInfo[] tribes, AutoTribeController controller, Configuration cfg)
    {
        Cards.Clear();
        UnderRank.Clear();

        var readyCount = 0;
        for (var tribeIndex = 0; tribeIndex < tribes.Length; tribeIndex++)
        {
            var tribe = tribes[tribeIndex];
            if (!FilterBar.PassesKindFilter(cfg, tribe)) continue;
            if (!IsRunnable(tribe)) continue;
            Cards.Add(tribe);
            readyCount++;
        }

        if (!cfg.ShowReadyOnly)
        {
            for (var tribeIndex = 0; tribeIndex < tribes.Length; tribeIndex++)
            {
                var tribe = tribes[tribeIndex];
                if (!FilterBar.PassesKindFilter(cfg, tribe)) continue;
                if (tribe.Unlocked && tribe.MeetsRankRequirement && !IsRunnable(tribe)) Cards.Add(tribe);
            }

            for (var tribeIndex = 0; tribeIndex < tribes.Length; tribeIndex++)
            {
                var tribe = tribes[tribeIndex];
                if (!FilterBar.PassesKindFilter(cfg, tribe)) continue;
                if (!tribe.Unlocked) Cards.Add(tribe);
            }

            for (var tribeIndex = 0; tribeIndex < tribes.Length; tribeIndex++)
            {
                var tribe = tribes[tribeIndex];
                if (!FilterBar.PassesKindFilter(cfg, tribe)) continue;
                if (tribe.Unlocked && !tribe.MeetsRankRequirement) UnderRank.Add(tribe);
            }
        }

        if (Cards.Count == 0 && UnderRank.Count == 0) return false;

        var collapsed = cfg.CollapsedEras.Contains(era);
        if (SectionHeader(era, readyCount, Cards.Count + UnderRank.Count, collapsed))
        {
            if (collapsed) cfg.CollapsedEras.Remove(era);
            else cfg.CollapsedEras.Add(era);
            cfg.SaveDebounced();
            collapsed = !collapsed;
        }

        if (collapsed)
        {
            Styling.VSpace(7);
            return true;
        }

        Styling.VSpace(2);
        if (Cards.Count > 0) DrawGrid(era, controller, cfg);

        if (UnderRank.Count > 0)
        {
            if (Cards.Count > 0) Styling.VSpace(3);
            DrawChipFlow();
        }

        Styling.VSpace(9);
        return true;
    }

    private static bool SectionHeader(TribeEra era, int readyCount, int totalCount, bool collapsed)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var lineHeight = ImGui.GetTextLineHeight();
        var origin = ImGui.GetCursorScreenPos();
        var width = MathF.Max(1f, ImGui.GetContentRegionAvail().X);

        var toggled = ImGui.InvisibleButton(EraHeaderIds[(int)era], new Vector2(width, lineHeight));
        var hovered = ImGui.IsItemHovered();
        if (hovered) ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

        DrawChevron(new Vector2(origin.X + 4f * scale, origin.Y + lineHeight * 0.5f), 4f * scale,
            collapsed, hovered ? Styling.TextSecondary : Styling.TextMuted);

        ImGui.SetCursorScreenPos(new Vector2(origin.X + 15f * scale, origin.Y));
        using (ImRaii.PushColor(ImGuiCol.Text, hovered ? Styling.TextStrong : Styling.TextSecondary))
            ImGui.TextUnformatted(EraHeaders[(int)era]);
        var textEnd = ImGui.GetItemRectMax().X;

        if (readyCount > 0)
        {
            ImGui.SameLine(0, 7f * scale);
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.AccentTeal))
                ImGui.TextUnformatted($"{readyCount} ready");
            textEnd = ImGui.GetItemRectMax().X;
        }

        if (collapsed)
        {
            ImGui.SameLine(0, 7f * scale);
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextMuted))
                ImGui.TextUnformatted($"· {totalCount} hidden");
            textEnd = ImGui.GetItemRectMax().X;
        }

        var lineY = origin.Y + lineHeight * 0.5f;
        var lineStart = textEnd + 8f * scale;
        var lineEnd = origin.X + width;
        if (lineEnd > lineStart)
            ImGui.GetWindowDrawList().AddLine(new Vector2(lineStart, lineY), new Vector2(lineEnd, lineY),
                ImGui.GetColorU32(Styling.Hairline), 1f);

        ImGui.SetCursorScreenPos(new Vector2(origin.X, origin.Y + lineHeight));
        return toggled;
    }

    private static void DrawLockedExpansions(Configuration cfg)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var lineHeight = ImGui.GetTextLineHeight();
        var origin = ImGui.GetCursorScreenPos();
        var width = MathF.Max(1f, ImGui.GetContentRegionAvail().X);
        var hidden = cfg.HideLockedExpansions;

        var clicked = ImGui.InvisibleButton("##lockederas", new Vector2(width, lineHeight));
        var hovered = ImGui.IsItemHovered();
        if (hovered) ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

        DrawChevron(new Vector2(origin.X + 4f * scale, origin.Y + lineHeight * 0.5f), 4f * scale,
            hidden, hovered ? Styling.TextSecondary : Styling.TextMuted);

        var plural = LockedEras.Count == 1 ? "" : "s";
        var headline = hidden
            ? $"{LockedEras.Count} expansion{plural} not unlocked yet"
            : $"Hide {LockedEras.Count} expansion{plural} you have not unlocked";

        ImGui.SetCursorScreenPos(new Vector2(origin.X + 15f * scale, origin.Y));
        using (ImRaii.PushColor(ImGuiCol.Text, hovered ? Styling.TextSecondary : Styling.TextMuted))
            ImGui.TextUnformatted(headline);

        if (hidden)
        {
            RefreshLockedEraNames(cfg);
            var namesX = ImGui.GetItemRectMax().X + 9f * scale;
            if (namesX + ImGui.CalcTextSize(lockedEraNames).X < origin.X + width)
            {
                ImGui.SameLine(0, 9f * scale);
                using (ImRaii.PushColor(ImGuiCol.Text, Styling.WithAlpha(Styling.TextMuted, 0.75f)))
                    ImGui.TextUnformatted(lockedEraNames);
            }
        }

        ImGui.SetCursorScreenPos(new Vector2(origin.X, origin.Y + lineHeight));
        Styling.VSpace(6);

        if (hovered)
        {
            using var tooltip = ImRaii.Tooltip();
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextSecondary))
                ImGui.TextUnformatted(hidden
                    ? "Show these expansions anyway, locked tribes and all."
                    : "Collapse the expansions where you have not unlocked a single tribe.");
        }

        if (!clicked) return;
        cfg.HideLockedExpansions = !hidden;
        cfg.SaveDebounced();
    }

    private static void RefreshLockedEraNames(Configuration cfg)
    {
        var mask = 0;
        for (var eraIndex = 0; eraIndex < LockedEras.Count; eraIndex++)
        {
            mask |= 1 << (int)LockedEras[eraIndex];
        }

        var signature = (mask << 1) | (cfg.ExpansionOrder == ExpansionOrder.OldestFirst ? 1 : 0);
        if (signature == lockedEraSignature) return;
        lockedEraSignature = signature;

        var builder = new StringBuilder();
        for (var eraIndex = 0; eraIndex < LockedEras.Count; eraIndex++)
        {
            if (eraIndex > 0) builder.Append(" · ");
            builder.Append(LockedEras[eraIndex].ShortName());
        }
        lockedEraNames = builder.ToString();
    }

    private static void DrawEmptyState(bool nothingUnlocked)
    {
        Styling.VSpace(14);
        Styling.TextCentered(nothingUnlocked
            ? "No tribes unlocked yet — finish a tribe's intro quest in game to get started."
            : "Nothing matches the filters above.", Styling.TextDim);
        Styling.VSpace(14);
    }

    private static void DrawGrid(TribeEra era, AutoTribeController controller, Configuration cfg)
    {
        var available = ImGui.GetContentRegionAvail().X;
        var minCardWidth = Layout.TribeCardMinWidth * ImGuiHelpers.GlobalScale;
        var columns = Math.Max(1, Math.Min(Cards.Count, (int)(available / minCardWidth)));

        using var table = ImRaii.Table(EraGridIds[(int)era], columns,
            ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.NoBordersInBody);
        if (!table) return;

        for (var cardIndex = 0; cardIndex < Cards.Count; cardIndex++)
        {
            var tribe = Cards[cardIndex];
            ImGui.TableNextColumn();
            if (!tribe.Unlocked) TribeCard.DrawLocked(tribe);
            else if (IsRunnable(tribe)) TribeCard.Draw(tribe, controller, cfg);
            else TribeCard.DrawDone(tribe, controller, cfg);
        }
    }

    private static void DrawChipFlow()
    {
        var spacing = ImGui.GetStyle().ItemSpacing.X;
        var rightEdge = ImGui.GetWindowPos().X + ImGui.GetWindowContentRegionMax().X;
        for (var chipIndex = 0; chipIndex < UnderRank.Count; chipIndex++)
        {
            if (chipIndex > 0 && ImGui.GetItemRectMax().X + spacing + TribeChip.Width(UnderRank[chipIndex]) < rightEdge)
                ImGui.SameLine();
            TribeChip.Draw(UnderRank[chipIndex]);
        }
    }

    private static void DrawChevron(Vector2 center, float radius, bool collapsed, Vector4 color)
    {
        var drawList = ImGui.GetWindowDrawList();
        var packed = ImGui.GetColorU32(color);
        if (collapsed)
            drawList.AddTriangleFilled(
                center + new Vector2(radius * 0.75f, 0f),
                center + new Vector2(-radius * 0.6f, radius),
                center + new Vector2(-radius * 0.6f, -radius), packed);
        else
            drawList.AddTriangleFilled(
                center + new Vector2(0f, radius * 0.75f),
                center + new Vector2(-radius, -radius * 0.6f),
                center + new Vector2(radius, -radius * 0.6f), packed);
    }

    private static TribeEra[] BuildNewestFirst()
    {
        var ordered = new TribeEra[ErasOldestFirst.Length];
        for (var eraIndex = 0; eraIndex < ordered.Length; eraIndex++)
        {
            ordered[eraIndex] = ErasOldestFirst[ordered.Length - 1 - eraIndex];
        }
        return ordered;
    }

    private static string[] BuildEraHeaders()
    {
        var headers = new string[ErasOldestFirst.Length];
        for (var eraIndex = 0; eraIndex < headers.Length; eraIndex++)
        {
            headers[eraIndex] = ErasOldestFirst[eraIndex].DisplayName().ToUpperInvariant();
        }
        return headers;
    }

    private static string[] BuildIds(string prefix)
    {
        var ids = new string[ErasOldestFirst.Length];
        for (var eraIndex = 0; eraIndex < ids.Length; eraIndex++)
        {
            ids[eraIndex] = prefix + ErasOldestFirst[eraIndex];
        }
        return ids;
    }
}
