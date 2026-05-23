using AutoTribeQuests.Core;
using AutoTribeQuests.Core.Tasks;
using AutoTribeQuests.Windows;
using clib;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ECommons;

namespace AutoTribeQuests;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    internal Configuration Configuration { get; }
    internal WindowSystem WindowSystem { get; } = new("AutoTribeQuests");
    internal AutoTribeController Controller { get; }

    private readonly MainWindow mainWindow;
    private readonly ConfigWindow configWindow;
    private readonly AboutWindow aboutWindow;

    public Plugin()
    {
        ECommonsMain.Init(PluginInterface, this);
        CLibMain.Init(PluginInterface, this, CLibModule.Automation);

        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Controller = new AutoTribeController();

        mainWindow = new MainWindow(this);
        configWindow = new ConfigWindow(this);
        aboutWindow = new AboutWindow();

        WindowSystem.AddWindow(mainWindow);
        WindowSystem.AddWindow(configWindow);
        WindowSystem.AddWindow(aboutWindow);

        CommandManager.AddHandler(Constants.PrimaryCommand, new CommandInfo(OnCommand)
        {
            HelpMessage = "Toggle the Allied Tribes window. /atq config opens settings, /atq about opens credits."
        });
        CommandManager.AddHandler(Constants.AliasCommand, new CommandInfo(OnCommand)
        {
            HelpMessage = "Alias for /atq."
        });

        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;

        WindowSystem.RemoveAllWindows();
        mainWindow.Dispose();
        configWindow.Dispose();
        aboutWindow.Dispose();

        CommandManager.RemoveHandler(Constants.PrimaryCommand);
        CommandManager.RemoveHandler(Constants.AliasCommand);

        CLibMain.Dispose();
        ECommonsMain.Dispose();
    }

    private void OnCommand(string command, string args)
    {
        var trimmed = args.Trim();
        if (trimmed.Equals("config", StringComparison.OrdinalIgnoreCase))
            ToggleConfigUi();
        else if (trimmed.Equals("about", StringComparison.OrdinalIgnoreCase))
            ToggleAboutUi();
        else
            ToggleMainUi();
    }

    public void ToggleMainUi() => mainWindow.Toggle();
    public void ToggleConfigUi() => configWindow.Toggle();
    public void ToggleAboutUi() => aboutWindow.Toggle();
}
