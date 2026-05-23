using AutoDailyTribes.Core.External;
using AutoDailyTribes.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using System.Numerics;

namespace AutoDailyTribes.Windows;

public sealed class DependenciesWindow : Window, IDisposable
{
    public DependenciesWindow() : base("Auto Daily Tribes — Dependencies###AutoDailyTribesDeps")
    {
        Flags = ImGuiWindowFlags.NoCollapse;
        Size = new Vector2(560, 420);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(420, 280),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
    }

    public void Dispose() { }

    public override void Draw()
    {
        using var style = Styling.PushWindowStyle();

        DrawHeader();
        ImGui.Separator();
        ImGui.Spacing();
        DrawTable();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        DrawFooter();
    }

    private static void DrawHeader()
    {
        ImGui.SetWindowFontScale(1.18f);
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextStrong))
            ImGui.TextUnformatted("Required & optional plugins");
        ImGui.SetWindowFontScale(1.0f);

        var missing = ExternalPlugins.All.Count(p => ExternalPlugins.Catalog[p].Required && !ExternalPlugins.IsInstalled(p));
        using (ImRaii.PushColor(ImGuiCol.Text, missing == 0 ? Styling.AccentMint : Styling.AccentRose))
            ImGui.TextUnformatted(missing == 0
                ? "All required plugins are installed and loaded."
                : $"{missing} required plugin{(missing == 1 ? " is" : "s are")} missing.");
    }

    private static void DrawTable()
    {
        if (!ImGui.BeginTable("##deps", 3,
                ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.PadOuterX | ImGuiTableFlags.RowBg))
            return;

        ImGui.TableSetupColumn("##status", ImGuiTableColumnFlags.WidthFixed, 32f * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("##name", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("##action", ImGuiTableColumnFlags.WidthFixed, 130f * ImGuiHelpers.GlobalScale);

        foreach (var plugin in ExternalPlugins.All)
            DependencyRow.Draw(plugin);

        ImGui.EndTable();
    }

    private static void DrawFooter()
    {
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextMuted))
            ImGui.TextWrapped(
                "Install adds the plugin's source repository to Dalamud and queues an install. " +
                "If one-click install fails (URL drift, network), right-click a plugin name to " +
                "copy its repo URL and add it manually via /xlsettings → Experimental → Custom " +
                "Plugin Repositories.");
    }
}
