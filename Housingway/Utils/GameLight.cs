using System;
using System.Diagnostics;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Layer;
using FFXIVClientStructs.FFXIV.Common.Math;
using FFXIVClientStructs.Interop;
using SceneLight = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Light;

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
        var copyLight = copy.Data->RenderLight;

        copy.Data->Position = scene->Position;
        copy.Data->Rotation = scene->Rotation;
        copy.Data->Scale = scene->Scale;
        
        // need to add gobo stuff

        copyLight->Transform = (Transform*)&copy.Data->Position;
        
        copyLight->LightFlags = render->LightFlags;
        copyLight->LightShape = render->LightShape;
        copyLight->Color = render->Color;
        copyLight->Intensity = render->Intensity;
        copyLight->MaxRange = render->MaxRange;
        copyLight->ShadowPlaneNear = render->ShadowPlaneNear;
        copyLight->ShadowPlaneFar = render->ShadowPlaneFar;
        copyLight->FalloffType = render->FalloffType;
        copyLight->FlatLightSkewAngleDegrees = render->FlatLightSkewAngleDegrees;
        copyLight->FalloffFactor = render->FalloffFactor;
        copyLight->SpotLightAngleDegrees = render->SpotLightAngleDegrees;
        copyLight->AngularFalloffDegrees = render->AngularFalloffDegrees;
        copyLight->Range = render->Range;
        copyLight->CharacterShadowRange = render->CharacterShadowRange;
        copyLight->CullingBounds = render->CullingBounds;
        copyLight->RangeBounds = render->RangeBounds;
        copyLight->EnableSSAOMaybe = render->EnableSSAOMaybe;
        copyLight->ShadowBiasMaybe = render->ShadowBiasMaybe;
        copyLight->ShadowDepthNear = render->ShadowDepthNear;
        copyLight->ShadowDepthFar = render->ShadowDepthFar;
        copyLight->ShadowStartDist = render->ShadowStartDist;
        copyLight->ShadowEndDist = render->ShadowEndDist;
        copyLight->LightFade = render->LightFade;
        copyLight->LightFadeLength = render->LightFadeLength;
        copyLight->LightSelect = render->LightSelect;
        
        copyLight->CullingBounds = new AxisAlignedBounds(Vector3.NegativeInfinity, Vector3.PositiveInfinity);
        copy.Data->UpdateCulling();
        
        copy.Init();
        
        return true;
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