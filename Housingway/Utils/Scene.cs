using System;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using Serilog;

namespace Housingway.Utils;

public static unsafe class Scene
{
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
