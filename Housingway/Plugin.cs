using System;
using System.Collections.Generic;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Housingway.Config;
using Housingway.Interface.Windows;
using Housingway.Tweaks;
using Housingway.Utils;

namespace Housingway;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IGameConfig GameConfig { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static IGameInteropProvider GameInteropProvider { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;

    public HousingFunctions HousingFunctions { get; init; } = null!;

    private const string CommandName = "/housingway";

    public Configuration Configuration { get; init; }
    
    public readonly WindowSystem WindowSystem = new("Housingway");
    private ConfigWindow ConfigWindow { get; init; }
    
    public ITweak[] Tweaks { get; init; }
    
    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        
        ConfigWindow = new ConfigWindow(this);

        WindowSystem.AddWindow(ConfigWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Show the Housingway config window."
        });
        
        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;

        HousingFunctions = new HousingFunctions();

        Tweaks = [
            new OverrideInteriorLighting(this),
            new ToggleAmbientOcclusion()
        ];
        
        foreach (var tweak in Tweaks)
        {
            if (Configuration.EnabledTweaks.Contains(tweak.GetType().Name))
            {
                EnableTweak(tweak);
            }
        }
    }

    public void EnableTweak(ITweak tweak)
    {
        if (tweak.Enabled) return;

        try
        {
            tweak.Enable();
            tweak.Enabled = true;
            Configuration.EnabledTweaks.Add(tweak.GetType().Name);
            Configuration.Save();
            Log.Verbose($"Enabled Tweak {tweak.Name}");
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
            ChatGui.PrintError($"Error while enabling {tweak.Name}");
        }
    }

    public void DisableTweak(ITweak tweak)
    {
        if (!tweak.Enabled) return;
        
        try
        {
            tweak.Disable();
            tweak.Enabled = false;
            Configuration.EnabledTweaks.Remove(tweak.GetType().Name);
            Configuration.Save();
            Log.Verbose($"Disabled Tweak {tweak.Name}");
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
            ChatGui.PrintError($"Error while disabling {tweak.Name}");
        }
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        
        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);

        foreach (var tweak in Tweaks)
        {
            tweak.Dispose();
        }
        
        HousingFunctions.Dispose();
    }

    private void OnCommand(string command, string args)
    {
        // In response to the slash command, toggle the display status of our main ui
        ConfigWindow.Toggle();
    }
    
    public void ToggleConfigUi() => ConfigWindow.Toggle();
}
