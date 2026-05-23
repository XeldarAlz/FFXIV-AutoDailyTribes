using AutoTribeQuests.Core.Tasks;
using AutoTribeQuests.Windows.Sections;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using System.Numerics;

namespace AutoTribeQuests.Windows;

// MainWindow is intentionally a thin shell. To add a new piece of UI:
//   1. Write Windows/Sections/MyThing.cs with a public static Draw(...) method.
//   2. Add `MyThing.Draw(...);` between two existing calls below.
// Components live one level deeper in Windows/Components/ for reuse across
// sections. Both folders are append-only — never edit an existing section's
// signature, write a new one instead.
public sealed class MainWindow : Window, IDisposable
{
    private readonly Plugin plugin;

    public MainWindow(Plugin plugin) : base("Allied Tribes###AutoTribeQuestsMain")
    {
        this.plugin = plugin;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(520, 360),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
        Size = new Vector2(720, 520);
        SizeCondition = ImGuiCond.FirstUseEver;
        Flags = ImGuiWindowFlags.NoCollapse;
    }

    public void Dispose() { }

    public override void Draw()
    {
        var cfg = plugin.Configuration;
        var ctrl = plugin.Controller;

        using var style = Styling.PushWindowStyle();

        TopToolbar.Draw(plugin, ctrl);
        DependencyBanner.Draw(plugin);
        Header.Draw(ctrl, cfg);
        TribeList.Draw(ctrl, cfg);
        Footer.Draw();
    }
}
