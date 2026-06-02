using System;
using Dalamud.Game.ClientState;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Housingway.Utils;

namespace Housingway.Tweaks;

public unsafe partial class ModelAdjustments : ConfigurableTweak<ModelAdjustmentsConfig>
{
    public override string Name { get; init; } = "Model Adjustments";

    public override string Description { get; init; } = "Some toggleable adjustments geared towards void builders. " +
                                                        "No more house shell or shame cube.";
    public override bool Enabled { get; set; }

    public ModelAdjustments(Plugin plugin)
    {
        PluginConfig = plugin.Configuration;
        Config = PluginConfig.Tweaks.ModelAdjustments;
    }
    
    public override void Enable()
    {
        Plugin.ClientState.ZoneInit += OnZoneInit;
        FindModels();
        ToggleModels();
    }
    
    private BgObject* lightguard = null;
    private BgObject* shameCube = null;

    private void OnZoneInit(ZoneInitEventArgs obj)
    {
        // Only check if we're in a house.
        if (obj.TerritoryType.Value.Bg.ToString().Contains("/ind/"))
        {
            Plugin.Framework.Update += OnUpdate;
        }
        else
        {
            Plugin.Framework.Update -= OnUpdate;
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
            Plugin.Framework.Update -= OnUpdate;
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
            Plugin.Log.Error(e.ToString());
        }
        
    }

    public override void Disable()
    {
        Plugin.ClientState.ZoneInit -= OnZoneInit;
        Plugin.Framework.Update -= OnUpdate; // in case this gets disabled while we still haven't found objs
        
        ToggleModels(true);
    }

    public override void Dispose()
    {
        lightguard = null;
        shameCube = null;
    }
}
