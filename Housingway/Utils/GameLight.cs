using System;
using System.Diagnostics;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Layer;
using FFXIVClientStructs.Interop;
using SceneLight = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Light;

namespace Housingway.Utils;

public unsafe class GameLight : IDisposable
{
    public SceneLight* Data;

    private bool isCopy;
    private SceneLight* original;
    
    public static bool TryMakeCopy(Pointer<LightLayoutInstance> instance, out GameLight copy)
    {
        copy = new GameLight();
        copy.isCopy = true;

        if (instance.IsNull) return false;
        
        var scene = instance.Value->GraphicsObject;
        var render = scene->RenderLight;

        if (scene->LoadState != 3) return false;

        copy.Data = null;
        copy.original = scene;

        fixed (byte* poolPtr = "Housingway.Light\0"u8)
        {
            copy.Data = SceneLight.Create(render->LightShape, poolPtr, null);
        }

        if (copy.Data is null) return false;

        copy.Data->Position = scene->Position;
        copy.Data->Rotation = scene->Rotation;
        copy.Data->Scale = scene->Scale;

        *copy.Data->RenderLight = *render;
        copy.Data->RenderLight->Transform = (Transform*)&copy.Data->Position;
        copy.Init();
        
        return true;
    }

    private void Init()
    {
        Service.Log.Verbose($"Creating new light");
        Service.Framework.Update += OnUpdate;
    }

    public bool IsLoaded()
    {
        if (Data is null) return false;
        return Data->LoadState == 3;
    }

    private void OnUpdate(IFramework framework) => Update();

    private void Update()
    {
        if (!IsLoaded()) return;
        
        if (isCopy) original->IsVisible = false;
        Data->UpdateMaterials();
    }

    public void Dispose()
    {
        Service.Log.Verbose($"Cleaning up light");
        Debug.Assert(Service.Framework.IsInFrameworkUpdateThread);
        
        Service.Framework.Update -= OnUpdate;

        if (isCopy && original is not null)
            original->IsVisible = true;
        
        Data->CleanupRender();
        Data->Dtor(1);
        Data = null;
    }
}