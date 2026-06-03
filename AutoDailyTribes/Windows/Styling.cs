using AutoDailyTribes.Core.Tribes;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoDailyTribes.Windows;

internal static class Styling
{
    public static readonly Vector4 AccentTeal     = new(0.32f, 0.78f, 0.78f, 1.00f);
    public static readonly Vector4 AccentTealSoft = new(0.50f, 0.90f, 0.90f, 1.00f);
    public static readonly Vector4 AccentMint     = new(0.46f, 0.86f, 0.66f, 1.00f);
    public static readonly Vector4 AccentAmber    = new(0.92f, 0.74f, 0.34f, 1.00f);
    public static readonly Vector4 AccentRose     = new(0.93f, 0.42f, 0.50f, 1.00f);

    public static readonly Vector4 KindCombat   = new(0.90f, 0.42f, 0.45f, 1.00f);
    public static readonly Vector4 KindCrafter  = new(0.95f, 0.74f, 0.36f, 1.00f);
    public static readonly Vector4 KindGatherer = new(0.50f, 0.82f, 0.52f, 1.00f);
    public static readonly Vector4 KindMixed    = new(0.72f, 0.55f, 0.93f, 1.00f);

    public static readonly Vector4 EraARR  = new(0.92f, 0.78f, 0.42f, 0.08f);
    public static readonly Vector4 EraHW   = new(0.58f, 0.72f, 0.92f, 0.08f);
    public static readonly Vector4 EraSB   = new(0.95f, 0.55f, 0.48f, 0.08f);
    public static readonly Vector4 EraShB  = new(0.78f, 0.62f, 0.96f, 0.08f);
    public static readonly Vector4 EraEW   = new(0.92f, 0.62f, 0.78f, 0.08f);
    public static readonly Vector4 EraDT   = new(0.96f, 0.74f, 0.52f, 0.08f);

    public static readonly Vector4 CardBg        = new(0.075f, 0.090f, 0.105f, 0.85f);
    public static readonly Vector4 CardBgSoft    = new(0.090f, 0.105f, 0.120f, 0.55f);
    public static readonly Vector4 CardBgHover   = new(0.105f, 0.125f, 0.145f, 0.95f);
    public static readonly Vector4 BorderDim     = new(0.22f, 0.25f, 0.30f, 1.00f);
    public static readonly Vector4 BorderActive  = new(0.32f, 0.78f, 0.78f, 1.00f);
    public static readonly Vector4 BorderLocked  = new(0.18f, 0.18f, 0.22f, 1.00f);

    public static readonly Vector4 TextStrong    = new(0.96f, 0.96f, 0.97f, 1.00f);
    public static readonly Vector4 TextSecondary = new(0.78f, 0.80f, 0.84f, 1.00f);
    public static readonly Vector4 TextDim       = new(0.55f, 0.58f, 0.62f, 1.00f);
    public static readonly Vector4 TextMuted     = new(0.40f, 0.42f, 0.46f, 1.00f);

    public const double PulseMedium = 800.0;

    public static float Pulse(double periodMs = PulseMedium)
    {
        var t = (Environment.TickCount % periodMs) / periodMs;
        return (float)((Math.Sin(t * Math.PI * 2.0) + 1.0) * 0.5);
    }

    public static Vector4 PulseColor(Vector4 a, Vector4 b, double periodMs = PulseMedium)
        => Vector4.Lerp(a, b, Pulse(periodMs));

    public static IDisposable PushCardStyle()
    {
        var p = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 7f * ImGuiHelpers.GlobalScale);
        p.Push(ImGuiStyleVar.ChildBorderSize, 1f);
        p.Push(ImGuiStyleVar.WindowPadding, new Vector2(11, 9) * ImGuiHelpers.GlobalScale);
        p.Push(ImGuiStyleVar.FrameRounding, 5f);
        return p;
    }

    public static IDisposable PushWindowStyle()
    {
        var p = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 5f);
        p.Push(ImGuiStyleVar.WindowRounding, 7f);
        p.Push(ImGuiStyleVar.ChildRounding, 7f);
        p.Push(ImGuiStyleVar.ItemSpacing, new Vector2(8, 7) * ImGuiHelpers.GlobalScale);
        return p;
    }

    public static ImRaii.ColorDisposable PushAccentButtonColors()
        => ImRaii.PushColor(ImGuiCol.Button, AccentTeal * 0.55f)
            .Push(ImGuiCol.ButtonHovered, AccentTeal * 0.75f)
            .Push(ImGuiCol.ButtonActive, AccentTeal);

    public static void SectionLabel(string label)
    {
        using (ImRaii.PushColor(ImGuiCol.Text, TextDim))
            ImGui.TextUnformatted(label.ToUpperInvariant());
    }

    public static Vector4 KindColor(TribeKind kind) => kind switch
    {
        TribeKind.Combat   => KindCombat,
        TribeKind.Crafter  => KindCrafter,
        TribeKind.Gatherer => KindGatherer,
        TribeKind.Mixed    => KindMixed,
        _ => TextDim,
    };

    public static Vector4 EraTint(TribeEra era) => era switch
    {
        TribeEra.ARR  => EraARR,
        TribeEra.HW   => EraHW,
        TribeEra.SB   => EraSB,
        TribeEra.ShB  => EraShB,
        TribeEra.EW   => EraEW,
        TribeEra.DT   => EraDT,
        _ => CardBg,
    };
}
