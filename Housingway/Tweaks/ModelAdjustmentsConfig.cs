using System.Diagnostics;
using Dalamud.Bindings.ImGui;

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
        bool guard = Config.DisableLightguard;
        if (ImGui.Checkbox("Disable Lightguard", ref guard))
        {
            Config.DisableLightguard = guard;
            ToggleModels();
            Plugin.Configuration.Save();
        }
        
        bool cube = Config.DisableShameCube;
        if (ImGui.Checkbox("Disable ShameCube", ref cube))
        {
            Config.DisableShameCube = cube;
            ToggleModels();
            Plugin.Configuration.Save();
        }

        bool limit = Config.ShowBuildLimit;
        if (ImGui.Checkbox("Show Build Limit When Camera Near", ref limit))
        {
            Config.ShowBuildLimit = limit;
            Plugin.Configuration.Save();
        }
        
        Debug();
    }

    [Conditional("DEBUG")]
    private void Debug()
    {
        string guard = lightguard.IsNull ? "Null" : lightguard.Value->ModelResourceHandle->FileName.ToString();
        string cube = shameCube.IsNull ? "Null" : shameCube.Value->ModelResourceHandle->FileName.ToString();
        
        ImGui.InputText("Lightguard", ref guard, flags: ImGuiInputTextFlags.ReadOnly);
        ImGui.InputText("ShameCube", ref cube, flags: ImGuiInputTextFlags.ReadOnly);
    }
}
