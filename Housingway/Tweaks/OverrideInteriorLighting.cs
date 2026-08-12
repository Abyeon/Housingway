using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Game.ClientState;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Group;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Layer;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Node;
using FFXIVClientStructs.FFXIV.Common.Math;
using FFXIVClientStructs.Interop;
using Housingway.Tweaks.Base;
using Housingway.Utils;

namespace Housingway.Tweaks;

// Heavily inspired by https://github.com/ktisis-tools/Ktisis/blob/v0.3/main/Ktisis/Services/Data/HousingDataService.cs
public partial class OverrideInteriorLighting : ConfigurableTweak<OverrideInteriorLightingConfig>
{
    public override string Name { get; init; } = "Override Interior Lighting";
    public override string Author { get; init; } = "Abyeon";
    public override string Description { get; init; } = "Overrides the interior lighting of other player's houses to your desired setting.";

    private CancellationTokenSource cts = new();
    
    private readonly List<GameLight> gameLights = [];
    
    private static unsafe float InitialValue
    {
        get
        {
            var man = HousingManager.Instance();
            if (man == null || !man->IsInside()) return float.NaN;
            return 1.0f - (man->IndoorTerritory->SavedInvertedBrightness * 0.2f);
        }
    }

    private static unsafe float IndoorLight
    {
        get
        {
            var man = HousingManager.Instance();
            if (man == null || !man->IsInside()) return float.NaN;
            
            return man->IndoorTerritory->BrightnessCurrent;
        }
        set
        {
            var man = HousingManager.Instance();
            if (man == null || !man->IsInside()) return;

            var indoor = man->IndoorTerritory;
            
            indoor->BrightnessCurrent = value + 0.000001f; // literally just to make sure the light updates on zone init
            float speed = value - indoor->BrightnessCurrent;
        
            indoor->BrightnessTarget = value;
            indoor->BrightnessTransitionSpeed = speed;
            indoor->IsBrightnessTransitioning = true;
        }
    }

    public override async Task Enable()
    {
        HousingService.OnEnterHousingArea += OnEnterHousingArea;
        Service.ClientState.ZoneInit += OnZoneInit;
        
        cts = new CancellationTokenSource();
        
        if (HousingService.IsInside)
        {
            await Update();
        }
    }

    private void OnZoneInit(ZoneInitEventArgs obj)
    {
        ClearLights();
    }

    private void OnEnterHousingArea(bool indoors)
    {
        ClearLights();
        if (indoors) Task.Run(Update);
    }

    private async Task Update()
    {
        if (Config.ConfigFlags.HasFlag(LightConfigFlags.Object))
            await FindLights();
        
        await Service.Framework.Run(() =>
        {
            if (Config.ConfigFlags.HasFlag(LightConfigFlags.Brightness))
                SetLight(Config.Light);
            
            if (Config.ConfigFlags.HasFlag(LightConfigFlags.Object))
                ApplySettings();
        }, cts.Token);
    }
    
    private void ClearLights()
    {
        if (gameLights.Count == 0) return;
        gameLights.AggregateToDisposable().Dispose();
        gameLights.Clear();
    }
    
    private unsafe Task FindLights()
    {
        Service.Log.Verbose("Searching for lights");
        
        var active = LayoutWorld.Instance()->ActiveLayout;
        
        foreach (var layer in active->Layers.Values)
        {
            if (layer.IsNull) continue;
            foreach (var instance in layer.Value->Instances.Values)
            {
                if (instance.IsNull) continue;
                if (instance.Value->Id.Type != InstanceType.SharedGroup) continue;
                
                var group = (SharedGroupLayoutInstance*)instance.Value;

                foreach (Pointer<ChildNodeInstance> child in group->Instances.Instances)
                {
                    if (child.IsNull) continue;
                    if (child.Value->Instance->Id.Type != InstanceType.Light) continue;
                
                    Service.Log.Verbose($"Light found: {child.Value->Instance->Id.InstanceKey}");
                    var light = (LightLayoutInstance*)child.Value->Instance;
                    if (light->LightType != LightType.Point) continue;
                    
                    if (GameLight.TryMakeCopy(light, out GameLight copy))
                    {
                        gameLights.Add(copy);
                    }
                }
            }
        }

        return Task.CompletedTask;
    }
    
    private unsafe void ApplySettings()
    {
        // SetLight(Config.Light);
        
        foreach (var light in gameLights)
        {
            if (cts.IsCancellationRequested) return;
            if (!light.IsLoaded()) continue;
            var sceneLight = light.Data;
            var renderLight = sceneLight->RenderLight;

            if (renderLight == null) continue;
            
            // renderLight->LightFlags = Config.LightFlags;
            // renderLight->
            // renderLight->LightShape = Config.LightShape;
            renderLight->Color = Config.Color;
            renderLight->Intensity = Config.Intensity;
            // renderLight->FalloffFactor = Config.FalloffFactor;
            // renderLight->AngularFalloffDegrees = Config.AngularFalloffDegrees;
            renderLight->Range = Config.Range;
            // renderLight->CharacterShadowRange = Config.CharacterShadowRange;
            
            renderLight->CullingBounds = new AxisAlignedBounds(Vector3.NegativeInfinity, Vector3.PositiveInfinity);
            sceneLight->UpdateCulling();
            // sceneLight->UpdateMaterials();
        }
    }
    
    public override async Task Disable()
    {
        HousingService.OnEnterHousingArea -= OnEnterHousingArea;
        Service.ClientState.ZoneInit -= OnZoneInit;

        await cts.CancelAsync();
        
        await Service.Framework.Run(() =>
        {
            ClearLights();
            SetLight(InitialValue);
        });
    }

    public override void ResetConfig()
    {
        base.ResetConfig();
        SetLight(InitialValue);
        Config.Light = IndoorLight;
        ApplySettings();
    }

    private void SetLight(float value)
    {
        IndoorLight = value;
    }
}
