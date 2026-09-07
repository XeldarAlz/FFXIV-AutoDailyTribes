using AutoDailyTribes.Core;
using AutoDailyTribes.Core.Debug;
using AutoDailyTribes.Core.External;
using AutoDailyTribes.Core.Localization;
using AutoDailyTribes.Core.Tasks;
using AutoDailyTribes.Windows;
using AutoDailyTribes.Windows.Shell;
using clib;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ECommons;
using ECommons.DalamudServices;
using System.Globalization;
using System.IO;

namespace AutoDailyTribes;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    internal Configuration Configuration { get; }
    internal static Configuration Cfg { get; private set; } = null!;
    internal static Plugin Instance { get; private set; } = null!;
    internal WindowSystem WindowSystem { get; } = new("AutoDailyTribes");
    internal AutoTribeController Controller { get; }

    private readonly AppWindow appWindow;
    private readonly CommandInfo primaryCommand;
    private readonly CommandInfo aliasCommand;

    public Plugin()
    {
        ECommonsMain.Init(PluginInterface, this);
        CLibMain.Init(PluginInterface, this, CLibModule.Automation);

        Instance = this;
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Cfg = Configuration;
        Controller = new AutoTribeController();

        InitializeLocalization();
        Fonts.Initialize(PluginInterface.UiBuilder, PluginDirectory);
        appWindow = new AppWindow(this);
        WindowSystem.AddWindow(appWindow);

        primaryCommand = new CommandInfo(OnCommand) { HelpMessage = Loc.T(L.Plugin.CommandHelp) };
        aliasCommand = new CommandInfo(OnCommand) { HelpMessage = Loc.T(L.Plugin.CommandHelpAlias) };
        CommandManager.AddHandler(AdtConstants.PrimaryCommand, primaryCommand);
        CommandManager.AddHandler(AdtConstants.AliasCommand, aliasCommand);

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
        appWindow.Dispose();
        Fonts.Dispose();

        CommandManager.RemoveHandler(AdtConstants.PrimaryCommand);
        CommandManager.RemoveHandler(AdtConstants.AliasCommand);

        QuestionableSettings.RestoreQuestCompletion();

        CLibMain.Dispose();
        ECommonsMain.Dispose();
    }

    // The command list keeps the help text Dalamud captured at registration, so a language switch
    // has to rewrite it in place.
    public void OnLanguageChanged()
    {
        primaryCommand.HelpMessage = Loc.T(L.Plugin.CommandHelp);
        aliasCommand.HelpMessage = Loc.T(L.Plugin.CommandHelpAlias);
    }

    private static string PluginDirectory => PluginInterface.AssemblyLocation.DirectoryName ?? string.Empty;

    private static void InitializeLocalization()
    {
        var directory = Path.Combine(PluginDirectory, "Localization");
        if (string.IsNullOrEmpty(Cfg.Language))
        {
            Cfg.Language = DetectLanguage();
            Cfg.Save();
        }

        Loc.Initialize(Cfg.Language, directory);
    }

    private static string DetectLanguage()
    {
        var dalamudLanguage = PluginInterface.UiLanguage;
        if (Languages.IsKnown(dalamudLanguage)) return Languages.Resolve(dalamudLanguage).Code;

        switch (Svc.ClientState.ClientLanguage)
        {
            case Dalamud.Game.ClientLanguage.German: return Languages.German.Code;
            case Dalamud.Game.ClientLanguage.French: return Languages.French.Code;
            case Dalamud.Game.ClientLanguage.Japanese: return Languages.Japanese.Code;
        }

        var osLanguage = CultureInfo.InstalledUICulture.TwoLetterISOLanguageName;
        return Languages.IsKnown(osLanguage) ? Languages.Resolve(osLanguage).Code : Languages.English.Code;
    }

    private void OnCommand(string command, string args)
    {
        var trimmed = args.Trim();
        if (trimmed.Equals("config", StringComparison.OrdinalIgnoreCase))
            ToggleConfigUi();
        else if (trimmed.Equals("about", StringComparison.OrdinalIgnoreCase))
            ToggleAboutUi();
        else if (trimmed.Equals("deps", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("dependencies", StringComparison.OrdinalIgnoreCase))
            ToggleDependenciesUi();
        else if (trimmed.Equals("log", StringComparison.OrdinalIgnoreCase))
            ToggleLogUi();
        else if (trimmed.Equals("target", StringComparison.OrdinalIgnoreCase))
            TargetDumper.Dump();
        else
            ToggleMainUi();
    }

    public void ToggleMainUi() => appWindow.Toggle();
    public void ToggleConfigUi() => appWindow.TogglePage(AppWindow.Page.Settings);
    public void ToggleAboutUi() => appWindow.TogglePage(AppWindow.Page.About);
    public void ToggleDependenciesUi() => appWindow.TogglePage(AppWindow.Page.Plugins);
    public void ToggleLogUi() => appWindow.TogglePage(AppWindow.Page.Log);
}
