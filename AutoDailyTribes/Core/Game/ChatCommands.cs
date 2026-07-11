using ECommons.Automation;
using ECommons.DalamudServices;

namespace AutoDailyTribes.Core.Game;

internal static class ChatCommands
{
    public static void Dispatch(string commandBlock)
    {
        if (string.IsNullOrWhiteSpace(commandBlock)) return;

        var lines = commandBlock.Split('\n');
        var delayMs = 0;
        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var command = lines[lineIndex].Trim();
            if (command.Length == 0) continue;
            if (!command.StartsWith('/'))
            {
                Svc.Log.Warning($"[ADT] Post-run line skipped, commands must start with '/': {command}");
                continue;
            }

            var scheduled = command;
            Svc.Framework.RunOnTick(() => Send(scheduled), TimeSpan.FromMilliseconds(delayMs));
            delayMs += AdtConstants.PostRunCommandSpacingMs;
        }
    }

    private static void Send(string command)
    {
        try
        {
            Svc.Log.Info($"[ADT] Post-run command: {command}");
            Chat.ExecuteCommand(command);
        }
        catch (Exception exception)
        {
            Svc.Log.Error($"[ADT] Post-run command '{command}' failed: {exception.Message}");
        }
    }
}
