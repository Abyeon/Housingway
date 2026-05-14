using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Layer;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision.Math;
using Housingway.Utils;

namespace Housingway.Tweaks;

[Flags]
public enum CollisionFlags : ulong
{
    None = 0,
    CameraCollision = 1UL << 12, // Camera collides with this
    PlayerCollision = 1UL << 13, // Player collides with this
}

public unsafe partial class ToggleCameraCollision : BaseTweak
{
    public override string Name { get; init; } = "Disable Camera Collision";
    public override string Description { get; init; } = "Allows the camera to clip through furnishings!";
    public override bool Enabled { get; set; }
    
    public override void Enable()
    {
        HousingService.OnFurnitureAdded += OnFurnitureAdded;
        UpdateFurniture();
    }

    private void OnFurnitureAdded(Furniture furniture)
    {
        DisableCameraCollision(furniture);
    }

    private List<int> faded = [];

    private void DoFade()
    {
        if (Plugin.ObjectTable.LocalPlayer == null) return;
        if (!HousingService.IsInside) return;
        
        var camMan = CameraManager.Instance();
        if (camMan == null) return;

        var cam = camMan->CurrentCamera;
        if (cam == null) return;
        
        Vector3 start = cam->Position;
        Vector3 end = Plugin.ObjectTable.LocalPlayer.Position;
        Vector4 boundSphere = ((start + end) * 0.5f).AsVector4() with { W = Vector3.Distance(start, end) * 0.5f };

        foreach (var furn in HousingService.CurrentFurniture)
        {
            var index = furn.HousingFurniture.Index;
            var obj = furn.Object;
            if (obj == null) continue;
            
            var cull = furn.Cull;
            if (cull == null || cull->Distance >= 1000f) continue;

            var dist = Vector3.Distance(obj->Position, boundSphere.AsVector3());

            var graphics = furn.Graphics;
            if (graphics == null) continue;

            Plugin.Log.Verbose($"Trying to set transparency on {obj->NameString}");
            
            // try
            // {
            //     if (dist < boundSphere.W)
            //     {
            //         faded?.Add(index);
            //         graphics->SetTransparency(1f);
            //     }
            //     else if (faded != null && faded.Contains(index))
            //     {
            //         faded.Remove(index);
            //         graphics->SetTransparency(0);
            //     }
            // }
            // catch (Exception e)
            // {
            //     Plugin.Log.Error($"Error while trying to set transparency on {obj->NameString}.\n{e}");
            // }
            
        }
    }
    
    private void UpdateFurniture(bool enabled = false)
    {
        if (!HousingService.IsInside) return;

        foreach (var furn in HousingService.CurrentFurniture)
        {
            DisableCameraCollision(furn, enabled);
        }
    }

    private static void DisableCameraCollision(Furniture furniture, bool enabled = false)
    {
        var collider = furniture.Collider;
        
        if (collider == null) return;
            
        if (enabled)
        {
            collider->ObjectMaterialMask &= ~(1UL << 12);
        }
        else
        {
            collider->ObjectMaterialMask |= (1UL << 12);
        }
    }

    public override void Disable()
    {
        HousingService.OnFurnitureAdded -= OnFurnitureAdded;
        UpdateFurniture(true);
    }

    public override void Dispose() { }
}
