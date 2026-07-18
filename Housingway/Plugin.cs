using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Housingway.Config;
using Housingway.Interface.Windows;
using Housingway.Utils;
using Pictomancy;

namespace Housingway;

public sealed class Plugin : IDalamudPlugin
{
    internal Scene Scene { get; init; }
    internal HousingService HousingService { get; init; }
    
    public static Configuration Configuration { get; set; } = null!;
    public static TweakManager TweakManager { get; set; } = null!;
    
    private const string CommandName = "/housingway";

    public readonly WindowSystem WindowSystem = new("Housingway");
    private ConfigWindow ConfigWindow { get; init; }
    internal static Overlay Overlay { get; private set; } = null!;

    public readonly PctContext PctContext;
    
    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Service>();
        
        Configuration = Service.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        PctContext = PctService.Initialize(Service.PluginInterface, new PctOptions()
        {
            EnableDxRenderer = true,
            EnableKtkOutput = false,
            EnableVfxRenderer = false,
            MaxTriangleVertices = 100000
        });
        
        Scene = new Scene();
        Overlay = new Overlay();
        
        WindowSystem.AddWindow(Overlay);

        TweakManager = new TweakManager();
        HousingService = new HousingService();
        Overlay.IsOpen = HousingService.InHousingArea;
        
        ConfigWindow = new ConfigWindow();
        WindowSystem.AddWindow(ConfigWindow);
        
        Service.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Show the Housingway config window."
        });
        
        Service.PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        Service.PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
    }

    public void Dispose()
    {
        Service.PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        Service.PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        
        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        Overlay.Dispose();

        Service.CommandManager.RemoveHandler(CommandName);

        TweakManager.Dispose();
        
        HousingService.Dispose();
        Scene.Dispose();
        
        PctContext.Dispose();
    }

    private void OnCommand(string command, string args) => ConfigWindow.Toggle();

    public void ToggleConfigUi() => ConfigWindow.Toggle();
}
