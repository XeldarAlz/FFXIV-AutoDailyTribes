using AutoDailyTribes.Core;
using AutoDailyTribes.Core.Localization;
using AutoDailyTribes.Core.Tasks;
using AutoDailyTribes.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoDailyTribes.Windows.Sections;

// The plan for today: an allowance ring, a one-line summary, the clear button and the run-order
// strip. The card grows with the strip, so the background is painted on a lower draw channel once
// the content height is known.
internal static class TodayCard
{
    private const float PadX = 18f;
    private const float PadY = 16f;
    private const float RingThickness = 5f;
    private const float RingGap = 18f;
    private const float TitleGap = 6f;
    private const float StripGap = 14f;
    private const float ButtonHeight = 28f;

    private static readonly string[] AllowanceLabels = BuildAllowanceLabels();

    public static void Draw(Configuration cfg, AutoTribeController ctrl)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var plan = RunPlan.Resolve(cfg);
        var origin = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var padX = PadX * scale;
        var padY = PadY * scale;
        var dl = ImGui.GetWindowDrawList();

        dl.ChannelsSplit(2);
        dl.ChannelsSetCurrent(1);

        var radius = Layout.TodayRingRadius * scale;
        var ringCenter = new Vector2(origin.X + padX + radius, origin.Y + padY + radius);
        DrawRing(ringCenter, radius, plan);

        var columnX = ringCenter.X + radius + RingGap * scale;
        var columnRight = origin.X + width - padX;
        var y = origin.Y + padY;

        var title = Loc.T(L.Tribes.Today);
        var titleSize = TextDraw.SectionTitleSize(title);
        TextDraw.SectionTitle(title, new Vector2(columnX, y), Styling.TextStrong);
        DrawClearButton(cfg, ctrl, plan, columnRight, y + titleSize.Y * 0.5f);
        y += titleSize.Y + TitleGap * scale;

        TextDraw.At(TextDraw.Truncate(Summary(plan), columnRight - columnX), new Vector2(columnX, y), Styling.TextDim);
        y += ImGui.GetTextLineHeight();

        y = MathF.Max(y, ringCenter.Y + radius) + StripGap * scale;
        ImGui.SetCursorScreenPos(new Vector2(origin.X + padX, y));
        using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(6f, 6f) * scale))
        {
            ImGui.PushID("##adt_today_strip");
            ImGui.BeginGroup();
            QueueStrip.Draw(cfg, ctrl, width - padX * 2f);
            ImGui.EndGroup();
            ImGui.PopID();
        }

        var end = new Vector2(origin.X + width, ImGui.GetItemRectMax().Y + padY);

        dl.ChannelsSetCurrent(0);
        Paint.Glass(dl, origin, end, Styling.PanelRounding * scale, Styling.AccentTeal, 0.07f, 0f, elevated: true);
        dl.ChannelsMerge();

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(new Vector2(width, end.Y - origin.Y));
    }

    private static void DrawRing(Vector2 center, float radius, RunPlan.Snapshot plan)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var thickness = RingThickness * scale;
        var left = Math.Clamp(plan.AllowanceLeft, 0, AdtConstants.DailyAllowanceCap);
        var used = AdtConstants.DailyAllowanceCap - left;
        var fraction = Motion.Approach(Motion.Key("##adt_allowance_ring"), used / (float)AdtConstants.DailyAllowanceCap, 6f);
        var accent = left == 0 ? Styling.AccentMint : Styling.AccentTeal;

        ProgressRing.Track(center, radius, thickness, Styling.WithAlpha(Styling.BorderDim, 0.7f));
        ProgressRing.Fill(center, radius, thickness, fraction, accent);
        ProgressRing.CenterValue(center, AllowanceLabels[left], Loc.T(L.Tribes.RingCaption), Styling.TextStrong, Styling.TextDim);

        var extent = new Vector2(radius, radius);
        if (!Hit.HoveringRect(center - extent, center + extent)) return;

        Tooltip.Show(Loc.T(L.Tribes.RingTooltip, used, AdtConstants.DailyAllowanceCap, AdtConstants.MaxAcceptsPerTribe,
            AdtConstants.DailyAllowanceCap / AdtConstants.MaxAcceptsPerTribe));
    }

    private static string Summary(RunPlan.Snapshot plan)
    {
        var countdown = Formatting.ResetCountdown();
        if (plan.SelectedCount == 0)
        {
            return Loc.T(L.Tribes.SummaryEmpty, plan.AllowanceLeft, AdtConstants.DailyAllowanceCap, countdown);
        }

        return Loc.T(L.Tribes.SummaryPicked, Formatting.Tribes(plan.SelectedCount), plan.Runnable.Count,
            plan.AllowancesNeeded, plan.AllowanceLeft, countdown);
    }

    private static void DrawClearButton(Configuration cfg, AutoTribeController ctrl, RunPlan.Snapshot plan, float rightX, float midY)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var canClear = !ctrl.Running && plan.SelectedCount > 0;
        var label = Loc.T(L.Common.Clear);
        var width = PillButton.Width(label);
        ImGui.SetCursorScreenPos(new Vector2(rightX - width, midY - ButtonHeight * scale * 0.5f));
        if (!PillButton.Draw("##adt_clear_picks", label, Styling.AccentRose, PillButton.Emphasis.Ghost,
                enabled: canClear, height: ButtonHeight, tooltip: Loc.T(L.Tribes.ClearHint)))
        {
            return;
        }

        cfg.SelectedTribes.Clear();
        cfg.SaveDebounced();
    }

    private static string[] BuildAllowanceLabels()
    {
        var labels = new string[AdtConstants.DailyAllowanceCap + 1];
        for (var index = 0; index < labels.Length; index++) labels[index] = index.ToString();
        return labels;
    }
}
