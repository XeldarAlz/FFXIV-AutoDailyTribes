using AutoDailyTribes.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoDailyTribes.Windows.Pages;

internal sealed class AboutPage
{
    private const string Name = "Auto Daily Tribes";
    private const string RepoUrl = "https://github.com/XeldarAlz/FFXIV-AutoDailyTribes";
    private const string PatreonUrl = "https://www.patreon.com/XeldarAlz";
    private const string DiscordUrl = "https://discord.gg/3HbJCscMyS";
    private const string HubUrl = "https://github.com/XeldarAlz/DalamudPlugins";
    private const string Author = "XeldarAlz";

    private const string IssuesUrl = RepoUrl + "/issues";
    private const string DiscussionsUrl = RepoUrl + "/discussions";
    private const string SecurityUrl = RepoUrl + "/security/advisories/new";

    private const string ConnectTitle = "Connect";
    private const string SupportTitle = "Made with care";
    private const string SupportBody = "I build and maintain this in my spare time. If it has helped you, a Patreon membership lets me keep improving it. No pressure, and thank you for being here.";
    private const string SupportButton = "Support on Patreon";
    private const string PatreonHint = "Open Patreon · right-click to copy";
    private const string LinkHint = "Click to open · right-click to copy";
    private const string MadeBy = "Made by " + Author;

    private const float RevealMs = 420f;
    private const float RevealStaggerMs = 95f;
    private const float RevealSlide = 12f;
    private const float HeroIconSize = 148f;
    private const float HeroRingRadius = 120f;

    private static readonly (FontAwesomeIcon Icon, string Label, string Url, Vector4 Accent)[] Links =
    [
        (FontAwesomeIcon.CodeBranch, "GitHub", RepoUrl, Styling.AccentViolet),
        (FontAwesomeIcon.Hashtag, "Discord", DiscordUrl, Styling.AccentDiscord),
        (FontAwesomeIcon.Comments, "Discussions", DiscussionsUrl, Styling.AccentBlue),
        (FontAwesomeIcon.Bug, "Report a bug", IssuesUrl, Styling.AccentRose),
        (FontAwesomeIcon.ThLarge, "More plugins", HubUrl, Styling.AccentMint),
        (FontAwesomeIcon.ShieldAlt, "Security", SecurityUrl, Styling.AccentAmber),
    ];

    private static readonly Vector2[] BloomOffsets =
    [
        new(1.6f, 0f), new(-1.6f, 0f), new(0f, 1.6f), new(0f, -1.6f),
    ];

    private static readonly FactCategory[] Categories =
    [
        new(FontAwesomeIcon.Heart, "A little reminder", Styling.AccentRose,
        [
            "Been at it a while? Roll your shoulders and take one slow breath.",
            "Hydration check. When did you last drink some water?",
            "Blink a few times and let your eyes rest for a moment.",
            "Stand up, stretch, and shake out your hands. Future you says thanks.",
            "Sit up and settle in comfortably. Your back will thank you later.",
            "Remember to eat something today. You matter more than any score.",
            "Eyes feel tired? Look at something far away for twenty seconds.",
            "Whatever you're chasing, you're allowed to take a break whenever.",
            "You're doing great. Be a little kinder to yourself today.",
            "A glass of water and a quick stretch can reset a long session.",
            "Unclench your jaw and drop your shoulders. There you go.",
            "Rest is part of the journey too. Step away whenever you need to.",
        ]),
        new(FontAwesomeIcon.Lightbulb, "Did you know?", Styling.AccentAmberSoft,
        [
            "Honey never spoils. Jars over 3,000 years old have been found still edible.",
            "Octopuses have three hearts and blue blood.",
            "A day on Venus is longer than a whole year on Venus.",
            "Bananas are berries, but strawberries aren't.",
            "There are more possible chess games than atoms in the observable universe.",
            "Sharks have been around longer than trees have.",
            "A group of flamingos is called a flamboyance.",
            "Honeybees can recognize individual human faces.",
            "Wombat droppings are cube shaped.",
            "The Eiffel Tower can grow over 15 cm taller on a hot day.",
            "Hot water can sometimes freeze faster than cold water.",
            "A bolt of lightning is roughly five times hotter than the surface of the Sun.",
        ]),
        new(FontAwesomeIcon.Star, "Words to live by", Styling.AccentMintSoft,
        [
            "Done is better than perfect. You can always polish later.",
            "Small steps every day add up to surprising distances.",
            "Comparison is the thief of joy. Run your own race.",
            "Progress, not perfection.",
            "You don't have to be great to start, but you have to start to be great.",
            "Be patient with yourself. Growth takes time.",
            "The best time to begin was yesterday. The second best is right now.",
            "Celebrate the small wins. They count too.",
            "Slow progress is still progress.",
            "Your only real competition is who you were yesterday.",
        ]),
        new(FontAwesomeIcon.GrinBeam, "Just for fun", Styling.AccentBlueSoft,
        [
            "Why don't scientists trust atoms? Because they make up everything.",
            "I would tell you a chemistry joke, but I know I wouldn't get a reaction.",
            "Why did the scarecrow win an award? He was outstanding in his field.",
            "I'm reading a book about anti-gravity. It's impossible to put down.",
            "Why don't skeletons fight each other? They don't have the guts.",
            "What do you call fake spaghetti? An impasta.",
            "Why did the bicycle fall over? It was two tired.",
            "What do you call cheese that isn't yours? Nacho cheese.",
            "I'm on a seafood diet. I see food, and I eat it.",
            "I only know 25 letters of the alphabet. I don't know y.",
        ]),
    ];

    private readonly record struct FactCategory(FontAwesomeIcon Icon, string Header, Vector4 Color, string[] Lines);

    private static readonly float[] linkWidths = new float[Links.Length];
    private static readonly List<string> bodyLines = [];
    private static readonly int[][] factBags = new int[Categories.Length][];
    private static readonly int[] factBagPositions = new int[Categories.Length];
    private static readonly int[] factLastServed = new int[Categories.Length];
    private static readonly string version = typeof(AboutPage).Assembly.GetName().Version?.ToString() ?? "?";
    private static readonly string versionLabel = $"v {version}";

    private static float bodyLinesWidth = -1f;
    private static int factCategory = -1;
    private static int factLine;
    private static bool iconHovered;

    private long openTick = long.MinValue / 2;

    public void Draw(long shownTick)
    {
        openTick = shownTick;

        using (Motion.PushAlpha(Reveal(0)))
            AmbientBackground();

        RevealSection(0, () =>
        {
            DrawHero();
            Styling.VSpace(16f);
        });
        RevealSection(1, () =>
        {
            DrawSupport();
            Styling.VSpace(16f);
        });
        RevealSection(2, () =>
        {
            SectionHeader(FontAwesomeIcon.Link, ConnectTitle, Styling.AccentBlue);
            Styling.VSpace(6f);
            DrawConnect();
            Styling.VSpace(16f);
        });
        RevealSection(3, DrawFooter);
    }

    private float Reveal(int index)
    {
        var elapsed = Environment.TickCount64 - openTick;
        var progress = (elapsed - index * RevealStaggerMs) / RevealMs;
        return Motion.Smoothstep(Math.Clamp(progress, 0f, 1f));
    }

    private void RevealSection(int index, Action draw)
    {
        var alpha = Reveal(index);
        if (alpha < 1f) ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (1f - alpha) * RevealSlide * ImGuiHelpers.GlobalScale);
        using (Motion.PushAlpha(alpha))
            draw();
    }

    private static void AmbientBackground()
    {
        var windowPos = ImGui.GetWindowPos();
        var min = windowPos + ImGui.GetWindowContentRegionMin();
        var max = windowPos + ImGui.GetWindowContentRegionMax();
        var width = max.X - min.X;
        var height = max.Y - min.Y;

        var dl = ImGui.GetWindowDrawList();
        dl.PushClipRect(min, max, true);
        SoftBlob(dl, min + new Vector2(width * (0.26f + 0.12f * Motion.Wave(11000)), height * (0.20f + 0.10f * Motion.Wave(13700))),
            width * 0.55f, Styling.AccentTeal, 0.075f);
        SoftBlob(dl, min + new Vector2(width * (0.80f + 0.12f * Motion.Wave(15500)), height * (0.32f + 0.10f * Motion.Wave(9300))),
            width * 0.48f, Styling.AccentPink, 0.060f);
        SoftBlob(dl, min + new Vector2(width * (0.55f + 0.14f * Motion.Wave(17900)), height * (0.82f + 0.08f * Motion.Wave(12100))),
            width * 0.52f, Styling.AccentViolet, 0.050f);
        dl.PopClipRect();
    }

    private static void SoftBlob(ImDrawListPtr dl, Vector2 center, float radius, Vector4 color, float peak)
    {
        const int layers = 5;
        for (var layer = layers; layer >= 1; layer--)
        {
            var layerRadius = radius * layer / layers;
            var alpha = peak * (1f - (layer - 1f) / layers);
            dl.AddCircleFilled(center, layerRadius, Paint.Col(Styling.WithAlpha(color, alpha)), 40);
        }
    }

    private static void DrawHero()
    {
        var scale = ImGuiHelpers.GlobalScale;
        var dl = ImGui.GetWindowDrawList();

        Styling.VSpace(32f);

        var start = ImGui.GetCursorScreenPos();
        var availX = ImGui.GetContentRegionAvail().X;
        var ringRadius = HeroRingRadius * scale;
        var bob = Motion.Wave(3000) * 3f * scale;
        var center = new Vector2(start.X + availX * 0.5f, start.Y + ringRadius + bob);

        ProgressRing.Glow(center, ringRadius, Styling.AccentTeal, 0.55f + 0.5f * Styling.Pulse(Styling.PulseBreath));
        ProgressRing.Track(center, ringRadius, 1.5f * scale, Styling.WithAlpha(Styling.BorderDim, 0.7f));
        ProgressRing.Sweep(center, ringRadius, 2.6f * scale, Styling.AccentTealSoft, Styling.PulseOrbit, MathF.PI * 0.55f, 1f);
        OrbitParticles(dl, center, ringRadius, 3, 4600, +1, Styling.AccentTealSoft, 2.4f * scale);
        OrbitParticles(dl, center, ringRadius * 0.74f, 2, 6000, -1, Styling.AccentMintSoft, 2.0f * scale);

        var half = HeroIconSize * 0.5f * scale;
        var iconMin = new Vector2(center.X - half, center.Y - half);
        var iconMax = new Vector2(center.X + half, center.Y + half);
        var rounding = HeroIconSize * 0.20f * scale;
        AppIcon.Draw(dl, iconMin, iconMax, rounding, 0.92f + 0.08f * Styling.Pulse(2200.0));
        Paint.Stroke(dl, iconMin, iconMax, Styling.WithAlpha(Styling.AccentTealSoft, 0.55f), rounding, 1.5f * scale);

        IconEasterEgg(iconMin, iconMax, scale);

        ImGui.SetCursorScreenPos(start);
        ImGui.Dummy(new Vector2(availX, ringRadius * 2f));

        Styling.VSpace(10f);
        ShimmerCentered(Name, Styling.TextStrong, Styling.AccentTealSoft, Styling.PulseOrbit, 0.42f);
        Styling.VSpace(9f);
        CenteredPill(versionLabel, Styling.TextSecondary, Styling.WithAlpha(Styling.AccentTeal, 0.45f), Styling.CardBgSoft);
    }

    private static void OrbitParticles(ImDrawListPtr dl, Vector2 center, float radius, int count, double periodMs, int direction, Vector4 color, float dotRadius)
    {
        var baseAngle = -MathF.PI / 2f + direction * Styling.Phase(periodMs) * MathF.PI * 2f;
        for (var index = 0; index < count; index++)
        {
            var angle = baseAngle + index * (MathF.PI * 2f / count);
            var position = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
            dl.AddCircleFilled(position, dotRadius * 2.4f, Paint.Col(Styling.WithAlpha(color, 0.16f)));
            dl.AddCircleFilled(position, dotRadius * 1.5f, Paint.Col(Styling.WithAlpha(color, 0.32f)));
            dl.AddCircleFilled(position, dotRadius, Paint.Col(color));
        }
    }

    private static void IconEasterEgg(Vector2 min, Vector2 max, float scale)
    {
        if (!Hit.HoveringRect(min, max))
        {
            iconHovered = false;
            return;
        }

        if (!iconHovered)
        {
            iconHovered = true;
            factCategory = (factCategory + 1) % Categories.Length;
            factLine = NextLineInCategory(factCategory);
        }

        var category = Categories[Math.Max(0, factCategory)];
        ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        using (Tooltip.Begin())
        {
            using (ImRaii.PushFont(UiBuilder.IconFont))
            using (ImRaii.PushColor(ImGuiCol.Text, category.Color))
                ImGui.TextUnformatted(category.Icon.ToIconString());
            ImGui.SameLine(0, 8f * scale);
            using (ImRaii.PushColor(ImGuiCol.Text, category.Color))
                ImGui.TextUnformatted(category.Header);
            ImGui.Spacing();
            Tooltip.Text(category.Lines[factLine]);
        }
    }

    private static int NextLineInCategory(int category)
    {
        var count = Categories[category].Lines.Length;
        if (factBags[category] == null || factBagPositions[category] >= count)
        {
            var avoidFirst = factBags[category] == null ? -1 : factLastServed[category];
            factBags[category] = Shuffle(count, avoidFirst);
            factBagPositions[category] = 0;
        }

        var line = factBags[category][factBagPositions[category]++];
        factLastServed[category] = line;
        return line;
    }

    private static int[] Shuffle(int count, int avoidFirst)
    {
        var order = new int[count];
        for (var index = 0; index < count; index++) order[index] = index;
        for (var index = count - 1; index > 0; index--)
        {
            var swap = Random.Shared.Next(index + 1);
            (order[index], order[swap]) = (order[swap], order[index]);
        }

        if (count > 1 && order[0] == avoidFirst)
        {
            var swap = 1 + Random.Shared.Next(count - 1);
            (order[0], order[swap]) = (order[swap], order[0]);
        }

        return order;
    }

    private static void DrawSupport()
    {
        var scale = ImGuiHelpers.GlobalScale;
        var dl = ImGui.GetWindowDrawList();
        var pulse = Styling.Pulse(Styling.PulseBreath);
        var accent = Styling.PulseColor(Styling.AccentPink, Styling.AccentViolet, 5200.0);

        var slotOrigin = ImGui.GetCursorScreenPos();
        var fullAvail = ImGui.GetContentRegionAvail().X;
        var margin = 24f * scale;
        var origin = new Vector2(slotOrigin.X + margin, slotOrigin.Y);
        var availX = fullAvail - margin * 2f;
        var pad = 16f * scale;
        var medallionRadius = 22f * scale;
        var buttonHeight = 36f * scale;
        var innerWidth = availX - pad * 2f;
        var lineHeight = ImGui.GetTextLineHeight();
        var spacing = ImGui.GetStyle().ItemSpacing.Y;
        float titleHeight;
        using (Fonts.PushHeadline())
            titleHeight = ImGui.GetTextLineHeight();

        EnsureBodyLines(innerWidth);
        var bodyHeight = bodyLines.Count * lineHeight + MathF.Max(0, bodyLines.Count - 1) * spacing;
        var height = pad + medallionRadius * 2f + 12f * scale + titleHeight + spacing + bodyHeight + 14f * scale + buttonHeight + pad;

        var end = new Vector2(origin.X + availX, origin.Y + height);
        var centerX = origin.X + availX * 0.5f;
        var rounding = Styling.CardRounding * scale;

        Paint.Fill(dl, origin, end, Vector4.Lerp(Styling.CardBg, Styling.AccentPink, 0.07f), rounding);
        Paint.Stroke(dl, origin, end, Styling.WithAlpha(accent, 0.55f + 0.35f * pulse), rounding, 1.5f);

        var beat = Heartbeat(1400.0);
        var medallionCenter = new Vector2(centerX, origin.Y + pad + medallionRadius);
        ProgressRing.Glow(medallionCenter, medallionRadius, accent, 0.4f + 0.7f * beat);
        dl.AddCircleFilled(medallionCenter, medallionRadius, Paint.Col(Vector4.Lerp(Styling.CardBg, accent, 0.28f)));
        ProgressRing.Track(medallionCenter, medallionRadius, 1.5f * scale, Styling.WithAlpha(accent, 0.85f));
        ProgressRing.CenterIcon(medallionCenter, FontAwesomeIcon.Heart, Styling.Lighten(accent, 0.25f), medallionRadius * (0.80f + 0.22f * beat));

        var textY = origin.Y + pad + medallionRadius * 2f + 12f * scale;
        using (Fonts.PushHeadline())
            TextDraw.Center(SupportTitle, centerX, textY, Styling.TextStrong);
        textY += titleHeight + spacing;
        for (var lineIndex = 0; lineIndex < bodyLines.Count; lineIndex++)
        {
            TextDraw.Center(bodyLines[lineIndex], centerX, textY, Styling.TextSecondary);
            textY += lineHeight + spacing;
        }

        var buttonOrigin = new Vector2(origin.X + pad, end.Y - pad - buttonHeight);
        PatreonButton(buttonOrigin, new Vector2(innerWidth, buttonHeight), accent);

        ImGui.SetCursorScreenPos(slotOrigin);
        ImGui.Dummy(new Vector2(fullAvail, height));
    }

    // ImGui wraps text but always left-aligns it, so the paragraph is broken into lines by hand so
    // each can be centered. The lines only change with the available width, so they are cached.
    private static void EnsureBodyLines(float width)
    {
        if (MathF.Abs(width - bodyLinesWidth) < 0.5f) return;

        bodyLinesWidth = width;
        bodyLines.Clear();
        var words = SupportBody.Split(' ');
        var current = string.Empty;
        for (var wordIndex = 0; wordIndex < words.Length; wordIndex++)
        {
            var candidate = current.Length == 0 ? words[wordIndex] : current + " " + words[wordIndex];
            if (current.Length > 0 && ImGui.CalcTextSize(candidate).X > width)
            {
                bodyLines.Add(current);
                current = words[wordIndex];
                continue;
            }

            current = candidate;
        }

        if (current.Length > 0) bodyLines.Add(current);
    }

    private static void PatreonButton(Vector2 origin, Vector2 size, Vector4 accent)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var dl = ImGui.GetWindowDrawList();
        var end = origin + size;
        var hover = Hit.HoveringRect(origin, end);
        var rounding = size.Y * 0.5f;

        var fill = (hover ? Styling.Lighten(accent, 0.16f) : accent) with { W = 1f };

        var glowPulse = 0.5f + 0.5f * Styling.Pulse(Styling.PulseBreath);
        for (var layer = 3; layer >= 1; layer--)
        {
            var grow = layer * 2.6f * scale;
            var alpha = 0.06f * layer * glowPulse * (hover ? 1.8f : 1f);
            dl.AddRectFilled(origin - new Vector2(grow, grow), end + new Vector2(grow, grow), Paint.Col(Styling.WithAlpha(fill, alpha)), rounding + grow);
        }

        Paint.Fill(dl, origin, end, fill, rounding);
        Paint.TopLight(dl, origin, end, rounding, 0.22f);
        Sheen(dl, origin, size, 3000.0);
        Paint.Stroke(dl, origin, end, new Vector4(1f, 1f, 1f, hover ? 0.42f : 0.18f), rounding);

        var iconSize = TextDraw.IconSize(FontAwesomeIcon.HandHoldingHeart);
        var labelSize = TextDraw.Measure(SupportButton);
        var innerGap = 9f * scale;
        var contentWidth = iconSize.X + innerGap + labelSize.X;
        var startX = origin.X + (size.X - contentWidth) * 0.5f;
        var midY = origin.Y + size.Y * 0.5f;

        TextDraw.Icon(FontAwesomeIcon.HandHoldingHeart, new Vector2(startX, midY - iconSize.Y * 0.5f), Styling.TextStrong);
        TextDraw.At(SupportButton, new Vector2(startX + iconSize.X + innerGap, midY - labelSize.Y * 0.5f), Styling.TextStrong);

        if (!hover) return;
        ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        Tooltip.Show(PatreonHint);
        if (ImGui.IsMouseClicked(ImGuiMouseButton.Left)) UrlActions.Open(PatreonUrl);
        else if (ImGui.IsMouseClicked(ImGuiMouseButton.Right)) ImGui.SetClipboardText(PatreonUrl);
    }

    private static void Sheen(ImDrawListPtr dl, Vector2 origin, Vector2 size, double periodMs)
    {
        var phase = Styling.Phase(periodMs);
        if (phase > 0.35f) return;
        var sweep = phase / 0.35f;

        dl.PushClipRect(origin, origin + size, true);
        var slant = size.Y * 0.55f;
        var travel = size.X + slant + 40f;
        var centerX = origin.X - 20f + sweep * travel;
        const int half = 15;
        for (var offset = -half; offset <= half; offset++)
        {
            var alpha = 0.16f * (1f - MathF.Abs(offset) / (float)half);
            var x = centerX + offset;
            dl.AddLine(new Vector2(x + slant, origin.Y), new Vector2(x, origin.Y + size.Y), Paint.Col(new Vector4(1f, 1f, 1f, alpha)), 1.3f);
        }

        dl.PopClipRect();
    }

    private static void DrawConnect()
    {
        var scale = ImGuiHelpers.GlobalScale;
        var gap = 7f * scale;
        var avail = ImGui.GetContentRegionAvail().X;
        var pillHeight = ImGui.GetFrameHeight() * 1.15f;

        for (var index = 0; index < Links.Length; index++) linkWidths[index] = PillWidth(Links[index].Icon, Links[index].Label);

        var rowStart = 0;
        while (rowStart < Links.Length)
        {
            var rowEnd = rowStart;
            var rowWidth = 0f;
            while (rowEnd < Links.Length)
            {
                var next = rowEnd == rowStart ? linkWidths[rowEnd] : rowWidth + gap + linkWidths[rowEnd];
                if (rowEnd > rowStart && next > avail) break;
                rowWidth = next;
                rowEnd++;
            }

            var startX = ImGui.GetCursorPosX() + MathF.Max(0f, (avail - rowWidth) * 0.5f);
            for (var index = rowStart; index < rowEnd; index++)
            {
                if (index == rowStart) ImGui.SetCursorPosX(startX);
                else ImGui.SameLine(0, gap);
                var (icon, label, url, accent) = Links[index];
                LinkPill(icon, label, url, accent, new Vector2(linkWidths[index], pillHeight));
            }

            rowStart = rowEnd;
        }
    }

    private static float PillWidth(FontAwesomeIcon icon, string label)
    {
        var scale = ImGuiHelpers.GlobalScale;
        return TextDraw.IconSize(icon).X + 6f * scale + TextDraw.Measure(label).X + 14f * scale * 2f;
    }

    private static void LinkPill(FontAwesomeIcon icon, string label, string url, Vector4 accent, Vector2 size)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var slotOrigin = ImGui.GetCursorScreenPos();
        var hovered = Hit.HoveringRect(slotOrigin, slotOrigin + size);
        var hover = Motion.Hover(Motion.Key(url), hovered);

        var lift = hover * 2.5f * scale;
        var origin = slotOrigin - new Vector2(0f, lift);
        var end = origin + size;
        var dl = ImGui.GetWindowDrawList();
        var rounding = size.Y * 0.5f;

        if (hover > 0.01f)
        {
            for (var layer = 2; layer >= 1; layer--)
            {
                var grow = layer * 2.4f * scale;
                dl.AddRectFilled(origin - new Vector2(grow, grow), end + new Vector2(grow, grow), Paint.Col(Styling.WithAlpha(accent, 0.05f * layer * hover)), rounding + grow);
            }
        }

        var background = Vector4.Lerp(Styling.CardBgSoft, Vector4.Lerp(Styling.CardBg, accent, 0.24f), hover);
        var border = Vector4.Lerp(Styling.BorderDim, accent, hover);
        Paint.Pill(dl, origin, end, background, border);

        var iconSize = TextDraw.IconSize(icon);
        var labelSize = TextDraw.Measure(label);
        var innerGap = 6f * scale;
        var contentWidth = iconSize.X + innerGap + labelSize.X;
        var startX = origin.X + (size.X - contentWidth) * 0.5f;
        var midY = origin.Y + size.Y * 0.5f;

        TextDraw.Icon(icon, new Vector2(startX, midY - iconSize.Y * 0.5f), Vector4.Lerp(accent, Styling.TextStrong, hover));
        TextDraw.At(label, new Vector2(startX + iconSize.X + innerGap, midY - labelSize.Y * 0.5f), Vector4.Lerp(Styling.TextSecondary, Styling.TextStrong, hover));

        ImGui.SetCursorScreenPos(slotOrigin);
        ImGui.Dummy(size);

        if (!hovered) return;
        ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        Tooltip.Show(LinkHint);
        if (ImGui.IsMouseClicked(ImGuiMouseButton.Left)) UrlActions.Open(url);
        else if (ImGui.IsMouseClicked(ImGuiMouseButton.Right)) ImGui.SetClipboardText(url);
    }

    private static void DrawFooter()
    {
        var scale = ImGuiHelpers.GlobalScale;
        Paint.Divider(4f);

        var twinkle = Styling.Pulse(2600.0);
        var glyphSize = TextDraw.IconSize(FontAwesomeIcon.Code);
        var gap = 6f * scale;
        var labelSize = TextDraw.Measure(MadeBy);
        var origin = ImGui.GetCursorScreenPos();
        var avail = ImGui.GetContentRegionAvail().X;
        var startX = origin.X + MathF.Max(0f, (avail - glyphSize.X - gap - labelSize.X) * 0.5f);

        TextDraw.Icon(FontAwesomeIcon.Code, new Vector2(startX, origin.Y + (labelSize.Y - glyphSize.Y) * 0.5f),
            Vector4.Lerp(Styling.AccentBlue, Styling.Lighten(Styling.AccentBlueSoft, 0.3f), twinkle));
        TextDraw.At(MadeBy, new Vector2(startX + glyphSize.X + gap, origin.Y), Styling.TextDim);
        ImGui.Dummy(new Vector2(avail, labelSize.Y));
    }

    private static void SectionHeader(FontAwesomeIcon icon, string label, Vector4 accent)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var iconSize = TextDraw.IconSize(icon);
        var labelSize = TextDraw.SmallCapsSize(label);

        var iconGap = 8f * scale;
        var sidePad = 12f * scale;
        var contentWidth = iconSize.X + iconGap + labelSize.X;

        var start = ImGui.GetCursorScreenPos();
        var avail = ImGui.GetContentRegionAvail().X;
        var rightX = start.X + avail;
        var contentStartX = start.X + MathF.Max(0f, (avail - contentWidth) * 0.5f);
        var lineY = start.Y + iconSize.Y * 0.5f;

        TextDraw.Icon(icon, new Vector2(contentStartX, start.Y), accent);
        var labelX = contentStartX + iconSize.X + iconGap;
        TextDraw.SmallCaps(label, new Vector2(labelX, start.Y + (iconSize.Y - labelSize.Y) * 0.5f), Styling.TextDim);

        RuleLine(start.X, contentStartX - sidePad, lineY, accent, brightAtStart: false);
        RuleLine(labelX + labelSize.X + sidePad, rightX, lineY, accent, brightAtStart: true);

        ImGui.SetCursorScreenPos(start);
        ImGui.Dummy(new Vector2(avail, iconSize.Y));
    }

    private static void RuleLine(float x0, float x1, float y, Vector4 accent, bool brightAtStart)
    {
        if (x1 - x0 < 1f) return;
        var dl = ImGui.GetWindowDrawList();
        var glowPhase = Styling.Phase(3200.0);
        const int segments = 22;
        for (var segment = 0; segment < segments; segment++)
        {
            var t0 = segment / (float)segments;
            var t1 = (segment + 1) / (float)segments;
            var edge = brightAtStart ? t0 : 1f - t0;
            var fade = 0.5f * (1f - edge);
            var travel = MathF.Max(0f, 1f - MathF.Abs(t0 - glowPhase) * 6f);
            dl.AddLine(new Vector2(x0 + (x1 - x0) * t0, y), new Vector2(x0 + (x1 - x0) * t1, y),
                Paint.Col(Styling.WithAlpha(accent, fade + 0.35f * travel)), 1f);
        }
    }

    private static void ShimmerCentered(string text, Vector4 baseColor, Vector4 shimmerColor, double periodMs, float bandFraction)
    {
        using var font = Fonts.PushTitle();
        var size = TextDraw.Measure(text);
        var avail = ImGui.GetContentRegionAvail().X;
        var origin = ImGui.GetCursorScreenPos();
        var start = new Vector2(origin.X + MathF.Max(0f, (avail - size.X) * 0.5f), origin.Y);

        var bloom = Styling.WithAlpha(Styling.AccentTeal, 0.22f);
        for (var index = 0; index < BloomOffsets.Length; index++)
        {
            TextDraw.At(text, start + BloomOffsets[index] * ImGuiHelpers.GlobalScale, bloom);
        }

        TextDraw.At(text, start, baseColor);

        var dl = ImGui.GetWindowDrawList();
        var bandWidth = size.X * bandFraction;
        var phase = Styling.Phase(periodMs);
        var bandCenter = start.X - bandWidth + phase * (size.X + bandWidth * 2f);
        dl.PushClipRect(new Vector2(bandCenter - bandWidth * 0.5f, start.Y), new Vector2(bandCenter + bandWidth * 0.5f, start.Y + size.Y), true);
        TextDraw.At(text, start, shimmerColor);
        dl.PopClipRect();

        ImGui.Dummy(new Vector2(avail, size.Y));
    }

    private static void CenteredPill(string text, Vector4 textColor, Vector4 borderColor, Vector4 backgroundColor)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var padX = 11f * scale;
        var padY = 3f * scale;
        var textSize = TextDraw.Measure(text);
        var width = textSize.X + padX * 2f;
        var height = textSize.Y + padY * 2f;

        Styling.CenterNextItem(width);
        var origin = ImGui.GetCursorScreenPos();
        var end = origin + new Vector2(width, height);
        Paint.Pill(ImGui.GetWindowDrawList(), origin, end, backgroundColor, borderColor);
        TextDraw.At(text, origin + new Vector2(padX, padY), textColor);
        ImGui.Dummy(new Vector2(width, height));
    }

    private static float Heartbeat(double periodMs)
    {
        var phase = Styling.Phase(periodMs);
        return MathF.Max(Bump(phase, 0.06f, 0.06f), Bump(phase, 0.20f, 0.06f) * 0.6f);
    }

    private static float Bump(float phase, float center, float width)
    {
        var distance = (phase - center) / width;
        if (distance < -1f || distance > 1f) return 0f;
        return 0.5f * (1f + MathF.Cos(distance * MathF.PI));
    }
}
