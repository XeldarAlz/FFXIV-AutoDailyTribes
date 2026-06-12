using AutoDailyTribes.Core.Tasks;
using AutoDailyTribes.Windows.Components;
using AutoDailyTribes.Windows.Sections;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using System.Numerics;

namespace AutoDailyTribes.Windows;

public sealed class MainWindow : Window, IDisposable
{
    private readonly Plugin plugin;

    public MainWindow(Plugin plugin) : base("Auto Daily Tribes###AutoDailyTribesMain")
    {
        this.plugin = plugin;
        Size = new Vector2(720, 560);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public void Dispose() { }

    public override void Draw()
    {
        var cfg = plugin.Configuration;
        var ctrl = plugin.Controller;

        using var style = Styling.PushWindowStyle();

        // While a run is active the hero card is the headline — keep only the toolbar icons.
        if (ctrl.Running) HeaderStrip.DrawIconsInline(plugin);
        else HeaderStrip.Draw(plugin);
        DependencyBanner.Draw(plugin);

        if (ctrl.Running) RunningPanel.Draw(ctrl);
        else              SetupPanel.Draw(ctrl, cfg);
    }
}
