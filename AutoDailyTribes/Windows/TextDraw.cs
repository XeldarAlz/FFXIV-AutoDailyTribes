using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoDailyTribes.Windows;

internal static class TextDraw
{
    private const string Ellipsis = "…";

    public static string Upper(string text) => text.ToUpperInvariant();

    public static Vector2 Measure(string text) => ImGui.CalcTextSize(text);

    public static Vector2 MeasureWrapped(string text, float wrapWidth) => ImGui.CalcTextSize(text, false, wrapWidth);

    public static void At(string text, Vector2 pos, Vector4 color)
        => ImGui.GetWindowDrawList().AddText(pos, Paint.Col(color), text);

    public static void Right(string text, float rightX, float y, Vector4 color)
        => At(text, new Vector2(rightX - Measure(text).X, y), color);

    public static void Center(string text, float centerX, float y, Vector4 color)
        => At(text, new Vector2(centerX - Measure(text).X * 0.5f, y), color);

    public static void Middle(string text, Vector2 min, Vector2 max, Vector4 color)
    {
        var size = Measure(text);
        At(text, new Vector2((min.X + max.X - size.X) * 0.5f, (min.Y + max.Y - size.Y) * 0.5f), color);
    }

    public static void Wrapped(string text, Vector2 pos, float wrapWidth, Vector4 color)
        => ImGui.GetWindowDrawList().AddText(ImGui.GetFont(), ImGui.GetFontSize(), pos, Paint.Col(color), text, wrapWidth);

    public static Vector2 IconSize(FontAwesomeIcon icon)
    {
        using (ImRaii.PushFont(UiBuilder.IconFont))
            return Measure(icon.ToIconString());
    }

    public static void Icon(FontAwesomeIcon icon, Vector2 pos, Vector4 color)
    {
        using (ImRaii.PushFont(UiBuilder.IconFont))
            At(icon.ToIconString(), pos, color);
    }

    public static void IconCentered(FontAwesomeIcon icon, Vector2 center, Vector4 color)
    {
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            var glyph = icon.ToIconString();
            var size = Measure(glyph);
            At(glyph, center - size * 0.5f, color);
        }
    }

    public static void IconRight(FontAwesomeIcon icon, float rightX, float centerY, Vector4 color)
    {
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            var glyph = icon.ToIconString();
            var size = Measure(glyph);
            At(glyph, new Vector2(rightX - size.X, centerY - size.Y * 0.5f), color);
        }
    }

    public static string Truncate(string text, float maxWidth)
    {
        if (Measure(text).X <= maxWidth) return text;
        var budget = maxWidth - Measure(Ellipsis).X;
        var low = 1;
        var high = text.Length - 1;
        while (low < high)
        {
            var mid = (low + high + 1) / 2;
            if (Measure(text[..mid]).X <= budget) low = mid;
            else high = mid - 1;
        }

        return text[..low] + Ellipsis;
    }

    public static void SmallCaps(string label, Vector2 pos, Vector4 color)
    {
        using (Fonts.PushCaption())
            At(Upper(label), pos, color);
    }

    public static Vector2 SmallCapsSize(string label)
    {
        using (Fonts.PushCaption())
            return Measure(Upper(label));
    }

    public static void SectionTitle(string label, Vector2 pos, Vector4 color)
    {
        using (Fonts.PushHeadline())
            At(label, pos, color);
    }

    public static Vector2 SectionTitleSize(string label)
    {
        using (Fonts.PushHeadline())
            return Measure(label);
    }

    public static float LineHeight() => ImGui.GetTextLineHeight();
}
