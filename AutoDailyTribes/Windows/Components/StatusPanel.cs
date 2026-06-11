using AutoDailyTribes.Core;
using AutoDailyTribes.Core.Tribes;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoDailyTribes.Windows.Components;

// Idle summary shown in the setup view: daily allowance + reset countdown + a one-line "what next"
// hint. The live run state lives in RunningPanel, so this panel never needs the controller.
internal static class StatusPanel
{
    private const float StateScale = 1.25f;

    public static void Draw()
    {
        var allowanceLeft = TribeStateReader.GlobalAllowanceLeft();
        var allowanceUsed = AdtConstants.DailyAllowanceCap - allowanceLeft;
        var exhausted = allowanceLeft <= 0;
        var fraction = Math.Clamp((float)allowanceUsed / AdtConstants.DailyAllowanceCap, 0f, 1f);

        var accent = exhausted ? Styling.AccentMint : Styling.BorderActive * 0.55f;
        var bg = exhausted ? Vector4.Lerp(Styling.CardBg, Styling.AccentMint, 0.06f) : Styling.CardBg;

        var scale = ImGuiHelpers.GlobalScale;
        var lineH = ImGui.GetTextLineHeight();
        var rowGap = ImGui.GetStyle().ItemSpacing.Y;
        var barH = Layout.AllowanceBarHeight * scale;

        var leftH = lineH * StateScale + rowGap + lineH;
        var rightH = lineH + rowGap + barH;
        var contentH = MathF.Max(leftH, rightH);
        var cardH = contentH + 9f * scale * 2f;

        using (Card.Begin("##statuspanel", new Vector2(-1, cardH), bg, accent, 1.4f))
        {
            var origin = new Vector2(ImGui.GetCursorPosX(), ImGui.GetCursorPosY());
            var innerW = ImGui.GetContentRegionAvail().X;
            var rightW = MathF.Min(Layout.StatusPanelInfoWidth * scale, innerW * 0.45f);
            var gap = 16f * scale;
            var leftW = innerW - rightW - gap;

            DrawState(exhausted, accent,
                new Vector2(origin.X, origin.Y + (contentH - leftH) * 0.5f), leftW, lineH, rowGap);

            DrawAllowance(allowanceUsed, fraction, exhausted,
                new Vector2(origin.X + innerW - rightW, origin.Y + (contentH - rightH) * 0.5f),
                rightW, lineH, rowGap, barH);
        }
    }

    private static void DrawState(
        bool exhausted, Vector4 accent,
        Vector2 origin, float width, float lineH, float rowGap)
    {
        var icon = exhausted ? FontAwesomeIcon.CheckCircle : FontAwesomeIcon.PlayCircle;
        var stateLabel = exhausted ? "All done today" : "Ready";
        var activity = exhausted
            ? "Daily allowances spent — back after reset"
            : "Pick tribes below, then hit Run selected";

        ImGui.SetCursorPos(origin);
        ImGui.SetWindowFontScale(StateScale);
        using (ImRaii.PushFont(UiBuilder.IconFont))
        using (ImRaii.PushColor(ImGuiCol.Text, accent))
            ImGui.TextUnformatted(icon.ToIconString());
        ImGui.SameLine();
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextStrong))
            ImGui.TextUnformatted(stateLabel);
        ImGui.SetWindowFontScale(1f);

        ImGui.SetCursorPos(new Vector2(origin.X, origin.Y + lineH * StateScale + rowGap));
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextSecondary))
            ImGui.TextUnformatted(Fit(activity, width));
    }

    private static void DrawAllowance(
        int used, float fraction, bool exhausted,
        Vector2 origin, float width, float lineH, float rowGap, float barH)
    {
        var reset = $"Reset {FormatResetCountdown()}";
        var resetW = ImGui.CalcTextSize(reset).X;

        ImGui.SetCursorPos(origin);
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
            ImGui.TextUnformatted("ALLOWANCE");
        ImGui.SetCursorPos(new Vector2(origin.X + width - resetW, origin.Y));
        using (ImRaii.PushColor(ImGuiCol.Text, exhausted ? Styling.AccentAmber : Styling.TextDim))
            ImGui.TextUnformatted(reset);

        ImGui.SetCursorPos(new Vector2(origin.X, origin.Y + lineH + rowGap));
        DrawBar(width, barH, fraction, used, exhausted);
    }

    private static void DrawBar(float width, float barH, float fraction, int used, bool exhausted)
    {
        var drawList = ImGui.GetWindowDrawList();
        var begin = ImGui.GetCursorScreenPos();
        var end = begin + new Vector2(width, barH);

        drawList.AddRectFilled(begin, end, ImGui.GetColorU32(Styling.CardBgSoft), 4f);
        if (fraction > 0)
        {
            var fillEnd = new Vector2(begin.X + width * fraction, end.Y);
            var fillColor = exhausted ? Styling.AccentMint : Styling.AccentTeal;
            drawList.AddRectFilled(begin, fillEnd, ImGui.GetColorU32(fillColor), 4f);
        }

        var label = $"{used} / {AdtConstants.DailyAllowanceCap}";
        var size = ImGui.CalcTextSize(label);
        var pos = new Vector2(begin.X + (width - size.X) * 0.5f, begin.Y + (barH - size.Y) * 0.5f);
        var textColor = fraction > 0.5f
            ? ImGui.GetColorU32(new Vector4(0.06f, 0.08f, 0.08f, 1f))
            : ImGui.GetColorU32(Styling.TextSecondary);
        drawList.AddText(pos, textColor, label);

        ImGui.Dummy(new Vector2(width, barH));
    }

    private static string FormatResetCountdown()
    {
        var now = DateTime.UtcNow;
        var nextReset = new DateTime(now.Year, now.Month, now.Day, 15, 0, 0, DateTimeKind.Utc);
        if (nextReset <= now) nextReset = nextReset.AddDays(1);
        var r = nextReset - now;
        return r.TotalHours >= 1
            ? $"{(int)r.TotalHours}h {r.Minutes:D2}m"
            : r.TotalMinutes >= 1
                ? $"{r.Minutes}m {r.Seconds:D2}s"
                : $"{r.Seconds}s";
    }

    private static string Fit(string text, float maxWidth)
    {
        if (ImGui.CalcTextSize(text).X <= maxWidth) return text;
        while (text.Length > 1 && ImGui.CalcTextSize(text + "…").X > maxWidth)
            text = text[..^1];
        return text + "…";
    }
}
