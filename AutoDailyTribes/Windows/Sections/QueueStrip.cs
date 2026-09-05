using AutoDailyTribes.Core.Tasks;
using AutoDailyTribes.Core.Tribes;
using AutoDailyTribes.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using System.Numerics;

namespace AutoDailyTribes.Windows.Sections;

// The picked tribes as a wrapping row of chips in run order. Chips drag to reorder and carry a
// close glyph to drop a tribe; both are frozen while a run is in progress.
internal static class QueueStrip
{
    private const float ChipHeight = 32f;
    private const float Gap = 8f;
    private const float PadX = 11f;
    private const float InnerGap = 6f;
    private const float IconSize = 20f;

    private const string EmptyHint = "Pick tribes below. They run in the order you add them, and the list is remembered for tomorrow.";
    private const string DragHint = "Drag to reorder";
    private const string RemoveHint = "Remove from the run";
    private const string StateDone = "done";
    private const string StateLocked = "locked";
    private const string StateHidden = "hidden";

    private readonly record struct ChipMetrics(
        float Total, float BodyWidth, float NumberWidth, float NameWidth, float StateWidth, string Number, string? State);

    private static readonly List<TribeInfo> tribes = [];
    private static readonly string[] Numbers = BuildNumbers();
    private static readonly string[] RankNeeded = BuildRankNeeded();
    private static (Vector2 Min, Vector2 Max)[] rects = new (Vector2, Vector2)[TribeRegistry.Tribes.Length];
    private static int? dragIndex;

    public static void Draw(Configuration cfg, AutoTribeController controller, float width)
    {
        Collect(cfg);
        if (tribes.Count == 0)
        {
            dragIndex = null;
            DrawEmpty(width);
            return;
        }

        var running = controller.Running;
        var scale = ImGuiHelpers.GlobalScale;
        var height = ChipHeight * scale;
        var gap = Gap * scale;
        var timesWidth = TextDraw.IconSize(FontAwesomeIcon.Times).X;

        var mouse = ImGui.GetMousePos();
        var dragActive = !running && dragIndex is not null && ImGui.IsMouseDown(ImGuiMouseButton.Left);

        var regionStart = ImGui.GetCursorScreenPos();
        var x = regionStart.X;
        var y = regionStart.Y;
        var maxY = y + height;

        int? remove = null;
        if (rects.Length < tribes.Count) rects = new (Vector2, Vector2)[tribes.Count];

        for (var index = 0; index < tribes.Count; index++)
        {
            var tribe = tribes[index];
            var metrics = Measure(cfg, tribe, index + 1, timesWidth, scale);

            if (x > regionStart.X && x + metrics.Total > regionStart.X + width)
            {
                x = regionStart.X;
                y += height + gap;
            }

            var origin = new Vector2(x, y);
            var end = origin + new Vector2(metrics.Total, height);
            rects[index] = (origin, end);

            var isDropTarget = dragActive && dragIndex != index && Contains(origin, end, mouse);
            DrawChip(origin, end, metrics, tribe, index, running, isDropTarget, ref remove);

            x += metrics.Total + gap;
            maxY = MathF.Max(maxY, y + height);
        }

        ImGui.SetCursorScreenPos(regionStart);
        ImGui.Dummy(new Vector2(width, maxY - regionStart.Y));

        (int From, int To)? move = null;
        if (!running && dragIndex is int source)
        {
            if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            {
                var target = -1;
                for (var index = 0; index < tribes.Count; index++)
                {
                    if (Contains(rects[index].Min, rects[index].Max, mouse)) target = index;
                }

                if (target >= 0 && target != source) move = (source, target);
                dragIndex = null;
            }
            else if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
            {
                dragIndex = null;
            }
            else if (source < tribes.Count)
            {
                DrawDragPreview(tribes[source].Name, mouse);
            }
        }

        var changed = false;
        if (remove is int removeIndex) changed = cfg.SelectedTribes.Remove(tribes[removeIndex].BeastTribeId);
        else if (move is { } movement) changed = MoveFiltered(cfg.SelectedTribes, movement.From, movement.To);
        if (changed) cfg.SaveDebounced();
    }

    private static void Collect(Configuration cfg)
    {
        tribes.Clear();
        var selected = cfg.SelectedTribes;
        for (var index = 0; index < selected.Count; index++)
        {
            var tribe = RunPlan.Find(selected[index]);
            if (tribe is not null) tribes.Add(tribe);
        }
    }

    private static void DrawEmpty(float width)
    {
        using (Fonts.PushCaption())
        {
            var origin = ImGui.GetCursorScreenPos();
            TextDraw.Wrapped(EmptyHint, origin + new Vector2(2f * ImGuiHelpers.GlobalScale, 0f), width, Styling.TextMuted);
            ImGui.Dummy(new Vector2(width, TextDraw.MeasureWrapped(EmptyHint, width).Y));
        }
    }

    private static bool Contains(Vector2 min, Vector2 max, Vector2 point)
        => point.X >= min.X && point.X <= max.X && point.Y >= min.Y && point.Y <= max.Y;

    private static string? StateLabel(Configuration cfg, TribeInfo tribe)
    {
        if (!tribe.Unlocked) return StateLocked;
        if (!tribe.MeetsRankRequirement) return RankNeeded[Math.Clamp(tribe.MinRankForDailies, 0, RankNeeded.Length - 1)];
        if (!RunPlan.PassesFilters(cfg, tribe)) return StateHidden;
        if (!RunPlan.IsRunnable(tribe)) return StateDone;
        return null;
    }

    private static ChipMetrics Measure(Configuration cfg, TribeInfo tribe, int position, float timesWidth, float scale)
    {
        var padX = PadX * scale;
        var gap = InnerGap * scale;
        var number = position < Numbers.Length ? Numbers[position] : position.ToString();
        var numberWidth = TextDraw.Measure(number).X;
        var nameWidth = TextDraw.Measure(tribe.Name).X;

        var state = StateLabel(cfg, tribe);
        var stateWidth = 0f;
        if (state is not null)
        {
            using (Fonts.PushCaption())
                stateWidth = TextDraw.Measure(state).X;
        }

        var bodyWidth = padX + numberWidth + gap + IconSize * scale + gap + nameWidth
            + (state is not null ? gap + stateWidth : 0f)
            + gap;
        var closeWidth = timesWidth + gap * 2f;
        return new ChipMetrics(bodyWidth + closeWidth, bodyWidth, numberWidth, nameWidth, stateWidth, number, state);
    }

    private static void DrawChip(Vector2 origin, Vector2 end, ChipMetrics metrics, TribeInfo tribe, int index, bool running,
        bool isDropTarget, ref int? remove)
    {
        var dl = ImGui.GetWindowDrawList();
        var scale = ImGuiHelpers.GlobalScale;
        var height = end.Y - origin.Y;

        ImGui.SetCursorScreenPos(origin);
        ImGui.PushID((nint)tribe.BeastTribeId);
        ImGui.InvisibleButton("##adt_chip", new Vector2(metrics.BodyWidth, height));
        var bodyHovered = ImGui.IsItemHovered();
        if (!running && ImGui.IsItemActivated()) dragIndex = index;
        var beingDragged = dragIndex == index;
        if (!running && (bodyHovered || beingDragged)) ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeAll);
        if (!running && bodyHovered && !beingDragged && !ImGui.IsMouseDown(ImGuiMouseButton.Left)) Tooltip.Show(DragHint);

        ImGui.SetCursorScreenPos(new Vector2(origin.X + metrics.BodyWidth, origin.Y));
        var closeClicked = ImGui.InvisibleButton("##adt_chip_close", new Vector2(end.X - origin.X - metrics.BodyWidth, height));
        var closeHovered = ImGui.IsItemHovered();
        ImGui.PopID();
        if (!running)
        {
            if (closeHovered) ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            if (closeClicked) remove = index;
        }

        var hover = Motion.Hover(Motion.Key("##adt_chip", tribe.BeastTribeId), !running && (bodyHovered || beingDragged));
        var muted = metrics.State is not null;
        var accent = Styling.AccentTeal;
        var rounding = height * 0.5f;
        var tint = running ? 0.06f : beingDragged ? 0.55f : isDropTarget ? 0.5f : 0.28f + 0.14f * hover;
        if (muted) tint *= 0.4f;
        var top = Styling.Tint(Styling.Surface2, accent, tint);
        var bottom = Styling.Tint(Styling.Surface1, accent, tint * 0.8f);
        Paint.Gradient(dl, origin, end, top, bottom, rounding);
        Paint.TopLight(dl, origin, end, rounding, 0.09f);
        var border = running || muted ? Styling.WithAlpha(Styling.BorderDim, 0.6f + 0.2f * hover)
            : isDropTarget ? Styling.AccentTealSoft
            : Styling.WithAlpha(accent, 0.5f + 0.35f * hover);
        Paint.Stroke(dl, origin, end, border, rounding, isDropTarget ? 1.8f : 1f);

        var dim = running || muted ? Styling.TextMuted : Styling.TextDim;
        var strong = running ? Styling.TextDim : muted ? Styling.TextSecondary : Styling.TextStrong;
        var midY = origin.Y + height * 0.5f;
        var gap = InnerGap * scale;
        var cursorX = origin.X + PadX * scale;

        PutText(metrics.Number, cursorX, midY, dim);
        cursorX += metrics.NumberWidth + gap;

        var icon = IconSize * scale;
        TribeIcon.Draw(dl, tribe, new Vector2(cursorX, midY - icon * 0.5f), icon, muted ? 0.6f : 1f);
        cursorX += icon + gap;

        PutText(tribe.Name, cursorX, midY, strong);
        cursorX += metrics.NameWidth;

        if (metrics.State is not null)
        {
            cursorX += gap;
            var stateColor = ReferenceEquals(metrics.State, StateDone) ? Styling.AccentMint : Styling.TextMuted;
            using (Fonts.PushCaption())
                PutText(metrics.State, cursorX, midY, stateColor);
        }

        var closeColor = running ? Styling.TextMuted : closeHovered ? Styling.AccentRose : Styling.TextDim;
        var closeSize = TextDraw.IconSize(FontAwesomeIcon.Times);
        TextDraw.Icon(FontAwesomeIcon.Times, new Vector2(origin.X + metrics.BodyWidth + gap, midY - closeSize.Y * 0.5f), closeColor);

        if (!running && closeHovered) Tooltip.Show(RemoveHint);
    }

    private static void DrawDragPreview(string name, Vector2 mouse)
    {
        var dl = ImGui.GetForegroundDrawList();
        var scale = ImGuiHelpers.GlobalScale;
        var pad = new Vector2(10f, 5f) * scale;
        var position = mouse + new Vector2(14f, 8f) * scale;
        var size = ImGui.CalcTextSize(name);
        var min = position - pad;
        var max = position + size + pad;
        Paint.Shadow(dl, min, max, (max.Y - min.Y) * 0.5f, 6f * scale, 0.5f);
        Paint.Pill(dl, min, max, Styling.Tint(Styling.Surface2, Styling.AccentTeal, 0.45f), Styling.WithAlpha(Styling.AccentTealSoft, 0.7f));
        dl.AddText(position, Paint.Col(Styling.TextStrong), name);
    }

    private static void PutText(string text, float x, float midY, Vector4 color)
    {
        var size = TextDraw.Measure(text);
        TextDraw.At(text, new Vector2(x, midY - size.Y * 0.5f), color);
    }

    // The strip skips ids the registry no longer knows, so a chip index has to be mapped back to
    // its slot in the configured list before the list is reordered.
    private static int RealIndex(List<uint> selected, int filteredIndex)
    {
        var seen = -1;
        for (var index = 0; index < selected.Count; index++)
        {
            if (RunPlan.Find(selected[index]) is not null && ++seen == filteredIndex) return index;
        }

        return -1;
    }

    private static bool MoveFiltered(List<uint> selected, int fromFiltered, int toFiltered)
        => ListReorder.Move(selected, RealIndex(selected, fromFiltered), RealIndex(selected, toFiltered));

    private static string[] BuildNumbers()
    {
        var numbers = new string[TribeRegistry.Tribes.Length + 1];
        for (var index = 0; index < numbers.Length; index++) numbers[index] = index.ToString();
        return numbers;
    }

    private static string[] BuildRankNeeded()
    {
        var labels = new string[Core.AdtConstants.MaxTribeRank + 1];
        for (var index = 0; index < labels.Length; index++) labels[index] = $"rank {index}";
        return labels;
    }
}
