using System.Diagnostics;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using Pictomancy;

namespace Housingway.Tweaks;

public class ModelAdjustmentsConfig
{
    public bool DisableLightguard = true;
    public bool DisableShameCube = true;
    public bool ShowBuildLimit = true;
}

public unsafe partial class ModelAdjustments
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

        var limit = Config.ShowBuildLimit;
        if (ImGui.Checkbox("Show Build Limit When Camera Near", ref limit))
        {
            Config.ShowBuildLimit = limit;
            PluginConfig.Save();
        }
        
        Debug();
    }

    [Conditional("DEBUG")]
    private void Debug()
    {
        var guard = lightguard is null ? "Null" : lightguard->ModelResourceHandle->FileName.ToString();
        var cube = shameCube is null ? "Null" : shameCube->ModelResourceHandle->FileName.ToString();
        
        ImGui.InputText("Lightguard", ref guard, flags: ImGuiInputTextFlags.ReadOnly);
        ImGui.InputText("ShameCube", ref cube, flags: ImGuiInputTextFlags.ReadOnly);
    }
}
