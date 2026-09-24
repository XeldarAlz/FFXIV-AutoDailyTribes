using AutoDailyTribes.Core.Localization;
using AutoDailyTribes.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using ECommons.DalamudServices;
using System.Numerics;

namespace AutoDailyTribes.Windows.Pages;

internal sealed partial class AboutPage
{
    private const string Name = "Auto Daily Tribes";
    private const string Author = "XeldarAlz";
    private const string RepoUrl = "https://github.com/XeldarAlz/FFXIV-AutoDailyTribes";
    private const string DiscordUrl = "https://discord.gg/hppkAvdBEE";
    private const string PatreonUrl = "https://www.patreon.com/XeldarAlz";

    private const float MaxColumnWidth = 860f;
    private const float SectionGap = 22f;
    private const float RevealMs = 460f;
    private const float RevealStaggerMs = 110f;
    private const float RevealSlide = 14f;

    private const float HeroHeight = 184f;
    private const float HeroPad = 26f;
    private const float HeroIconSize = 108f;
    private const float HeroTextGap = 26f;
    private const float HeroLineGap = 6f;
    private const float ChipPadX = 10f;
    private const float ChipPadY = 4f;
    private const float ChipIconGap = 6f;
    private const float FooterGap = 6f;

    private static readonly Vector2[] BloomOffsets = [new(1.6f, 0f), new(-1.6f, 0f), new(0f, 1.6f), new(0f, -1.6f)];
    private static readonly string version = typeof(AboutPage).Assembly.GetName().Version?.ToString() ?? "?";

    private long openTick = long.MinValue / 2;
    private float columnX;
    private float columnWidth;

    public void Draw(long shownTick)
    {
        openTick = shownTick;
        var scale = ImGuiHelpers.GlobalScale;
        var available = ImGui.GetContentRegionAvail().X;
        columnWidth = MathF.Min(available, MaxColumnWidth * scale);
        columnX = ImGui.GetCursorScreenPos().X + (available - columnWidth) * 0.5f;

        Styling.VSpace(6f);
        RevealSection(0, DrawHero);
        Styling.VSpace(SectionGap);
        RevealSection(1, DrawCommunity);
        Styling.VSpace(SectionGap);
        RevealSection(2, DrawSupport);
        Styling.VSpace(SectionGap);
        RevealSection(3, DrawFooter);
        Styling.VSpace(SectionGap);
    }

    private void RevealSection(int index, Action draw)
    {
        var elapsed = Environment.TickCount64 - openTick;
        var progress = Motion.Reduced ? 1f : Motion.EaseOutCubic(Math.Clamp((elapsed - index * RevealStaggerMs) / RevealMs, 0f, 1f));
        var cursorY = ImGui.GetCursorScreenPos().Y + (1f - progress) * RevealSlide * ImGuiHelpers.GlobalScale;
        ImGui.SetCursorScreenPos(new Vector2(columnX, cursorY));
        using (Motion.PushAlpha(progress))
        {
            draw();
        }
    }

    private void DrawHero()
    {
        var scale = ImGuiHelpers.GlobalScale;
        var dl = ImGui.GetWindowDrawList();
        var origin = ImGui.GetCursorScreenPos();
        var height = HeroHeight * scale;
        var max = origin + new Vector2(columnWidth, height);
        var rounding = Styling.CardRounding * 1.6f * scale;

        Paint.Shadow(dl, origin, max, rounding, 16f * scale, 0.5f);
        Paint.Gradient(dl, origin, max, Styling.Tint(Styling.Surface2, Styling.AccentTeal, 0.20f), Styling.Tint(Styling.Surface0, Styling.AccentTeal, 0.05f), rounding);
        DrawAurora(dl, origin, max);
        Paint.TopLight(dl, origin, max, rounding, 0.14f);
        Paint.Stroke(dl, origin, max, Styling.WithAlpha(Styling.AccentTeal, 0.35f), rounding, 1.2f * scale);

        var pad = HeroPad * scale;
        var iconSize = MathF.Min(HeroIconSize * scale, height - pad * 2f);
        var textGap = HeroTextGap * scale;
        var textWidth = MathF.Min(HeroTextWidth(), MathF.Max(1f, columnWidth - pad * 2f - iconSize - textGap));
        var groupX = origin.X + (columnWidth - iconSize - textGap - textWidth) * 0.5f;
        var bob = Motion.Reduced ? 0f : Motion.Wave(3200) * 3f * scale;
        var iconCenter = new Vector2(groupX + iconSize * 0.5f, origin.Y + height * 0.5f + bob);
        DrawHeroIcon(dl, iconCenter, iconSize);
        DrawHeroText(groupX + iconSize + textGap, textWidth, origin.Y, height);

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(new Vector2(columnWidth, height));
    }

    private static void DrawAurora(ImDrawListPtr dl, Vector2 min, Vector2 max)
    {
        var width = max.X - min.X;
        var height = max.Y - min.Y;
        dl.PushClipRect(min, max, true);
        SoftBlob(dl, min + new Vector2(width * (0.18f + 0.08f * Motion.Wave(11000)), height * (0.30f + 0.20f * Motion.Wave(13700))), height * 1.3f, Styling.AccentTeal, 0.10f);
        SoftBlob(dl, min + new Vector2(width * (0.72f + 0.10f * Motion.Wave(15500)), height * (0.10f + 0.25f * Motion.Wave(9300))), height * 1.1f, Styling.AccentViolet, 0.08f);
        SoftBlob(dl, min + new Vector2(width * (0.95f + 0.05f * Motion.Wave(17900)), height * (0.95f + 0.10f * Motion.Wave(12100))), height * 1.2f, Styling.AccentPink, 0.06f);
        dl.PopClipRect();
    }

    // Many faint rings stacked together fall off smoothly; a handful of stronger ones shows as visible bands.
    private static void SoftBlob(ImDrawListPtr dl, Vector2 center, float radius, Vector4 color, float peak)
    {
        const int layers = 18;
        var layerAlpha = peak * 2f / layers;
        for (var layer = layers; layer >= 1; layer--)
        {
            var fraction = layer / (float)layers;
            var alpha = layerAlpha * (1f - Motion.Smoothstep(fraction)) + layerAlpha * 0.15f;
            dl.AddCircleFilled(center, radius * fraction, Paint.Col(Styling.WithAlpha(color, alpha)), 64);
        }
    }

    private static void DrawHeroIcon(ImDrawListPtr dl, Vector2 center, float size)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var half = size * 0.5f;
        ProgressRing.Glow(center, half * 1.05f, Styling.AccentTeal, 0.5f + 0.45f * Styling.Pulse(Styling.PulseBreath));
        ProgressRing.Sweep(center, half * 1.28f, 2f * scale, Styling.AccentTealSoft, Styling.PulseOrbit, MathF.PI * 0.5f, 0.9f);
        OrbitParticles(dl, center, half * 1.28f, 3, 4600, 1, Styling.AccentTealSoft, 2.2f * scale);

        var iconMin = center - new Vector2(half, half);
        var iconMax = center + new Vector2(half, half);
        var rounding = size * 0.22f;
        AppIcon.Draw(dl, iconMin, iconMax, rounding);
        Paint.Stroke(dl, iconMin, iconMax, Styling.WithAlpha(Styling.AccentTealSoft, 0.55f), rounding, 1.5f * scale);
        IconEasterEgg(iconMin, iconMax, scale);
    }

    private static void OrbitParticles(ImDrawListPtr dl, Vector2 center, float radius, int count, double periodMs, int direction, Vector4 color, float dotRadius)
    {
        var baseAngle = -MathF.PI / 2f + direction * Styling.Phase(periodMs) * MathF.PI * 2f;
        for (var index = 0; index < count; index++)
        {
            var angle = baseAngle + index * (MathF.PI * 2f / count);
            var position = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
            dl.AddCircleFilled(position, dotRadius * 2.4f, Paint.Col(Styling.WithAlpha(color, 0.16f)));
            dl.AddCircleFilled(position, dotRadius, Paint.Col(color));
        }
    }

    private static string Punchline => Svc.PluginInterface.Manifest.Punchline ?? string.Empty;

    private static float HeroTextWidth()
    {
        float width;
        using (Fonts.PushTitle())
        {
            width = TextDraw.Measure(Name).X;
        }

        width = MathF.Max(width, TextDraw.Measure(Punchline).X);
        using (Fonts.PushCaption())
        {
            return MathF.Max(width, ChipWidth(FontAwesomeIcon.Tag, Loc.T(L.About.Version, version)));
        }
    }

    private static void DrawHeroText(float x, float width, float top, float height)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var punchline = Punchline;
        float titleHeight;
        using (Fonts.PushTitle())
        {
            titleHeight = TextDraw.LineHeight();
        }

        var punchlineHeight = punchline.Length > 0 ? TextDraw.LineHeight() + HeroLineGap * scale : 0f;
        float chipHeight;
        using (Fonts.PushCaption())
        {
            chipHeight = TextDraw.LineHeight() + ChipPadY * 2f * scale;
        }

        var blockHeight = titleHeight + HeroLineGap * scale + punchlineHeight + HeroLineGap * scale + chipHeight;
        var y = top + (height - blockHeight) * 0.5f;
        DrawShimmerTitle(Name, new Vector2(x, y), width);
        y += titleHeight + HeroLineGap * scale;

        if (punchline.Length > 0)
        {
            TextDraw.At(TextDraw.Truncate(punchline, width), new Vector2(x, y), Styling.TextSecondary);
            y += punchlineHeight;
        }

        y += HeroLineGap * scale;
        using (Fonts.PushCaption())
        {
            StaticChip(FontAwesomeIcon.Tag, Loc.T(L.About.Version, version), Styling.AccentTeal, new Vector2(x, y), chipHeight);
        }
    }

    private static void DrawShimmerTitle(string text, Vector2 position, float width)
    {
        using var font = Fonts.PushTitle();
        var size = TextDraw.Measure(text);
        var dl = ImGui.GetWindowDrawList();
        dl.PushClipRect(position, position + new Vector2(width, size.Y), true);
        var bloom = Styling.WithAlpha(Styling.AccentTeal, 0.22f);
        for (var index = 0; index < BloomOffsets.Length; index++)
        {
            TextDraw.At(text, position + BloomOffsets[index] * ImGuiHelpers.GlobalScale, bloom);
        }

        TextDraw.At(text, position, Styling.TextStrong);
        var bandWidth = size.X * 0.4f;
        var bandCenter = position.X - bandWidth + Styling.Phase(Styling.PulseOrbit) * (size.X + bandWidth * 2f);
        dl.PushClipRect(new Vector2(bandCenter - bandWidth * 0.5f, position.Y), new Vector2(bandCenter + bandWidth * 0.5f, position.Y + size.Y), true);
        TextDraw.At(text, position, Styling.AccentTealSoft);
        dl.PopClipRect();
        dl.PopClipRect();
    }

    private static float ChipWidth(FontAwesomeIcon icon, string label)
    {
        var scale = ImGuiHelpers.GlobalScale;
        return ChipPadX * 2f * scale + TextDraw.IconSize(icon).X + ChipIconGap * scale + TextDraw.Measure(label).X;
    }

    private static float StaticChip(FontAwesomeIcon icon, string label, Vector4 accent, Vector2 origin, float height)
    {
        var width = ChipWidth(icon, label);
        var max = origin + new Vector2(width, height);
        Paint.Pill(ImGui.GetWindowDrawList(), origin, max, Styling.WithAlpha(accent, 0.12f), Styling.WithAlpha(accent, 0.38f));
        DrawChipContent(icon, label, accent, Styling.TextSecondary, origin, height);
        return width;
    }

    private static void DrawChipContent(FontAwesomeIcon icon, string label, Vector4 iconColor, Vector4 textColor, Vector2 origin, float height)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var midY = origin.Y + height * 0.5f;
        var iconSize = TextDraw.IconSize(icon);
        var x = origin.X + ChipPadX * scale;
        TextDraw.Icon(icon, new Vector2(x, midY - iconSize.Y * 0.5f), iconColor);
        x += iconSize.X + ChipIconGap * scale;
        TextDraw.At(label, new Vector2(x, midY - TextDraw.LineHeight() * 0.5f), textColor);
    }

    private void DrawFooter()
    {
        var scale = ImGuiHelpers.GlobalScale;
        var origin = ImGui.GetCursorScreenPos();
        using var font = Fonts.PushCaption();
        var label = Loc.T(L.About.MadeBy, Author);
        var iconSize = TextDraw.IconSize(FontAwesomeIcon.Code);
        var labelSize = TextDraw.Measure(label);
        var gap = FooterGap * scale;
        var x = origin.X + (columnWidth - iconSize.X - gap - labelSize.X) * 0.5f;
        var twinkle = Styling.Pulse(2600.0);
        TextDraw.Icon(FontAwesomeIcon.Code, new Vector2(x, origin.Y + (labelSize.Y - iconSize.Y) * 0.5f),
            Vector4.Lerp(Styling.AccentBlue, Styling.Lighten(Styling.AccentBlueSoft, 0.3f), twinkle));
        TextDraw.At(label, new Vector2(x + iconSize.X + gap, origin.Y), Styling.TextDim);
        ImGui.Dummy(new Vector2(columnWidth, labelSize.Y));
    }

    private static void SectionHeader(FontAwesomeIcon icon, string label, Vector4 accent, float width)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var origin = ImGui.GetCursorScreenPos();
        var iconSize = TextDraw.IconSize(icon);
        var labelSize = TextDraw.SmallCapsSize(label);
        var midY = origin.Y + iconSize.Y * 0.5f;
        TextDraw.Icon(icon, origin, accent);
        var labelX = origin.X + iconSize.X + 8f * scale;
        TextDraw.SmallCaps(label, new Vector2(labelX, midY - labelSize.Y * 0.5f), Styling.TextDim);
        var lineStart = labelX + labelSize.X + 12f * scale;
        Paint.GradientH(ImGui.GetWindowDrawList(), new Vector2(lineStart, midY), new Vector2(origin.X + width, midY + 1f),
            Styling.WithAlpha(accent, 0.5f), Styling.WithAlpha(accent, 0f), 0f);
        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(new Vector2(width, iconSize.Y));
        Styling.VSpace(4f);
    }

    private static bool OpenOrCopy(Hit.Result hit, string url)
    {
        if (!hit.Hovered)
        {
            return false;
        }

        Tooltip.Show(Loc.T(L.Common.OpenCopyHint));
        if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
        {
            ImGui.SetClipboardText(url);
        }

        if (!hit.Clicked)
        {
            return false;
        }

        UrlActions.Open(url);
        return true;
    }
}
