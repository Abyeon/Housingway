using System;
using Dalamud.Game.ClientState;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using Housingway.Config;
using Housingway.Utils;
using InteropGenerator.Runtime;

namespace Housingway.Tweaks;

public unsafe partial class OverrideInteriorLighting : ConfigurableTweak<OverrideInteriorLightingConfig>
{
    public override string Name { get; init; } = "Override Interior Lighting";
    public override string Description { get; init; } = "Overrides the interior lighting of other player's houses to your desired lighting";
    public override bool Enabled { get; set; }

    private readonly Plugin plugin;

    public OverrideInteriorLighting(Plugin plugin)
    {
        this.plugin = plugin;
        PluginConfig = plugin.Configuration;
        Config = PluginConfig.Tweaks.OverrideInteriorLighting;
    }

    public override void Enable()
    {
        SetInitialValue();
        Plugin.ClientState.ZoneInit += OnZoneInit;
        UpdateLight();
    }
    
    private void OnZoneInit(ZoneInitEventArgs obj) 
    {
        Plugin.Framework.Run(SetInitialValue);
        UpdateLight();
    }
    
    private float initialValue;

    private void SetInitialValue()
    {
        var man = HousingManager.Instance();
        if (man == null || !man->IsInside()) return;

        var indoor = man->IndoorTerritory;
        if (indoor == null) return;
        
        initialValue = indoor->BrightnessCurrent;
    }
    
    private void UpdateLight()
    {
        var man = HousingManager.Instance();
        if (man->IsInside())
        {
            Plugin.Log.Debug($"Updating interior light to {Config.Light}");
            Plugin.Framework.Run(() => SetInteriorLight(Config.Light + 0.00001f));
            Plugin.Framework.Run(() => SetInteriorLight(Config.Light));
        }
    }
    
    private void SetInteriorLight(float target)
    {
        var man = HousingManager.Instance();
        if (man == null) return;

        if (!man->IsInside()) return;

        var indoor = man->IndoorTerritory;
        if (indoor == null) return;

        var speed = target - indoor->BrightnessCurrent;
            
        indoor->BrightnessTarget = target;
        indoor->BrightnessTransitionSpeed = speed;
        indoor->IsBrightnessTransitioning = true;
    }

    public override void Dispose()
    {
        SetInteriorLight(initialValue);
        Plugin.ClientState.ZoneInit -= OnZoneInit;
    }
}
