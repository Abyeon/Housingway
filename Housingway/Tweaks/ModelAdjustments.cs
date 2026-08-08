using System;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.Interop;
using Housingway.Tweaks.Base;
using Housingway.Utils;
using Pictomancy;

namespace Housingway.Tweaks;

public partial class ModelAdjustments : ConfigurableTweak<ModelAdjustmentsConfig>
{
    public override string Name { get; init; } = "Model Adjustments";
    public override string Author { get; init; } = "Abyeon";

    public override string Description { get; init; } = "Some toggleable adjustments geared towards void builders. " +
                                                        "No more house shell or shame cube.";
    
    public override async Task Enable()
    {
        HousingService.OnEnterHousingArea += OnEnterHousingArea;
        

        await Service.Framework.Run(() =>
        {
            FindModels();
            ToggleModels();
        });
    }

    private unsafe void OnOverlay(PctDrawList drawList)
    {
        if (!Config.ShowBuildLimit) return;
        if (!HousingService.IsInside) return;
        
        var cam = Scene.CurrentCamera;
        if (cam is null) return;
        if (cam->Position.SqrMagnitude < 2025) return; // dist from Vector3.Zero is < 45 units.
        
        var p = new PctDxParams
        {
            OccludedAlpha = 0,
            OcclusionTolerance = 0,
            FresnelOpacity = 1f,
            FresnelIntensity = 1f,
            FresnelSpread = 0.1f,
            ProjectionHeight = 0f,
            FadeStart = 0f,
        };
        
        drawList.AddSphere(Vector3.Zero, 50, 0x0CFFFFFF, p: p);
    }

    private Pointer<BgObject> lightguard = null;
    private Pointer<BgObject> shameCube = null;

    private void OnEnterHousingArea(bool indoors)
    {
        lightguard = null;
        shameCube = null;
        
        // Only check if we're in a house.
        if (indoors)
        {
            Service.Framework.Update += OnUpdate;
            Plugin.Overlay.OnDraw += OnOverlay;
        }
        else
        {
            Service.Framework.Update -= OnUpdate;
            Plugin.Overlay.OnDraw -= OnOverlay;
        }
    }
    
    // Yeah yeah, I'm polling- I don't know a better way atm.
    private void OnUpdate(IFramework framework)
    {
        FindModels();

        if (!lightguard.IsNull && !shameCube.IsNull)
        {
            ToggleModels();
            Service.Framework.Update -= OnUpdate;
        }
    }

    private unsafe void FindModels()
    {
        var man = HousingManager.Instance();
        if (man == null || !man->IsInside()) return;
        
        var world = World.Instance();
        foreach (var obj in world->ChildObjects)
        {
            if (obj->GetObjectType() != ObjectType.BgObject) continue;
            var bgObject = (BgObject*)obj;
            string name = bgObject->ModelResourceHandle->FileName.ToString();
            
            if (name.Contains("lightgard.mdl", StringComparison.InvariantCultureIgnoreCase))
            {
                lightguard = bgObject;
            } else if (name.Contains("env_room.mdl", StringComparison.InvariantCultureIgnoreCase))
            {
                shameCube = bgObject;
            }
        }
    }

    private unsafe void ToggleModels(bool enable = false)
    {
        if (!HousingService.IsInside) return;
        
        try
        {
            if (!lightguard.IsNull || shameCube.Value->LoadState != 7)
            {
                lightguard.Value->IsVisible = !Config.DisableLightguard || enable;
                lightguard.Value->UpdateRender();
            }

            if (!shameCube.IsNull || shameCube.Value->LoadState != 7)
            {
                shameCube.Value->IsVisible = !Config.DisableShameCube || enable;
                shameCube.Value->UpdateRender();
            }
        }
        catch (Exception e)
        {
            Service.Log.Error(e.ToString());
        }
        
    }

    public override async Task Disable()
    {
        HousingService.OnEnterHousingArea -= OnEnterHousingArea;
        Service.Framework.Update -= OnUpdate; // in case this gets disabled while we still haven't found objs
        Plugin.Overlay.OnDraw -= OnOverlay;
        
        await Service.Framework.Run(() =>
        {
            FindModels();
            ToggleModels(true);
        });
        
        lightguard = null;
        shameCube = null;
    }
}
