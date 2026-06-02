using System;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Camera = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Camera;

namespace Housingway.Utils;

public static unsafe class Scene
{
    // var camMan = CameraManager.Instance();
    //     if (camMan == null) return;
    //
    // var cam = camMan->CurrentCamera;
    //     if (cam == null) return;

    public static CameraManager* CameraMan => CameraManager.Instance();
    public static Camera* CurrentCamera => CameraMan != null ? CameraMan->CurrentCamera : null;
    
    public static void RedrawObjects()
    {
        var man = HousingManager.Instance();
        if (man == null || !man->IsInside()) return;

        var furnitureMan = man->GetFurnitureManager();
        
        foreach (ref var ptr in furnitureMan->ObjectManager.ObjectArray.Objects)
        {
            var gameObject = ptr.Value;
            if (gameObject == null) continue;
            
            gameObject->DisableDraw();
            
            Plugin.Log.Verbose($"Redrawing {gameObject->NameString} : {((IntPtr)gameObject):X}");
        }
    }
}
