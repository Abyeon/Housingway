using System.Threading;
using System.Threading.Tasks;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Housingway.Config;
using Housingway.Interface.Windows;
using Housingway.Profiles;
using Housingway.Utils;
using Pictomancy;

namespace Housingway;

public sealed class Plugin : IAsyncDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; set; } = null!;

    internal static Scene Scene { get; set; } = null!;
    internal static HousingService HousingService { get; set; } = null!;

    public static Configuration Configuration
    {
        get
        {
            if (ProfileManager.Profile is { } profile)
            {
                return profile.Config;
            }

            return field;
        } set;
    } = null!;

    public static TweakManager TweakManager { get; set; } = null!;
    public static ProfileManager ProfileManager { get; set; } = null!;

    private const string CommandName = "/housingway";

    public readonly WindowSystem WindowSystem = new("Housingway");
    internal static ConfigWindow ConfigWindow { get; set; } = null!;
    internal static ProfileWindow ProfileWindow { get; set; } = null!;
    internal static Overlay Overlay { get; private set; } = null!;

    public PctContext PctContext { get; private set; } = null!;
    
    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        PluginInterface.Create<Service>();
        
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        
        ProfileManager = new ProfileManager();

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
        
        TweakManager = new TweakManager();
        HousingService = new HousingService();
        
        Overlay.IsOpen = HousingService.InHousingArea;
        
        ConfigWindow = new ConfigWindow();
        ProfileWindow = new ProfileWindow();
        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(ProfileWindow);
        
        await ProfileManager.LoadAsync();
        
        Service.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Show the Housingway config window."
        });
        
        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
    }

    private static void OnCommand(string command, string args) => ConfigWindow.Toggle();

    public static void ToggleConfigUi() => ConfigWindow.Toggle();

    public async ValueTask DisposeAsync()
    {
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        
        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        Overlay.Dispose();

        Service.CommandManager.RemoveHandler(CommandName);
        
        await ProfileManager.DisposeAsync();

        TweakManager.Dispose();
        
        HousingService.Dispose();
        Scene.Dispose();
        
        PctContext.Dispose();
    }
}
