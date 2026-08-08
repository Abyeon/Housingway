using System.Threading.Tasks;
using Dalamud.Game.ClientState;
using FFXIVClientStructs.FFXIV.Client.Game;
using Housingway.Tweaks.Base;
using Housingway.Utils;

namespace Housingway.Tweaks;

// Heavily inspired by https://github.com/ktisis-tools/Ktisis/blob/v0.3/main/Ktisis/Services/Data/HousingDataService.cs
public partial class OverrideInteriorLighting : ConfigurableTweak<OverrideInteriorLightingConfig>
{
    public override string Name { get; init; } = "Override Interior Lighting";
    public override string Author { get; init; } = "Abyeon";
    public override string Description { get; init; } = "Overrides the interior lighting of other player's houses to your desired setting.";
    
    public OverrideInteriorLighting()
    {
        HousingService.OnEnterHousingArea += OnEnterHousingArea;
    }

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
        await Service.Framework.Run(UpdateLight);
    }
    
    public override async Task Disable()
    {
        HousingService.OnEnterHousingArea -= OnEnterHousingArea;
        await Service.Framework.Run(() => IndoorLight = InitialValue);
    }
    
    private void OnEnterHousingArea(bool indoors)
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
}
