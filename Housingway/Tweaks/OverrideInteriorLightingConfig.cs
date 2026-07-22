using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Housingway.Interface;
using Housingway.Utils;

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
        
        using var _ = ImRaii.Disabled(!HousingService.IsInside);

        ImGui.Spacing();
        if (ImGui.Button("Restore"))
        {
            IndoorLight = InitialValue;
            Config.Light = IndoorLight;
            Plugin.Configuration.Save();
        }
    }
}
