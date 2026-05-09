using Dalamud.Bindings.ImGui;

namespace Housingway.Tweaks;

public class ModelAdjustmentsConfig
{
    public bool DisableLightguard = true;
    public bool DisableShameCube = true;
}

public partial class ModelAdjustments
{
    public override void DrawConfig()
    {
        var guard = Config.DisableLightguard;
        if (ImGui.Checkbox("Disable Lightguard", ref guard))
        {
            Config.DisableLightguard = guard;
            ToggleModels();
            PluginConfig.Save();
        }
        
        var cube = Config.DisableShameCube;
        if (ImGui.Checkbox("Disable ShameCube", ref cube))
        {
            Config.DisableShameCube = cube;
            ToggleModels();
            PluginConfig.Save();
        }
    }
}
