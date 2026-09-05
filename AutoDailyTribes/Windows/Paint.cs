using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using System.Numerics;

namespace AutoDailyTribes.Windows;

internal static class Paint
{
    private const int ShadowLayers = 5;

    public static uint Col(Vector4 color) => ImGui.GetColorU32(color);

    private static uint Opaque(Vector4 color) => ImGui.ColorConvertFloat4ToU32(color with { W = 1f });

    public static void Fill(ImDrawListPtr dl, Vector2 min, Vector2 max, Vector4 color, float rounding, ImDrawFlags flags = ImDrawFlags.RoundCornersAll)
        => dl.AddRectFilled(min, max, Col(color), rounding, flags);

    public static void Stroke(ImDrawListPtr dl, Vector2 min, Vector2 max, Vector4 color, float rounding, float thickness = 1f, ImDrawFlags flags = ImDrawFlags.RoundCornersAll)
        => dl.AddRect(min, max, Col(color), rounding, flags, thickness);

    public static void Gradient(ImDrawListPtr dl, Vector2 min, Vector2 max, Vector4 top, Vector4 bottom, float rounding, ImDrawFlags flags = ImDrawFlags.RoundCornersAll)
    {
        var start = dl.VtxBuffer.Size;
        dl.AddRectFilled(min, max, Col(new Vector4(1f, 1f, 1f, top.W)), rounding, flags);
        var end = dl.VtxBuffer.Size;
        ImGuiP.ShadeVertsLinearColorGradientKeepAlpha(dl, start, end, min, new Vector2(min.X, max.Y), Opaque(top), Opaque(bottom));
    }

    public static void GradientH(ImDrawListPtr dl, Vector2 min, Vector2 max, Vector4 left, Vector4 right, float rounding, ImDrawFlags flags = ImDrawFlags.RoundCornersAll)
    {
        var start = dl.VtxBuffer.Size;
        dl.AddRectFilled(min, max, Col(new Vector4(1f, 1f, 1f, left.W)), rounding, flags);
        var end = dl.VtxBuffer.Size;
        ImGuiP.ShadeVertsLinearColorGradientKeepAlpha(dl, start, end, min, new Vector2(max.X, min.Y), Opaque(left), Opaque(right));
    }

    public static void Shadow(ImDrawListPtr dl, Vector2 min, Vector2 max, float rounding, float spread, float alpha)
    {
        var offset = new Vector2(0f, spread * 0.4f);
        for (var layer = ShadowLayers; layer >= 1; layer--)
        {
            var t = layer / (float)ShadowLayers;
            var grow = new Vector2(spread * t, spread * t);
            var layerAlpha = alpha / ShadowLayers * (1.25f - t);
            dl.AddRectFilled(min - grow + offset, max + grow + offset, Col(new Vector4(0f, 0f, 0f, layerAlpha)), rounding + grow.X);
        }
    }

    public static void Glow(ImDrawListPtr dl, Vector2 min, Vector2 max, float rounding, Vector4 color, float intensity)
    {
        var scale = ImGuiHelpers.GlobalScale;
        for (var layer = 3; layer >= 1; layer--)
        {
            var grow = new Vector2(layer * 3f * scale, layer * 3f * scale);
            var alpha = 0.04f * (4 - layer) * intensity;
            dl.AddRectFilled(min - grow, max + grow, Col(Styling.WithAlpha(color, alpha)), rounding + grow.X);
        }
    }

    public static void TopLight(ImDrawListPtr dl, Vector2 min, Vector2 max, float rounding, float alpha = 0.075f)
        => dl.AddLine(new Vector2(min.X + rounding, min.Y + 1f), new Vector2(max.X - rounding, min.Y + 1f), Col(new Vector4(1f, 1f, 1f, alpha)), 1f);

    public static void Surface(ImDrawListPtr dl, Vector2 min, Vector2 max, float rounding, Vector4 fill, Vector4 border, bool topLight = true, float borderThickness = 1f)
    {
        Fill(dl, min, max, fill, rounding);
        if (topLight) TopLight(dl, min, max, rounding);
        Stroke(dl, min, max, border, rounding, borderThickness);
    }

    public static void Glass(ImDrawListPtr dl, Vector2 min, Vector2 max, float rounding, Vector4 accent, float tint, float hover = 0f, bool elevated = false)
    {
        if (elevated) Shadow(dl, min, max, rounding, 10f * ImGuiHelpers.GlobalScale, 0.5f);

        var top = Vector4.Lerp(Styling.Surface1, accent, tint * 1.3f);
        var bottom = Vector4.Lerp(Styling.Surface0, accent, tint * 0.8f);
        if (hover > 0f)
        {
            top = Vector4.Lerp(top, Styling.Surface2, hover * 0.6f);
            bottom = Vector4.Lerp(bottom, Styling.Surface1, hover * 0.6f);
        }

        Gradient(dl, min, max, top with { W = 0.97f }, bottom with { W = 0.97f }, rounding);
        TopLight(dl, min, max, rounding);

        var borderMix = Math.Clamp(tint * 2.2f + hover * 0.4f, 0f, 1f);
        var border = Vector4.Lerp(Styling.WithAlpha(Styling.BorderDim, 0.75f), Styling.WithAlpha(accent, 0.9f), borderMix);
        Stroke(dl, min, max, border, rounding);
    }

    public static void Pill(ImDrawListPtr dl, Vector2 min, Vector2 max, Vector4 fill, Vector4 border)
    {
        var rounding = (max.Y - min.Y) * 0.5f;
        Fill(dl, min, max, fill, rounding);
        Stroke(dl, min, max, border, rounding);
    }

    public static void Bar(ImDrawListPtr dl, Vector2 origin, float width, float height, float fraction, Vector4 color)
    {
        var rounding = height * 0.5f;
        var end = origin + new Vector2(width, height);
        Fill(dl, origin, end, Styling.WithAlpha(Styling.Surface0, 0.9f), rounding);
        Stroke(dl, origin, end, Styling.WithAlpha(Styling.BorderDim, 0.55f), rounding);

        fraction = Math.Clamp(fraction, 0f, 1f);
        if (fraction <= 0f) return;

        var fillWidth = MathF.Max(height, width * fraction);
        var fillEnd = new Vector2(origin.X + fillWidth, end.Y);
        Gradient(dl, origin, fillEnd, Styling.Lighten(color, 0.22f), color, rounding);
        dl.AddCircleFilled(new Vector2(fillEnd.X - rounding, origin.Y + rounding), rounding * 1.7f, Col(Styling.WithAlpha(color, 0.22f)));
    }

    public static void IndeterminateBar(ImDrawListPtr dl, Vector2 origin, float width, float height, Vector4 color, double periodMs = 1500.0)
    {
        var rounding = height * 0.5f;
        var end = origin + new Vector2(width, height);
        Fill(dl, origin, end, Styling.WithAlpha(Styling.Surface0, 0.9f), rounding);
        Stroke(dl, origin, end, Styling.WithAlpha(Styling.BorderDim, 0.55f), rounding);

        var segmentWidth = width * 0.30f;
        var t = Motion.EaseInOutCubic(Styling.Phase(periodMs));
        var x0 = origin.X - segmentWidth + (width + segmentWidth) * t;
        var segMin = new Vector2(MathF.Max(origin.X, x0), origin.Y);
        var segMax = new Vector2(MathF.Min(end.X, x0 + segmentWidth), end.Y);
        if (segMax.X - segMin.X < 1f) return;

        dl.PushClipRect(origin, end, true);
        GradientH(dl, segMin, segMax, Styling.WithAlpha(Styling.Lighten(color, 0.3f), 0.85f), Styling.WithAlpha(color, 0.85f), rounding);
        dl.PopClipRect();
    }

    // Slot segments encode delivery, not acceptance: solid = turned in, hollow pulsing = accepted but
    // still in the journal, faint = unused.
    public static void Segments(ImDrawListPtr dl, Vector2 origin, float width, float height, int count, int solid, int hollow, Vector4 solidColor, Vector4 hollowColor, float gap)
    {
        if (count <= 0) return;
        var rounding = height * 0.5f;
        var segmentWidth = (width - gap * (count - 1)) / count;
        var scale = ImGuiHelpers.GlobalScale;
        var track = Styling.WithAlpha(Styling.TextSecondary, 0.13f);
        for (var index = 0; index < count; index++)
        {
            var min = new Vector2(origin.X + index * (segmentWidth + gap), origin.Y);
            var max = new Vector2(min.X + segmentWidth, origin.Y + height);
            if (index < solid)
            {
                Fill(dl, min, max, solidColor, rounding);
            }
            else if (index < solid + hollow)
            {
                Fill(dl, min, max, Styling.WithAlpha(hollowColor, 0.16f), rounding);
                Stroke(dl, min, max, Styling.PulseColor(hollowColor, Styling.WithAlpha(hollowColor, 0.5f), Styling.PulseBreath), rounding, 1.3f * scale);
            }
            else
            {
                Fill(dl, min, max, track, rounding);
            }
        }
    }

    public static void Dot(ImDrawListPtr dl, Vector2 center, float radius, Vector4 color, float haloAlpha = 0.22f)
    {
        dl.AddCircleFilled(center, radius * 2.3f, Col(Styling.WithAlpha(color, haloAlpha)));
        dl.AddCircleFilled(center, radius, Col(color));
    }

    public static void Hairline(ImDrawListPtr dl, Vector2 from, Vector2 to)
        => dl.AddLine(from, to, Col(Styling.Hairline), 1f);

    public static void Divider(float verticalPadding = 6f)
    {
        Styling.VSpace(verticalPadding);
        var origin = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        Hairline(ImGui.GetWindowDrawList(), origin, origin + new Vector2(width, 0f));
        ImGui.Dummy(new Vector2(width, 1f));
        Styling.VSpace(verticalPadding);
    }

    public static void Check(ImDrawListPtr dl, Vector2 center, float size, Vector4 color, float thickness)
    {
        var a = center + new Vector2(-size * 0.42f, 0f);
        var b = center + new Vector2(-size * 0.10f, size * 0.32f);
        var c = center + new Vector2(size * 0.46f, -size * 0.34f);
        var col = Col(color);
        dl.AddLine(a, b, col, thickness);
        dl.AddLine(b, c, col, thickness);
    }

    // Flips between a down and an up chevron, passing through a flat line at the halfway point.
    public static void Chevron(ImDrawListPtr dl, Vector2 center, float half, float flip, Vector4 color, float thickness)
    {
        var lift = half * 0.55f * (1f - 2f * flip);
        var left = center + new Vector2(-half, -lift);
        var middle = center + new Vector2(0f, lift);
        var right = center + new Vector2(half, -lift);
        var stroke = Col(color);
        dl.AddLine(left, middle, stroke, thickness);
        dl.AddLine(middle, right, stroke, thickness);
    }
}
