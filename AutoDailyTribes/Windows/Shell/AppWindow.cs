using AutoDailyTribes.Windows.Pages;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using System.Numerics;

namespace AutoDailyTribes.Windows.Shell;

public sealed class AppWindow : Window, IDisposable
{
    public enum Page { Tribes, Settings, Plugins, Log, About }

    private const float PageRevealMs = 260f;
    private const float PageSlide = 12f;
    private const float CollapseMs = 280f;
    private const float GripSize = 12f;
    private const float GripInset = 4f;
    private const float MinimumBodyHeight = 260f;
    private const ImGuiWindowFlags BaseFlags =
        ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoCollapse;

    private static readonly Vector2 DefaultSize = new(960, 720);
    private static readonly Vector2 Unbounded = new(float.MaxValue, float.MaxValue);
    private static readonly WindowSizeConstraints CompactConstraints = new()
    {
        MinimumSize = new Vector2(HeaderBar.MinimumWidth, Layout.HeaderHeight),
        MaximumSize = Unbounded,
    };
    private static readonly WindowSizeConstraints ExpandedConstraints = new()
    {
        MinimumSize = new Vector2(HeaderBar.MinimumWidth, Layout.HeaderHeight + Layout.DockHeight + MinimumBodyHeight),
        MaximumSize = Unbounded,
    };

    private readonly Plugin plugin;
    private readonly TribesPage tribesPage = new();
    private readonly SettingsPage settingsPage = new();
    private readonly PluginsPage pluginsPage = new();
    private readonly LogPage logPage = new();
    private readonly AboutPage aboutPage = new();

    private Page page = Page.Tribes;
    private long pageShownTick = Environment.TickCount64;
    private bool resetScroll;

    private bool compact;
    private float collapse;
    private float collapseFrom;
    private long collapseTick;
    private bool sizeDriven;
    private Vector2 expandedSize = DefaultSize;
    private IDisposable? chrome;
    private IDisposable? bodyFont;

    public AppWindow(Plugin plugin) : base("Auto Daily Tribes###AutoDailyTribesMain", BaseFlags)
    {
        this.plugin = plugin;
        Size = DefaultSize;
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = ExpandedConstraints;
        AllowPinning = false;
        AllowClickthrough = false;
    }

    public Page Current => page;

    public bool Compact => compact;

    public void Dispose() { }

    public void Show(Page target)
    {
        if (page != target)
        {
            page = target;
            pageShownTick = Environment.TickCount64;
            resetScroll = true;
        }

        if (compact) ToggleCompact();
        IsOpen = true;
    }

    public void TogglePage(Page target)
    {
        if (IsOpen && page == target && !compact) IsOpen = false;
        else Show(target);
    }

    public void ToggleCompact()
    {
        compact = !compact;
        collapseFrom = collapse;
        collapseTick = Environment.TickCount64;
    }

    public override void OnOpen() => pageShownTick = Environment.TickCount64;

    public override void PreDraw()
    {
        bodyFont = Fonts.PushBody();
        chrome = Styling.PushChrome(Vector2.Zero);
        AdvanceCollapse();

        var wasSizeDriven = sizeDriven;
        sizeDriven = compact || collapse > 0f;
        Flags = sizeDriven ? BaseFlags | ImGuiWindowFlags.NoResize : BaseFlags;
        SizeConstraints = sizeDriven ? CompactConstraints : ExpandedConstraints;

        if (sizeDriven)
        {
            var height = expandedSize.Y + (Layout.HeaderHeight - expandedSize.Y) * collapse;
            Size = new Vector2(expandedSize.X, height);
            SizeCondition = ImGuiCond.Always;
            return;
        }

        if (wasSizeDriven)
        {
            Size = expandedSize;
            SizeCondition = ImGuiCond.Always;
            return;
        }

        SizeCondition = ImGuiCond.FirstUseEver;
    }

    private void AdvanceCollapse()
    {
        var target = compact ? 1f : 0f;
        if (Motion.Reduced)
        {
            collapse = target;
            return;
        }

        var span = MathF.Abs(target - collapseFrom);
        if (span <= 0.0001f)
        {
            collapse = target;
            return;
        }

        var elapsed = (Environment.TickCount64 - collapseTick) / (CollapseMs * span);
        var t = Math.Clamp(elapsed, 0f, 1f);
        collapse = collapseFrom + (target - collapseFrom) * Motion.EaseInOutCubic(t);
    }

    public override void PostDraw()
    {
        chrome?.Dispose();
        chrome = null;
        bodyFont?.Dispose();
        bodyFont = null;
    }

    public override void Draw()
    {
        var scale = ImGuiHelpers.GlobalScale;
        var headerHeight = Layout.HeaderHeight * scale;
        var windowSize = ImGui.GetWindowSize();
        if (HeaderBar.HandleDrag(ImGui.GetWindowPos(), windowSize.X, headerHeight)) ToggleCompact();

        var windowPos = ImGui.GetWindowPos();
        var windowRounding = Styling.WindowRounding * scale;
        var dl = ImGui.GetWindowDrawList();

        if (collapse >= 0.999f)
        {
            HeaderBar.Draw(this, plugin, windowPos, windowSize.X, headerHeight, windowRounding, compact: true);
            return;
        }

        if (!sizeDriven) expandedSize = windowSize / scale;

        Ambient.Draw(dl, windowPos, windowPos + windowSize);
        HeaderBar.Draw(this, plugin, windowPos, windowSize.X, headerHeight, windowRounding, compact: false);

        using var bodyAlpha = ImRaii.PushStyle(ImGuiStyleVar.Alpha, MathF.Max(0.001f, 1f - collapse));
        DrawBody(windowPos, windowSize, headerHeight, windowRounding);
        if (!sizeDriven) DrawResizeGrip(dl, windowPos + windowSize);
    }

    private static void DrawResizeGrip(ImDrawListPtr dl, Vector2 corner)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var grip = GripSize * scale;
        var inset = GripInset * scale;
        var anchor = corner - new Vector2(inset, inset);
        var hovered = ImGui.IsMouseHoveringRect(corner - new Vector2(grip + inset, grip + inset), corner);
        var color = Paint.Col(Styling.WithAlpha(Styling.TextStrong, hovered ? 0.38f : 0.14f));
        for (var lineIndex = 1; lineIndex <= 3; lineIndex++)
        {
            var offset = lineIndex * grip / 3f;
            dl.AddLine(anchor - new Vector2(offset, 0f), anchor - new Vector2(0f, offset), color, 1.4f * scale);
        }
    }

    private void DrawBody(Vector2 windowPos, Vector2 windowSize, float headerHeight, float windowRounding)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var dl = ImGui.GetWindowDrawList();
        var running = plugin.Controller.Running;
        var dockHeight = page == Page.Tribes ? Layout.DockHeight * scale
            : running ? Layout.MiniPlayerHeight * scale
            : 0f;
        var railWidth = Layout.RailWidth * scale;
        var bodyTop = windowPos.Y + headerHeight;
        var bodyHeight = windowSize.Y - headerHeight - dockHeight;
        if (bodyHeight < 1f) return;

        ImGui.SetCursorScreenPos(new Vector2(windowPos.X, bodyTop));
        using (var rail = ImRaii.Child("##adt_rail", new Vector2(railWidth, bodyHeight), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
        {
            if (rail.Success && NavRail.Draw(page, plugin) is { } target) Show(target);
        }

        Paint.Hairline(dl, new Vector2(windowPos.X + railWidth, bodyTop + 10f * scale), new Vector2(windowPos.X + railWidth, bodyTop + bodyHeight - 10f * scale));

        ImGui.SetCursorScreenPos(new Vector2(windowPos.X + railWidth, bodyTop));
        var padding = Layout.ContentPadding * scale;
        var rightInset = Layout.ContentRightInset * scale;
        using (ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(padding, padding * 0.8f)))
        using (var content = ImRaii.Child("##adt_page", new Vector2(windowSize.X - railWidth - rightInset, bodyHeight), false, ImGuiWindowFlags.AlwaysUseWindowPadding))
        {
            if (content) DrawPage();
        }

        if (dockHeight <= 0f) return;
        ImGui.SetCursorScreenPos(new Vector2(windowPos.X, windowPos.Y + windowSize.Y - dockHeight));
        var dockSize = new Vector2(windowSize.X, dockHeight);
        if (page == Page.Tribes) ActionDock.Draw(plugin, dockSize, windowRounding);
        else if (MiniPlayer.Draw(plugin, dockSize, windowRounding)) Show(Page.Tribes);
    }

    private void DrawPage()
    {
        if (resetScroll)
        {
            ImGui.SetScrollY(0f);
            resetScroll = false;
        }

        using var reveal = Motion.PushReveal(Motion.Reveal(pageShownTick, PageRevealMs), PageSlide);
        switch (page)
        {
            case Page.Tribes: tribesPage.Draw(plugin, this); break;
            case Page.Settings: settingsPage.Draw(plugin); break;
            case Page.Plugins: pluginsPage.Draw(); break;
            case Page.Log: logPage.Draw(); break;
            case Page.About: aboutPage.Draw(pageShownTick); break;
        }
    }
}
