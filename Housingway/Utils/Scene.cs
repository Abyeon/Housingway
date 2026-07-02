using System;
using Dalamud.Game.ClientState;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
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
        Plugin.ClientState.ZoneInit += OnZoneInit;
        CheckLayout();
    }

    private static void OnZoneInit(ZoneInitEventArgs obj)
    {
        Plugin.Framework.Update += OnUpdate;
        Bg = obj.TerritoryType.Value.Bg.ExtractText();
    }

    private static void OnUpdate(IFramework framework)
    {
        CheckLayout();
    }

    private static void CheckLayout()
    {
        var world = LayoutWorld.Instance();
        if (world == null) return;
        var active = world->ActiveLayout;
        if (active == null) return;

        if (active->InitState == 7)
        {
            Plugin.Framework.Update -= OnUpdate;
            OnZoneLoaded?.Invoke();
        }
    }
    
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

    public void Dispose()
    {
        Plugin.ClientState.ZoneInit -= OnZoneInit;
        Plugin.Framework.Update -= OnUpdate;
    }
}
