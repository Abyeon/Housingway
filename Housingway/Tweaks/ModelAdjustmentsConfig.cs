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
        
        DrawDebug();

        var camMan = CameraManager.Instance();
        if (camMan is null) return;

        var cam = camMan->GetActiveCamera();
        if (cam is null) return;
        
        var distance = Vector3.Distance(cam->LastPosition, Vector3.Zero);

        if (limit && distance >= 45)
        {
            using var drawList = PctService.Draw(ImGui.GetBackgroundDrawList(), new PctDrawHints
            {
                UIMask = UIMask.BackbufferAlpha,
                DrawWhenFaded = true,
                DrawInCutscene = true,
                DefaultParams = new PctDxParams
                {
                    OccludedAlpha = 0,
                    OcclusionTolerance = 0,
                    FresnelOpacity = 1f,
                    FresnelIntensity = 1f,
                    FresnelSpread = 0.1f,
                    ProjectionHeight = 0f,
                    FadeStart = 0f,
                }
            });

            if (drawList is null) return;
            
            drawList.AddSphere(Vector3.Zero, 50, 0x0CFFFFFF);
        }
    }

    [Conditional("DEBUG")]
    private void DrawDebug()
    {
        var guard = lightguard is null ? "Null" : lightguard->ModelResourceHandle->FileName.ToString();
        var cube = shameCube is null ? "Null" : shameCube->ModelResourceHandle->FileName.ToString();
        
        ImGui.InputText("Lightguard", ref guard, flags: ImGuiInputTextFlags.ReadOnly);
        ImGui.InputText("ShameCube", ref cube, flags: ImGuiInputTextFlags.ReadOnly);
    }
}
