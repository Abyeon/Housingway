using Dalamud.Game.ClientState;
using FFXIVClientStructs.FFXIV.Client.Game;
using Housingway.Tweaks.Base;

namespace Housingway.Tweaks;

// Heavily inspired by https://github.com/ktisis-tools/Ktisis/blob/v0.3/main/Ktisis/Services/Data/HousingDataService.cs
public unsafe partial class OverrideInteriorLighting : ConfigurableTweak<OverrideInteriorLightingConfig>
{
    public override string Name { get; init; } = "Override Interior Lighting";
    public override string Author { get; init; } = "Abyeon";
    public override string Description { get; init; } = "Overrides the interior lighting of other player's houses to your desired setting.";

    protected override OverrideInteriorLightingConfig Config
    {
        get => Plugin.Configuration.Tweaks.OverrideInteriorLighting;
        set => Plugin.Configuration.Tweaks.OverrideInteriorLighting = value;
    }
    
    public OverrideInteriorLighting()
    {
        Service.ClientState.ZoneInit += OnZoneInit;
    }
    
    private static float InitialValue
    {
        get
        {
            var man = HousingManager.Instance();
            if (man == null || !man->IsInside()) return float.NaN;
            return 1.0f - (man->IndoorTerritory->SavedInvertedBrightness * 0.2f);
        }
    }

    private static float IndoorLight
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
            var speed = value - indoor->BrightnessCurrent;
        
            indoor->BrightnessTarget = value;
            indoor->BrightnessTransitionSpeed = speed;
            indoor->IsBrightnessTransitioning = true;
        }
    }

    public override void Enable() => UpdateLight();
    public override void Disable() => IndoorLight = InitialValue;

    private void OnZoneInit(ZoneInitEventArgs obj) 
    {
        if (Enabled)
        {
            Service.Framework.Run(UpdateLight);
        }
    }

    private void UpdateLight()
    {
        IndoorLight = Config.Light;
    }

    public override void Dispose() => Service.ClientState.ZoneInit -= OnZoneInit;
}
