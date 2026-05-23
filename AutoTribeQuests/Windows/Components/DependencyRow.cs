using AutoTribeQuests.Core.External;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Diagnostics;
using System.Numerics;

namespace AutoTribeQuests.Windows.Components;

// One row in the deps grid:
//
//   ✓  vnavmesh  (required)                 [   installed   ]
//      Pathfinding and walking to NPCs.
//   ✗  Questionable  (required)             [    Install    ]
//      Plays out each daily quest...
//   ○  Artisan  (optional)                  [    Install    ]
//      Crafter tribes...
//
// Right-clicking the row name copies the repo URL to clipboard, in case the
// one-click install fails and the user needs to paste it manually.
internal static class DependencyRow
{
    public static void Draw(ExternalPlugin plugin)
    {
        var info = ExternalPlugins.Catalog[plugin];
        var installed = ExternalPlugins.IsInstalled(plugin);
        var installing = PluginInstaller.IsInstalling(plugin);

        ImGui.TableNextRow();

        // Status icon
        ImGui.TableSetColumnIndex(0);
        DrawStatusIcon(installed, info.Required);

        // Name + role
        ImGui.TableSetColumnIndex(1);
        DrawName(info, installed);

        // Action
        ImGui.TableSetColumnIndex(2);
        DrawAction(plugin, info, installed, installing);

        // Description row spans columns 1+2
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(1);
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
            ImGui.TextWrapped(info.Purpose);
    }

    private static void DrawStatusIcon(bool installed, bool required)
    {
        var (icon, color) = (installed, required) switch
        {
            (true,  _    ) => (FontAwesomeIcon.CheckCircle, Styling.AccentMint),
            (false, true ) => (FontAwesomeIcon.TimesCircle, Styling.AccentRose),
            (false, false) => (FontAwesomeIcon.Circle,      Styling.TextDim),
        };
        using (ImRaii.PushFont(UiBuilder.IconFont))
        using (ImRaii.PushColor(ImGuiCol.Text, color))
            ImGui.TextUnformatted(icon.ToIconString());
    }

    private static void DrawName(ExternalPluginInfo info, bool installed)
    {
        ImGui.AlignTextToFramePadding();
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextStrong))
            ImGui.TextUnformatted(info.DisplayName);
        ImGui.SameLine();
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextMuted))
            ImGui.TextUnformatted(info.Required ? "  required" : "  optional");

        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            using (ImRaii.Tooltip())
                ImGui.TextUnformatted($"Repo: {info.RepoUrl}\nLeft-click to open repo URL · right-click to copy");
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left)) OpenUrl(info.RepoUrl);
            else if (ImGui.IsMouseClicked(ImGuiMouseButton.Right)) ImGui.SetClipboardText(info.RepoUrl);
        }
    }

    private static void DrawAction(ExternalPlugin plugin, ExternalPluginInfo info, bool installed, bool installing)
    {
        var size = new Vector2(110 * ImGuiHelpers.GlobalScale, 0);
        if (installed)
        {
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.AccentMint))
            {
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted("installed");
            }
            return;
        }

        using (ImRaii.Disabled(installing))
        using (ImRaii.PushColor(ImGuiCol.Button, Styling.AccentTeal * 0.55f))
        using (ImRaii.PushColor(ImGuiCol.ButtonHovered, Styling.AccentTeal * 0.75f))
        using (ImRaii.PushColor(ImGuiCol.ButtonActive, Styling.AccentTeal))
        {
            var label = installing ? "Installing..." : "Install";
            if (ImGui.Button($"{label}##install_{plugin}", size))
                _ = PluginInstaller.Install(plugin);
        }
    }

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch
        {
            ImGui.SetClipboardText(url);
        }
    }
}
