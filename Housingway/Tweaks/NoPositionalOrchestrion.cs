using System.Numerics;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Housingway.Tweaks.Base;
using Housingway.Utils;
using Vector4 = FFXIVClientStructs.FFXIV.Common.Math.Vector4;

namespace Housingway.Tweaks;

public class NoPositionalOrchestrion : BaseTweak
{
    public override string Name { get; init; } = "No Positional Orchestrion";
    public override string Author { get; init; } = "Abyeon";
    public override string Description { get; init; } = "Disables the IsPositional flag on orchestrions, so your music plays in your head!";
    
    public override void Enable()
    {
        Service.Framework.Update += OnUpdate;
        HousingService.OnEnterHousingArea += OnEnterHousingArea;

        if (HousingService.IsInside)
        {
            FindOrchestrion();
        }
    }

    private Vector3 orchestrionPosition = Vector3.Zero;

    private void OnEnterHousingArea(bool indoors)
    {
        if (!indoors) return;
        FindOrchestrion();
    }

    private unsafe void FindOrchestrion()
    {
        orchestrionPosition = Vector3.Zero;
        
        Service.Log.Verbose($"Searching for orchestrion.");
        foreach (var furn in HousingService.CurrentFurniture)
        {
            if (furn.Object is null) continue;
            
            var sheet = furn.FurnitureSheet;
            if (!sheet.HasValue) continue;
            
            if (sheet.Value.CustomTalk.TryGetValue(out var customTalk))
            {
                if (customTalk.Name.Equals("HouFurOrchestrion_00330"))
                {
                    orchestrionPosition = furn.Object->Position;
                    Service.Log.Verbose($"Found orchestrion at {orchestrionPosition}");
                    return;
                }
            }
        }
        
        Service.Log.Verbose("Found no orchestrion.");
    }

    private unsafe void OnUpdate(IFramework framework)
    {
        SetPositional(false);
    }
    
    private unsafe void SetPositional(bool isPositional)
    {
        var man = OrchestrionManager.Instance();
        if (man is null) return;
        
        var sound = man->SoundData;
        if (sound is null) return;
        
        if (sound->IsPositional == isPositional) return;
        
        sound->IsPositional = isPositional;
        sound->SoundController.SetIsNonPositional(!isPositional);

        // Need to update the position in case we entered the house with the tweak on.
        var position = new Vector4(orchestrionPosition.X, orchestrionPosition.Y, orchestrionPosition.Z, 1);
        sound->SoundController.SetPosition(&position);
    }

    public override void Disable()
    {
        Service.Framework.Update -= OnUpdate;
        HousingService.OnEnterHousingArea -= OnEnterHousingArea;

        if (HousingService.IsInside)
        {
            FindOrchestrion();
            SetPositional(true);
        }
    }

    public override void Dispose() { }
}
