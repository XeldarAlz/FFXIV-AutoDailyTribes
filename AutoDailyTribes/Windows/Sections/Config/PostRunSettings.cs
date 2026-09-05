using AutoDailyTribes.Core.Localization;
using AutoDailyTribes.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoDailyTribes.Windows.Sections.Config;

internal static class PostRunSettings
{
    private const int MaxLength = 1000;
    private const int MinLines = 3;
    private const int MaxLines = 8;

    public static void Draw(Configuration cfg)
    {
        using var group = SettingsGroup.Begin(Loc.T(L.Settings.PostRunGroup));

        SettingsRow.DrawBlock(Loc.T(L.Settings.PostRunLabel), Loc.T(L.Settings.PostRunHelp), () => DrawEditor(cfg));
        SettingsRow.Caption(Loc.T(L.Settings.PostRunCaption));
    }

    private static void DrawEditor(Configuration cfg)
    {
        var commands = cfg.PostRunCommands;
        var lineCount = 1;
        for (var charIndex = 0; charIndex < commands.Length; charIndex++)
        {
            if (commands[charIndex] == '\n') lineCount++;
        }

        var scale = ImGuiHelpers.GlobalScale;
        var height = ImGui.GetTextLineHeight() * Math.Clamp(lineCount + 1, MinLines, MaxLines) + ImGui.GetStyle().FramePadding.Y * 2f;
        var width = SettingsGroup.ContentRightEdge - ImGui.GetCursorScreenPos().X;

        using var colors = SettingsControls.PushFrameColors();
        using var style = ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 1f)
            .Push(ImGuiStyleVar.FrameRounding, Styling.FrameRounding * scale);
        if (!ImGui.InputTextMultiline("##adt_post_run", ref commands, MaxLength, new Vector2(width, height))) return;

        cfg.PostRunCommands = commands;
        cfg.SaveDebounced();
    }
}
