using System;
using Dalamud.Game.ClientState;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using FFXIVClientStructs.Interop;
using Camera = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Camera;

namespace Housingway.Utils;

public unsafe class Scene : IDisposable
{
    public static CameraManager* CameraMan => CameraManager.Instance();
    public static Camera* CurrentCamera => CameraMan != null ? CameraMan->CurrentCamera : null;
    public static string Bg = string.Empty;

    internal delegate void ZoneLoaded();
    internal static event ZoneLoaded? OnZoneLoaded;
    
    public Scene()
    {
        Service.ClientState.ZoneInit += OnZoneInit;
        CheckLayout();
    }

    private static void OnZoneInit(ZoneInitEventArgs obj)
    {
        Service.Framework.Update += OnUpdate;
        Bg = obj.TerritoryType.Value.Bg.ExtractText();
    }

    private static void OnUpdate(IFramework framework)
    {
        CheckLayout();
    }

    private static void CheckLayout()
    {
        LayoutManager* active = LayoutWorld.Instance()->ActiveLayout;
        if (active == null) return;

        if (active->InitState != 7 || GameMain.Instance()->TerritoryLoadState != 2) return;
        
        Service.Framework.Update -= OnUpdate;
        OnZoneLoaded?.Invoke();
    }
    
    public static void RedrawObjects()
    {
        HousingManager* man = HousingManager.Instance();
        if (man == null || !man->IsInside()) return;

        HousingFurnitureManager* furnitureMan = man->GetFurnitureManager();
        
        foreach (ref Pointer<GameObject> ptr in furnitureMan->ObjectManager.ObjectArray.Objects)
        {
            GameObject* gameObject = ptr.Value;
            if (gameObject == null) continue;
            
            gameObject->DisableDraw();
            
            Service.Log.Verbose($"Redrawing {gameObject->NameString} : {((IntPtr)gameObject):X}");
        }
    }

    public void Dispose()
    {
        Service.ClientState.ZoneInit -= OnZoneInit;
        Service.Framework.Update -= OnUpdate;
    }
}
