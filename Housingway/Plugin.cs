using System;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Housingway.Config;
using Housingway.Interface.Windows;
using Housingway.Tweaks;
using Housingway.Tweaks.OverrideSkybox;
using Housingway.Utils;
using Pictomancy;

namespace Housingway;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IGameConfig GameConfig { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static IGameInteropProvider GameInteropProvider { get; private set; } = null!;
    [PluginService] internal static ISigScanner SigScanner { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    
    internal Scene Scene { get; init; }
    internal HousingService HousingService { get; init; }

    private const string CommandName = "/housingway";

    public static Configuration Configuration { get; set; } = null!;
    public static TweakManager TweakManager { get; set; } = null!;

    public readonly WindowSystem WindowSystem = new("Housingway");
    private ConfigWindow ConfigWindow { get; init; }
    internal static Overlay Overlay { get; private set; } = null!;

    public readonly PctContext PctContext;
    
    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        PctContext = PctService.Initialize(PluginInterface, new PctOptions()
        {
            EnableDxRenderer = true,
            EnableKtkOutput = false,
            EnableVfxRenderer = false,
            MaxTriangleVertices = 100000
        });
        
        Scene = new Scene();
        Overlay = new Overlay();
        
        WindowSystem.AddWindow(Overlay);
        //Overlay.Toggle();

        TweakManager = new TweakManager();
        TweakManager.LoadTweaks();

        HousingService = new HousingService();
        Overlay.IsOpen = HousingService.InHousingArea;
        
        ConfigWindow = new ConfigWindow();
        WindowSystem.AddWindow(ConfigWindow);
        
        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Show the Housingway config window."
        });
        
        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        
        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        Overlay.Dispose();

        CommandManager.RemoveHandler(CommandName);

        TweakManager.Dispose();
        
        HousingService.Dispose();
        Scene.Dispose();
        
        PctContext.Dispose();
    }

    private void OnCommand(string command, string args)
    {
        // In response to the slash command, toggle the display status of our main ui
        ConfigWindow.Toggle();
    }
    
    public void ToggleConfigUi() => ConfigWindow.Toggle();
}
