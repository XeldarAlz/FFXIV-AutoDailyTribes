using AutoDailyTribes.Core.External;
using AutoDailyTribes.Core.Localization;
using AutoDailyTribes.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using System.Numerics;

namespace AutoDailyTribes.Windows.Pages;

internal sealed class PluginsPage
{
    private const float PadX = 16f;
    private const float DiscRadius = 17f;

    public void Draw()
    {
        var missing = 0;
        foreach (var plugin in ExternalPlugins.All)
        {
            if (ExternalPlugins.Catalog[plugin].Required && !ExternalPlugins.IsInstalled(plugin)) missing++;
        }

        var status = missing == 0 ? Loc.T(L.Plugins.AllInstalled) : Loc.Plural(L.Plugins.Missing, missing);
        PageHeader.Draw(Loc.T(L.Plugins.Title), status, missing == 0 ? Styling.AccentMint : Styling.AccentRose);

        foreach (var plugin in ExternalPlugins.All)
        {
            DrawCard(plugin);
            Styling.VSpace(4f);
        }

        Styling.VSpace(6f);
        using (Fonts.PushCaption())
        {
            var footer = Loc.T(L.Plugins.Footer);
            var origin = ImGui.GetCursorScreenPos();
            var width = ImGui.GetContentRegionAvail().X;
            TextDraw.Wrapped(footer, origin, width, Styling.TextMuted);
            ImGui.Dummy(new Vector2(width, TextDraw.MeasureWrapped(footer, width).Y));
        }
    }

    private static string Purpose(ExternalPlugin plugin) => plugin switch
    {
        ExternalPlugin.Vnavmesh     => Loc.T(L.Plugins.PurposeVnavmesh),
        ExternalPlugin.Questionable => Loc.T(L.Plugins.PurposeQuestionable),
        ExternalPlugin.TextAdvance  => Loc.T(L.Plugins.PurposeTextAdvance),
        ExternalPlugin.Artisan      => Loc.T(L.Plugins.PurposeArtisan),
        _                           => ExternalPlugins.Catalog[plugin].Purpose,
    };

    private static void DrawCard(ExternalPlugin plugin)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var info = ExternalPlugins.Catalog[plugin];
        var installed = ExternalPlugins.IsInstalled(plugin);
        var disabled = ExternalPlugins.IsInstalledButDisabled(plugin);
        var installing = PluginInstaller.IsInstalling(plugin);

        var (icon, accent) = (installed, disabled, info.Required) switch
        {
            (true, true, _)   => (FontAwesomeIcon.ExclamationCircle, Styling.AccentAmber),
            (true, false, _)  => (FontAwesomeIcon.CheckCircle, Styling.AccentMint),
            (false, _, true)  => (FontAwesomeIcon.TimesCircle, Styling.AccentRose),
            (false, _, false) => (FontAwesomeIcon.Circle, Styling.TextDim),
        };

        var size = new Vector2(ImGui.GetContentRegionAvail().X, Layout.PluginCardHeight * scale);
        var origin = ImGui.GetCursorScreenPos();
        var end = origin + size;
        var dl = ImGui.GetWindowDrawList();
        Paint.Glass(dl, origin, end, Styling.CardRounding * scale, accent, 0.06f);

        var padX = PadX * scale;
        var midY = origin.Y + size.Y * 0.5f;
        var discRadius = DiscRadius * scale;
        var discCenter = new Vector2(origin.X + padX + discRadius, midY);
        ProgressRing.Disc(discCenter, discRadius, Styling.Tint(Styling.Surface1, accent, 0.3f));
        ProgressRing.Track(discCenter, discRadius, 1.2f * scale, Styling.WithAlpha(accent, 0.7f));
        ProgressRing.CenterIcon(discCenter, icon, accent, discRadius * 0.95f);

        var rightWidth = DrawAction(plugin, installed, disabled, installing, end, midY);

        var purpose = Purpose(plugin);
        var textX = discCenter.X + discRadius + 16f * scale;
        var maxTextWidth = end.X - padX - rightWidth - textX;
        float nameHeight;
        using (Fonts.PushHeadline())
            nameHeight = TextDraw.Measure(info.DisplayName).Y;
        float purposeHeight;
        using (Fonts.PushCaption())
            purposeHeight = TextDraw.Measure(purpose).Y;
        var top = midY - (nameHeight + 3f * scale + purposeHeight) * 0.5f;

        Vector2 nameSize;
        using (Fonts.PushHeadline())
        {
            nameSize = TextDraw.Measure(info.DisplayName);
            TextDraw.At(info.DisplayName, new Vector2(textX, top), Styling.TextStrong);
        }

        DrawRequirementTag(dl, info.Required, textX + nameSize.X + 10f * scale, top + nameHeight * 0.5f);

        using (Fonts.PushCaption())
            TextDraw.At(TextDraw.Truncate(purpose, maxTextWidth), new Vector2(textX, top + nameHeight + 3f * scale), Styling.TextDim);

        var nameMin = new Vector2(textX, top);
        var nameMax = nameMin + nameSize;
        if (Hit.HoveringRect(nameMin, nameMax))
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            Tooltip.Show(Loc.T(L.Plugins.RepoHint, info.RepoUrl));
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left)) UrlActions.Open(info.RepoUrl, log: false);
            else if (ImGui.IsMouseClicked(ImGuiMouseButton.Right)) ImGui.SetClipboardText(info.RepoUrl);
        }

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(size);
    }

    private static void DrawRequirementTag(ImDrawListPtr dl, bool required, float x, float midY)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var label = TextDraw.Upper(Loc.T(required ? L.Plugins.Required : L.Plugins.Optional));
        using (Fonts.PushCaption())
        {
            var labelSize = TextDraw.Measure(label);
            var tagMin = new Vector2(x, midY - labelSize.Y * 0.5f - 3f * scale);
            var tagMax = tagMin + labelSize + new Vector2(14f * scale, 6f * scale);
            var accent = required ? Styling.AccentTeal : Styling.TextDim;
            Paint.Pill(dl, tagMin, tagMax, Styling.WithAlpha(accent, 0.18f), Styling.WithAlpha(accent, 0.45f));
            TextDraw.Middle(label, tagMin, tagMax, required ? Styling.AccentTealSoft : Styling.TextSecondary);
        }
    }

    private static float DrawAction(ExternalPlugin plugin, bool installed, bool disabled, bool installing, Vector2 end, float midY)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var padX = PadX * scale;

        if (installed)
        {
            var (label, color, icon) = disabled
                ? (Loc.T(L.Plugins.Disabled), Styling.AccentAmber, FontAwesomeIcon.ExclamationTriangle)
                : (Loc.T(L.Plugins.Installed), Styling.AccentMint, FontAwesomeIcon.Check);
            var labelSize = TextDraw.Measure(label);
            var iconSize = TextDraw.IconSize(icon);
            var labelX = end.X - padX - labelSize.X;
            TextDraw.At(label, new Vector2(labelX, midY - labelSize.Y * 0.5f), color);
            var iconX = labelX - 6f * scale - iconSize.X;
            TextDraw.Icon(icon, new Vector2(iconX, midY - iconSize.Y * 0.5f), color);

            if (disabled && Hit.HoveringRect(new Vector2(iconX, midY - labelSize.Y), new Vector2(end.X - padX, midY + labelSize.Y)))
            {
                Tooltip.Show(Loc.T(L.Plugins.TextAdvanceDisabled));
            }

            return end.X - padX - iconX + 12f * scale;
        }

        var text = Loc.T(installing ? L.Plugins.Installing : L.Plugins.Install);
        var width = PillButton.Width(text, FontAwesomeIcon.Download);
        ImGui.SetCursorScreenPos(new Vector2(end.X - padX - width, midY - 15f * scale));
        ImGui.PushID((nint)((int)plugin + 1));
        if (PillButton.Draw("##adt_install", text, Styling.AccentTeal, PillButton.Emphasis.Filled, FontAwesomeIcon.Download, enabled: !installing, height: 30f))
        {
            _ = PluginInstaller.Install(plugin);
        }

        ImGui.PopID();
        return width + 12f * scale;
    }
}
