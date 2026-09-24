using AutoDailyTribes.Core;
using Dalamud.Bindings.ImGui;
using ECommons.DalamudServices;
using System.Diagnostics;

namespace AutoDailyTribes.Windows;

internal static class UrlActions
{
    public static void Open(string url, bool log = true)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            if (log)
                RunLog.Warning(ex, $"failed to launch browser for {url}, copied to clipboard instead");
            ImGui.SetClipboardText(url);
        }
    }
}
