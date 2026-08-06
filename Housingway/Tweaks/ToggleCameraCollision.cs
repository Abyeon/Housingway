using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using FFXIVClientStructs.Interop;
using Housingway.Tweaks.Base;
using Housingway.Utils;
using Housingway.Utils.Extensions;

namespace Housingway.Tweaks;

public unsafe partial class ToggleCameraCollision : BaseTweak
{
    public override string Name { get; init; } = "Disable Camera Collision";
    public override string Author { get; init; } = "Abyeon";
    public override string Description { get; init; } = "Allows the camera to clip through furnishings!";
    
    public override void Enable()
    {
        HousingService.OnFurnitureAdded += OnFurnitureAdded;
        UpdateFurniture();
    }

    private static void OnFurnitureAdded(Furniture furniture)
    {
        DisableCameraCollision(furniture.Collider);
    }
    
    private static void UpdateFurniture(bool enabled = false)
    {
        if (!HousingService.InHousingArea) return;

        foreach (var furn in HousingService.CurrentFurniture)
        {
            DisableCameraCollision(furn.Collider, enabled);
        }
    }

    private static void DisableCameraCollision(Pointer<Collider> collider, bool enabled = false) => collider.SetMaterialMask(MaterialFlag.CameraCollision, !enabled);

    public override void Disable()
    {
        HousingService.OnFurnitureAdded -= OnFurnitureAdded;
        UpdateFurniture(true);
    }

    public override void Dispose() { }
}
