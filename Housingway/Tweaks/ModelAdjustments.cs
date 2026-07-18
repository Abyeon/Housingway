using System;
using System.Numerics;
using Dalamud.Game.ClientState;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using Housingway.Utils;
using Pictomancy;

namespace Housingway.Tweaks;

public unsafe partial class ModelAdjustments : ConfigurableTweak<ModelAdjustmentsConfig>
{
    public override string Name { get; init; } = "Model Adjustments";
    public override string Author { get; init; } = "Abyeon";

    public override string Description { get; init; } = "Some toggleable adjustments geared towards void builders. " +
                                                        "No more house shell or shame cube.";

    public ModelAdjustments()
    {
        Config = Plugin.Configuration.Tweaks.ModelAdjustments;
    }
    
    public override void Enable()
    {
        Service.ClientState.ZoneInit += OnZoneInit;
        FindModels();
        ToggleModels();
    }

    private void OnOverlay(PctDrawList drawList)
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

    private BgObject* lightguard = null;
    private BgObject* shameCube = null;

    private void OnZoneInit(ZoneInitEventArgs obj)
    {
        // Only check if we're in a house.
        if (obj.TerritoryType.Value.Bg.ToString().Contains("/ind/"))
        {
            Service.Framework.Update += OnUpdate;
            Plugin.Overlay.OnDraw += OnOverlay;
        }
        else
        {
            Service.Framework.Update -= OnUpdate;
            Plugin.Overlay.OnDraw -= OnOverlay;
            lightguard = null;
            shameCube = null;
        }
    }
    
    // Yeah yeah, I'm polling- I don't know a better way atm.
    private void OnUpdate(IFramework framework)
    {
        FindModels();

        if (lightguard != null && shameCube != null)
        {
            ToggleModels();
            Service.Framework.Update -= OnUpdate;
        }
    }

    private void FindModels()
    {
        var man = HousingManager.Instance();
        if (man == null || !man->IsInside()) return;
        
        lightguard = FindByPath("lightgard.mdl");
        shameCube = FindByPath("env_room.mdl");
    }

    private static BgObject* FindByPath(string contains)
    {
        var world = World.Instance();
        foreach (var obj in world->ChildObjects)
        {
            if (obj->GetObjectType() == ObjectType.BgObject)
            {
                var bgObject = (BgObject*)obj;
                if (bgObject->ModelResourceHandle->FileName.ToString().Contains(contains, StringComparison.InvariantCultureIgnoreCase))
                {
                    return bgObject;
                }
            }
        }

        return null;
    }

    private void ToggleModels(bool enable = false)
    {
        if (!HousingService.IsInside) return;
        
        try
        {
            if (lightguard != null)
            {
                lightguard->IsVisible = !Config.DisableLightguard || enable;
                lightguard->UpdateRender();
            }

            if (shameCube != null)
            {
                shameCube->IsVisible = !Config.DisableShameCube || enable;
                shameCube->UpdateRender();
            }
        }
        catch (Exception e)
        {
            Service.Log.Error(e.ToString());
        }
        
    }

    public override void Disable()
    {
        Service.ClientState.ZoneInit -= OnZoneInit;
        Service.Framework.Update -= OnUpdate; // in case this gets disabled while we still haven't found objs
        Plugin.Overlay.OnDraw -= OnOverlay;
        
        ToggleModels(true);
        
        lightguard = null;
        shameCube = null;
    }

    public override void Dispose() { }
}
