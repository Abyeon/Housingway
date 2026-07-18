using FFXIVClientStructs.FFXIV.Client.Graphics.Environment;
using Housingway.Structs.Env;
using Housingway.Utils;

namespace Housingway.Tweaks.OverrideSkybox;

public partial class OverrideSkybox : ConfigurableTweak<OverrideSkyboxConfig>
{
    public override string Name { get; init; } = "Skybox";
    public override string Description { get; init; } = "Overrides the interior skybox.";

    private EnvService? envService;

    public OverrideSkybox()
    {
        Config = Plugin.Configuration.Tweaks.OverrideSkybox;

        texSky = new SetTextureSelect(Service.TextureProvider);
        texCloudTop = new SetTextureSelect(Service.TextureProvider);
        texCloudSide = new SetTextureSelect(Service.TextureProvider);
    }

    private void OnZoneLoaded() => UpdateEnvironment();

    private unsafe void UpdateEnvironment()
    {
        if (HousingService.IsInside)
        {
            var env = EnvManagerEx.Instance();
            if (env is null) return;
        
            envService!.Override = Config.Override;
            env->EnvState = Config.State;
        }
        else
        {
            envService!.Override = EnvOverride.None;
        }
        
        Service.Log.Debug("should do thing");
    }

    public override unsafe void Enable()
    {
        envService = new EnvService();
        envService.Override = Config.Override;
        
        Scene.OnZoneLoaded += OnZoneLoaded;
        
        var env = EnvManagerEx.Instance();
        if (env is not null && HousingService.IsInside)
        {
            env->EnvState = Config.State;
        }
        
        UpdateEnvironment();
    }

    public override void Disable()
    {
        Scene.OnZoneLoaded -= OnZoneLoaded;
        
        envService?.Dispose();
        envService = null;
    }

    public override void Dispose() { }
}
