using Dalamud.Bindings.ImGui;
using Dalamud.Configuration;

namespace Housingway.Tweaks;

public class OverrideInteriorLightingConfig
{
    public float Light = 1f;
}

public partial class OverrideInteriorLighting
{
    public override void DrawConfig()
    {
        var light = Config.Light;
        if (ImGui.SliderFloat("Light", ref light, 0, 1))
        {
            Config.Light = light;
            UpdateLight();
        }

        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            PluginConfig.Save();
        }
    }
}
