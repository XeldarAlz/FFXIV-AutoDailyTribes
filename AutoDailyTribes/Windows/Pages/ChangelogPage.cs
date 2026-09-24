using AutoDailyTribes.Core.Changelog;
using AutoDailyTribes.Core.Localization;
using AutoDailyTribes.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using System.Globalization;
using System.Numerics;

namespace AutoDailyTribes.Windows.Pages;

internal sealed class ChangelogPage
{
    private const float RailWidth = 26f;
    private const float RailDotRadius = 5f;
    private const float CardGap = 14f;
    private const float CardPadX = 18f;
    private const float CardPadY = 16f;
    private const float HeaderGap = 10f;
    private const float SeparatorGap = 12f;
    private const float BulletColumn = 18f;
    private const float BulletRadius = 2.5f;
    private const float BulletGap = 8f;
    private const float PillPadX = 8f;
    private const float PillPadY = 3f;
    private const float PillGap = 10f;
    private const float RevealMs = 360f;
    private const float RevealStaggerMs = 80f;
    private const float RevealSlide = 10f;
    private const string DateFormat = "d MMM yyyy";
    private const string MetaSeparator = " · ";

    private readonly string[] versionLabels = new string[ChangelogData.Entries.Length];
    private readonly string[] metaLabels = new string[ChangelogData.Entries.Length];

    private LanguageInfo? labelsLanguage;
    private long visitTick = long.MinValue;
    private int unseenAtVisit;

    public void Draw(long shownTick)
    {
        BeginVisit(shownTick);
        RefreshLabels();
        PageHeader.Draw(Loc.T(L.Changelog.Title), Loc.T(L.Changelog.Subtitle));

        var entries = ChangelogData.Entries;
        for (var index = 0; index < entries.Length; index++)
        {
            var progress = Motion.Reveal(shownTick, RevealMs, index * RevealStaggerMs);
            using (Motion.PushReveal(progress, RevealSlide))
            {
                DrawEntry(index, index == entries.Length - 1);
            }
        }
    }

    // The badge clears the moment the page opens, so which entries count as new is decided once per visit
    // from the version seen before this one.
    private void BeginVisit(long shownTick)
    {
        if (visitTick == shownTick)
        {
            return;
        }

        visitTick = shownTick;
        unseenAtVisit = ChangelogData.UnseenCount(Plugin.Cfg.LastSeenChangelogVersion);
        Plugin.Cfg.MarkChangelogSeen();
    }

    private void RefreshLabels()
    {
        if (ReferenceEquals(labelsLanguage, Loc.Current))
        {
            return;
        }

        labelsLanguage = Loc.Current;
        var entries = ChangelogData.Entries;
        for (var index = 0; index < entries.Length; index++)
        {
            var entry = entries[index];
            versionLabels[index] = Loc.T(L.Changelog.Version, entry.Version);
            metaLabels[index] = string.Concat(FormatDate(entry.Date), MetaSeparator, Loc.Plural(L.Changelog.Changes, entry.Highlights.Length));
        }
    }

    private void DrawEntry(int index, bool isLast)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var entry = ChangelogData.Entries[index];
        var isNew = index < unseenAtVisit;
        var isLatest = index == 0;
        var origin = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var railWidth = RailWidth * scale;
        var cardMin = new Vector2(origin.X + railWidth, origin.Y);
        var cardWidth = width - railWidth;
        var padX = CardPadX * scale;
        var padY = CardPadY * scale;
        var textLeft = cardMin.X + padX + BulletColumn * scale;
        var textWidth = MathF.Max(1f, cardMin.X + cardWidth - padX - textLeft);

        float headerHeight;
        using (Fonts.PushHeadline())
        {
            headerHeight = TextDraw.LineHeight();
        }

        var bodyHeight = MeasureBody(entry.Highlights, textWidth);
        var separatorY = cardMin.Y + padY + headerHeight + SeparatorGap * scale;
        var cardHeight = separatorY - cardMin.Y + SeparatorGap * scale + bodyHeight + padY;
        var cardMax = new Vector2(cardMin.X + cardWidth, cardMin.Y + cardHeight);
        var dl = ImGui.GetWindowDrawList();
        var rounding = Styling.CardRounding * scale;
        var headerMidY = cardMin.Y + padY + headerHeight * 0.5f;

        DrawRail(dl, origin, headerMidY, cardHeight, isNew || isLatest, isLast);

        if (isNew)
        {
            Paint.Glow(dl, cardMin, cardMax, rounding, Styling.AccentTeal, 0.8f);
        }

        if (isLatest)
        {
            Paint.Glass(dl, cardMin, cardMax, rounding, Styling.AccentTeal, 0.08f);
            Paint.Stroke(dl, cardMin, cardMax, Styling.WithAlpha(Styling.AccentTeal, isNew ? 0.55f : 0.30f), rounding);
        }
        else
        {
            Paint.Surface(dl, cardMin, cardMax, rounding, Styling.WithAlpha(Styling.Surface1, 0.75f), Styling.WithAlpha(Styling.BorderDim, 0.5f));
        }

        var x = cardMin.X + padX;
        using (Fonts.PushHeadline())
        {
            TextDraw.At(versionLabels[index], new Vector2(x, cardMin.Y + padY), isLatest ? Styling.AccentTealSoft : Styling.TextStrong);
            x += TextDraw.Measure(versionLabels[index]).X + PillGap * scale;
        }

        if (isNew || isLatest)
        {
            x += DrawPill(dl, isNew ? L.Changelog.New : L.Changelog.Latest, x, headerMidY, isNew) + PillGap * scale;
        }

        using (Fonts.PushCaption())
        {
            var meta = metaLabels[index];
            var metaSize = TextDraw.Measure(meta);
            var metaX = cardMax.X - padX - metaSize.X;
            if (metaX > x)
            {
                TextDraw.At(meta, new Vector2(metaX, headerMidY - metaSize.Y * 0.5f), Styling.TextMuted);
            }
        }

        Paint.Hairline(dl, new Vector2(cardMin.X + padX, separatorY), new Vector2(cardMax.X - padX, separatorY));
        DrawBody(dl, entry.Highlights, cardMin.X + padX, textLeft, textWidth, separatorY + SeparatorGap * scale);

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(new Vector2(width, cardHeight + (isLast ? 0f : CardGap * scale)));
    }

    private static void DrawRail(ImDrawListPtr dl, Vector2 origin, float dotY, float cardHeight, bool highlighted, bool isLast)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var center = new Vector2(origin.X + RailWidth * scale * 0.5f - 2f * scale, dotY);
        var radius = RailDotRadius * scale;
        if (!isLast)
        {
            var lineBottom = origin.Y + cardHeight + CardGap * scale + (dotY - origin.Y) - radius;
            dl.AddLine(center + new Vector2(0f, radius + 2f * scale), new Vector2(center.X, lineBottom),
                Paint.Col(Styling.WithAlpha(Styling.BorderDim, 0.8f)), 1.5f * scale);
        }

        if (highlighted)
        {
            Paint.Dot(dl, center, radius, Styling.AccentTeal, 0.28f);
            return;
        }

        dl.AddCircle(center, radius, Paint.Col(Styling.WithAlpha(Styling.TextDim, 0.8f)), 0, 1.5f * scale);
    }

    private static float DrawPill(ImDrawListPtr dl, LocString label, float x, float midY, bool filled)
    {
        var scale = ImGuiHelpers.GlobalScale;
        using (Fonts.PushCaption())
        {
            var text = TextDraw.Upper(Loc.T(label));
            var size = TextDraw.Measure(text);
            var min = new Vector2(x, midY - size.Y * 0.5f - PillPadY * scale);
            var max = new Vector2(x + size.X + PillPadX * 2f * scale, midY + size.Y * 0.5f + PillPadY * scale);
            if (filled)
            {
                Paint.Pill(dl, min, max, Styling.AccentTeal, Styling.WithAlpha(Styling.AccentTealSoft, 0.6f));
                TextDraw.At(text, new Vector2(min.X + PillPadX * scale, midY - size.Y * 0.5f), Styling.ForegroundOn(Styling.AccentTeal));
            }
            else
            {
                Paint.Pill(dl, min, max, Styling.WithAlpha(Styling.AccentTeal, 0.16f), Styling.WithAlpha(Styling.AccentTeal, 0.4f));
                TextDraw.At(text, new Vector2(min.X + PillPadX * scale, midY - size.Y * 0.5f), Styling.AccentTealSoft);
            }

            return max.X - min.X;
        }
    }

    private static float MeasureBody(LocString[] highlights, float textWidth)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var height = 0f;
        for (var index = 0; index < highlights.Length; index++)
        {
            if (index > 0)
            {
                height += BulletGap * scale;
            }

            height += TextDraw.MeasureWrapped(Loc.T(highlights[index]), textWidth).Y;
        }

        return height;
    }

    private static void DrawBody(ImDrawListPtr dl, LocString[] highlights, float bulletX, float textLeft, float textWidth, float top)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var lineHeight = TextDraw.LineHeight();
        var y = top;
        for (var index = 0; index < highlights.Length; index++)
        {
            if (index > 0)
            {
                y += BulletGap * scale;
            }

            var text = Loc.T(highlights[index]);
            dl.AddCircleFilled(new Vector2(bulletX + BulletRadius * scale, y + lineHeight * 0.5f), BulletRadius * scale, Paint.Col(Styling.AccentTeal));
            TextDraw.Wrapped(text, new Vector2(textLeft, y), textWidth, Styling.TextSecondary);
            y += TextDraw.MeasureWrapped(text, textWidth).Y;
        }
    }

    private static string FormatDate(string isoDate)
        => DateTime.TryParse(isoDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed.ToString(DateFormat, Loc.Culture)
            : isoDate;
}
