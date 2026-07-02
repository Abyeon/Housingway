using System;
using Housingway.Utils;
using Scene = Housingway.Utils.Scene;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

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
        // HousingService.OnFurnitureUpdate += OnFurnitureUpdate;
        UpdateFurniture();
    }

    // private void OnFurnitureUpdate(Furniture furniture)
    // {
    //     var cam = Scene.CurrentCamera;
    //     if (cam == null) return;
    //     
    //     var graphics = furniture.Graphics;
    //     if (graphics == null) return;
    //     
    //     var group = furniture.Group;
    //     
    //     try
    //     {
    //         Vector4 bounds = new();
    //         group->GetBoundingSphereImpl(&bounds);
    //
    //         if (IsBetween(cam->Position, Plugin.ObjectTable.LocalPlayer!.Position, new Vector3(bounds.X, bounds.Y, bounds.Z), bounds.W))
    //         {
    //             var alpha = Math.Clamp(graphics->GetTransparency() + 0.01f, 0f, 1f);
    //             graphics->SetTransparency(alpha);
    //         }
    //         else
    //         {
    //             graphics->SetTransparency(0f);
    //         }
    //     }
    //     catch (Exception e)
    //     {
    //         Plugin.Log.Error(e.ToString());
    //     }
    //     
    // }
    
    // public static bool IsBetween(Vector3 camera, Vector3 player, Vector3 target, float thicknessRadius)
    // {
    //     var camToPlayer = player - camera;
    //     var camToTarget = target - camera;
    //
    //     var distSq = Vector3.Dot(camToPlayer, camToPlayer);
    //     var proj = Vector3.Dot(camToTarget, camToPlayer);
    //     
    //     if (proj < 0 || proj > distSq)
    //     {
    //         return false;
    //     }
    //
    //     if (thicknessRadius <= 0f) return true;
    //     
    //     var targetDistSq = Vector3.Dot(camToTarget, camToTarget);
    //     var perpDistSq = targetDistSq - ((proj * proj) / distSq);
    //         
    //     return perpDistSq <= (thicknessRadius * thicknessRadius);
    // }

    private void OnFurnitureAdded(Furniture furniture)
    {
        DisableCameraCollision(furniture);
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
        // HousingService.OnFurnitureUpdate -= OnFurnitureUpdate;
        UpdateFurniture(true);
    }

    public override void Dispose() { }
}
