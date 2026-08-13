using System;
using System.Diagnostics;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Layer;
using FFXIVClientStructs.FFXIV.Common.Math;
using FFXIVClientStructs.Interop;
using SceneLight = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Light;
using RenderLight = FFXIVClientStructs.FFXIV.Client.Graphics.Render.Light;

namespace Housingway.Utils;

public unsafe class GameLight : IDisposable
{
    public SceneLight* Data;

    public bool IsCopy;
    public SceneLight* Original;
    
    public static bool TryMakeCopy(Pointer<LightLayoutInstance> instance, out GameLight copy)
    {
        copy = new GameLight();
        copy.IsCopy = true;

        if (instance.IsNull) return false;
        
        var scene = instance.Value->GraphicsObject;
        var render = scene->RenderLight;

        if (scene->LoadState != 3) return false;

        copy.Data = null;
        copy.Original = scene;

        fixed (byte* poolPtr = "Housingway.Light\0"u8)
        {
            copy.Data = SceneLight.Create(render->LightShape, poolPtr, null);
        }

        if (copy.Data is null) return false;
        
        copy.Data->Position = scene->Position;
        copy.Data->Rotation = scene->Rotation;
        copy.Data->Scale = scene->Scale;
        
        // need to add gobo stuff

        copy.Data->RenderLight->Transform = (Transform*)&copy.Data->Position;
        CopyTo(render, copy.Data->RenderLight);
        
        copy.Init();
        
        return true;
    }

    private static void CopyTo(RenderLight* source, RenderLight* target)
    {
        target->LightFlags = source->LightFlags;
        target->LightShape = source->LightShape;
        target->Color = source->Color;
        target->Intensity = source->Intensity;
        target->MaxRange = source->MaxRange;
        target->ShadowPlaneNear = source->ShadowPlaneNear;
        target->ShadowPlaneFar = source->ShadowPlaneFar;
        target->FalloffType = source->FalloffType;
        target->FlatLightSkewAngleDegrees = source->FlatLightSkewAngleDegrees;
        target->FalloffFactor = source->FalloffFactor;
        target->SpotLightAngleDegrees = source->SpotLightAngleDegrees;
        target->AngularFalloffDegrees = source->AngularFalloffDegrees;
        target->Range = source->Range;
        target->CharacterShadowRange = source->CharacterShadowRange;
        target->CullingBounds = source->CullingBounds;
        target->RangeBounds = source->RangeBounds;
        target->EnableSSAOMaybe = source->EnableSSAOMaybe;
        target->ShadowBiasMaybe = source->ShadowBiasMaybe;
        target->ShadowDepthNear = source->ShadowDepthNear;
        target->ShadowDepthFar = source->ShadowDepthFar;
        target->ShadowStartDist = source->ShadowStartDist;
        target->ShadowEndDist = source->ShadowEndDist;
        target->LightFade = source->LightFade;
        target->LightFadeLength = source->LightFadeLength;
        target->LightSelect = source->LightSelect;
    }

    private void Init()
    {
        Service.Log.Verbose($"Creating new light");
        Service.Framework.Update += OnUpdate;
        Service.Log.Verbose($"Light range = {Data->RenderLight->Range} (init)");
    }

    public bool IsLoaded()
    {
        if (Data is null) return false;
        return Data->LoadState == 3;
    }

    private void OnUpdate(IFramework framework) => Update();

    private void Update()
    {
        if (Data is null) return;
        
        if (IsCopy) Original->IsVisible = false;
        Data->UpdateMaterials();
    }

    public void Dispose()
    {
        Service.Log.Verbose($"Cleaning up light");
        Debug.Assert(Service.Framework.IsInFrameworkUpdateThread);
        
        Service.Framework.Update -= OnUpdate;

        if (IsCopy && Original is not null)
            Original->IsVisible = true;
        
        Data->CleanupRender();
        Data->Dtor(1);
        Data = null;
    }
}