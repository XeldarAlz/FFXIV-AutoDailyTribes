using AutoDailyTribes.Core;
using AutoDailyTribes.Core.Localization;
using AutoDailyTribes.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoDailyTribes.Windows.Pages;

internal sealed class LogPage
{
    private const float TimeColumn = 60f;
    private const float MarkerRadius = 3f;
    private const float MarkerGap = 12f;
    private const float RowGap = 5f;
    private const float ToolbarGap = 8f;
    private const float FooterGap = 8f;
    private const float MinimumListHeight = 90f;
    private const float ListPadX = 12f;
    private const float ListPadY = 10f;
    private const int CopiedNoticeMs = 1800;

    private int seenVersion = -1;
    private long copiedAtMs;

    public void Draw()
    {
        var count = RunLog.Count;
        PageHeader.Draw(Loc.T(L.Log.Title), Loc.Plural(L.Log.Entries, count));

        DrawToolbar(count);
        Styling.VSpace(ToolbarGap);
        DrawList(count);
        DrawFooter();
    }

    private void DrawToolbar(int count)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var origin = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var height = Layout.ActionPillHeight * scale;

        var copied = Environment.TickCount64 - copiedAtMs < CopiedNoticeMs;
        var copyLabel = Loc.T(copied ? L.Log.Copied : L.Log.Copy);
        var copyWidth = PillButton.Width(copyLabel, FontAwesomeIcon.Copy);

        ImGui.SetCursorScreenPos(origin);
        if (PillButton.Draw("##adt_log_copy", copyLabel, Styling.AccentTeal, PillButton.Emphasis.Filled, FontAwesomeIcon.Copy,
                enabled: count > 0, height: Layout.ActionPillHeight))
        {
            ImGui.SetClipboardText(RunLog.ToText());
            copiedAtMs = Environment.TickCount64;
        }

        ImGui.SetCursorScreenPos(origin + new Vector2(copyWidth + ToolbarGap * scale, 0f));
        if (PillButton.Draw("##adt_log_clear", Loc.T(L.Common.Clear), Styling.TextSecondary, PillButton.Emphasis.Ghost, FontAwesomeIcon.Eraser,
                enabled: count > 0, height: Layout.ActionPillHeight))
        {
            RunLog.Clear();
        }

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(new Vector2(width, height));
    }

    private void DrawList(int count)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var width = ImGui.GetContentRegionAvail().X;

        float footerHeight;
        using (Fonts.PushCaption())
            footerHeight = TextDraw.MeasureWrapped(Loc.T(L.Log.Footer), width).Y;

        var height = ImGui.GetContentRegionAvail().Y - footerHeight - FooterGap * 2f * scale;
        if (height < MinimumListHeight * scale) height = MinimumListHeight * scale;

        var origin = ImGui.GetCursorScreenPos();
        var end = origin + new Vector2(width, height);
        Paint.Surface(ImGui.GetWindowDrawList(), origin, end, Styling.CardRounding * scale,
            Styling.WithAlpha(Styling.Surface0, 0.7f), Styling.WithAlpha(Styling.BorderDim, 0.5f));

        using (ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(ListPadX * scale, ListPadY * scale)))
        using (var child = ImRaii.Child("##adt_log_list", new Vector2(width, height), false, ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.AlwaysUseWindowPadding))
        {
            if (!child) return;

            if (count == 0)
            {
                DrawEmpty();
                return;
            }

            var innerWidth = ImGui.GetContentRegionAvail().X;
            var followTail = ImGui.GetScrollY() >= ImGui.GetScrollMaxY() - Layout.LogRowHeight * scale;
            for (var index = 0; index < count; index++) DrawRow(RunLog.At(index), innerWidth);

            if (RunLog.Version == seenVersion) return;
            seenVersion = RunLog.Version;
            if (followTail) ImGui.SetScrollHereY(1f);
        }
    }

    private static void DrawEmpty()
    {
        var origin = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        using (Fonts.PushCaption())
        {
            var text = Loc.T(L.Log.Empty);
            TextDraw.Wrapped(text, origin, width, Styling.TextMuted);
            ImGui.Dummy(new Vector2(width, TextDraw.MeasureWrapped(text, width).Y));
        }
    }

    private static void DrawRow(RunLogLine line, float width)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var origin = ImGui.GetCursorScreenPos();
        var dl = ImGui.GetWindowDrawList();
        var color = line.Level switch
        {
            RunLogLevel.Warning => Styling.AccentAmber,
            RunLogLevel.Error   => Styling.AccentRose,
            _                   => Styling.TextSecondary,
        };

        var lineHeight = TextDraw.LineHeight();
        using (Fonts.PushCaption())
        {
            var time = RunLog.Time(line);
            var timeHeight = TextDraw.Measure(time).Y;
            TextDraw.At(time, new Vector2(origin.X, origin.Y + (lineHeight - timeHeight) * 0.5f), Styling.TextMuted);
        }

        var markerX = origin.X + TimeColumn * scale;
        dl.AddCircleFilled(new Vector2(markerX, origin.Y + lineHeight * 0.5f), MarkerRadius * scale, Paint.Col(color));

        var textX = markerX + MarkerGap * scale;
        var textWidth = Math.Max(1f, width - (textX - origin.X));
        var textHeight = TextDraw.MeasureWrapped(line.Message, textWidth).Y;
        TextDraw.Wrapped(line.Message, new Vector2(textX, origin.Y), textWidth, color);

        ImGui.Dummy(new Vector2(width, textHeight + RowGap * scale));
    }

    private static void DrawFooter()
    {
        Styling.VSpace(FooterGap);
        using (Fonts.PushCaption())
        {
            var footer = Loc.T(L.Log.Footer);
            var origin = ImGui.GetCursorScreenPos();
            var width = ImGui.GetContentRegionAvail().X;
            TextDraw.Wrapped(footer, origin, width, Styling.TextMuted);
            ImGui.Dummy(new Vector2(width, TextDraw.MeasureWrapped(footer, width).Y));
        }
    }
}
