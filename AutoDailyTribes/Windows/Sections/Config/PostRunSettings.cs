using AutoDailyTribes.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoDailyTribes.Windows.Sections.Config;

internal static class PostRunSettings
{
    private const string Group = "Chat commands";
    private const string Label = "Run when the batch finishes";
    private const string Help = "One command per line, each starting with a slash. They fire only when every queued tribe has finished on its own, never after Stop.";
    private const string Caption = "Lines that do not start with a slash are skipped, so nothing is ever said in chat. Try /li home to head home or /ays m to hand off to AutoRetainer.";
    private const int MaxLength = 1000;
    private const int MinLines = 3;
    private const int MaxLines = 8;

    public static void Draw(Configuration cfg)
    {
        using var group = SettingsGroup.Begin(Group);

        SettingsRow.DrawBlock(Label, Help, () => DrawEditor(cfg));
        SettingsRow.Caption(Caption);
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
