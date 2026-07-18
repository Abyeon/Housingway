using Dalamud.Bindings.ImGui;
using Housingway.Interface;

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
        if (Ui.SliderWithDefault("Light", ref light, 0, 1, InitialValue))
        {
            Config.Light = light;
            UpdateLight();
        }

        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            Plugin.Configuration.Save();
        }

        ImGui.Spacing();
        if (ImGui.Button("Restore"))
        {
            IndoorLight = InitialValue;
            Config.Light = IndoorLight;
            Plugin.Configuration.Save();
        }
    }
}
