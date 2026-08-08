using System.Threading.Tasks;
using Housingway.Structs.Env;
using Housingway.Tweaks.Base;
using Housingway.Utils;

namespace Housingway.Tweaks.OverrideSkybox;

public partial class OverrideSkybox : ConfigurableTweak<OverrideSkyboxConfig>
{
    public override string Name { get; init; } = "Skybox";
    public override string Author { get; init; } = "Abyeon";
    public override string Description { get; init; } = "Overrides the interior skybox.";

    private EnvService? envService;

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
    }

    public override async Task Enable()
    {
        await Service.Framework.Run(() =>
        {
            envService = new EnvService();
            envService.Override = Config.Override;

            Scene.OnZoneLoaded += OnZoneLoaded;

            unsafe
            {
                var env = EnvManagerEx.Instance();
                if (env is not null && HousingService.IsInside)
                {
                    env->EnvState = Config.State;
                }
            }

            UpdateEnvironment();
        });
    }

    public override async Task Disable()
    {
        Scene.OnZoneLoaded -= OnZoneLoaded;

        await Service.Framework.Run(() =>
        {
            envService?.Dispose();
            envService = null;
        });
    }
}
