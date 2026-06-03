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
                Svc.Log.Warning(ex, "[AutoDailyTribes] failed to launch browser for {0}, copied to clipboard instead", url);
            ImGui.SetClipboardText(url);
        }
    }
}
