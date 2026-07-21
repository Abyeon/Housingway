using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;
using Housingway.Tweaks.Base;

namespace Housingway.Tweaks;

// This was mainly yoinked from https://github.com/ktisis-tools/Ktisis/blob/v0.3/main/Ktisis/Services/Data/HousingDataService.cs
public unsafe class ToggleAmbientOcclusion : BaseTweak
{
    public override string Name { get; init; } = "Disable SSAO";
    public override string Author { get; init; } = "Abyeon";
    public override string Description { get; init; } = "Disables SSAO within housing.";

    private delegate nint ToggleSSAO(HousingManager* instance, bool option);
    
    [Signature("48 89 5C 24 ?? 57 48 83 EC ?? 48 8B 79 ?? 0F B6 DA")]
    private readonly ToggleSSAO? toggle = null;

    public ToggleAmbientOcclusion()
    {
        Service.GameInteropProvider.InitializeFromAttributes(this);
    }

    private void SetSSAO(bool state)
    {
        if (toggle == null)
        {
            Service.Log.Warning($"Sig for ToggleSSAO not found! (I blame Karou and Bwuny)");
            return;
        }
        
        toggle.Invoke(HousingManager.Instance(), state);
    }
    
    private bool SSAOEnabled
    {
        get
        {
            var man = HousingManager.Instance();
            return man != null && man->IsInside() && man->IndoorTerritory->SSAOEnable;
        }
        set
        {
            var man = HousingManager.Instance();
            if (man == null || !man->IsInside()) return;
            
            SetSSAO(value);
        }
    }

    private bool SavedSSAOEnabled
    {
        get
        {
            var man = HousingManager.Instance();
            return man != null && man->IsInside() && man->IndoorTerritory->SavedSSAOEnable;
        }
    }
    
    public override void Enable()
    {
        SSAOEnabled = false;
    }

    public override void Disable()
    {
        SSAOEnabled = SavedSSAOEnabled;
    }

    public override void Dispose() { }
}
