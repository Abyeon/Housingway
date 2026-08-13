using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Layer;
using FFXIVClientStructs.FFXIV.Common.Math;
using FFXIVClientStructs.Interop;
using SceneLight = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Light;
using RenderLight = FFXIVClientStructs.FFXIV.Client.Graphics.Render.Light;

namespace Housingway.Utils;

public class GameLight : IDisposable
{
    public Pointer<SceneLight> Data;

    public bool IsCopy;
    public Pointer<SceneLight> Original;

    private readonly CancellationTokenSource cts = new();
    
    public static unsafe bool TryMakeCopy(Pointer<LightLayoutInstance> instance, out GameLight copy)
    {
        copy = new GameLight();
        copy.IsCopy = true;

        if (instance.IsNull) return false;
        
        SceneLight* scene = instance.Value->GraphicsObject;
        RenderLight* render = scene->RenderLight;

        if (scene->LoadState != 3) return false;

        copy.Data = null;
        copy.Original = scene;

        fixed (byte* poolPtr = "Housingway.Light\0"u8)
        {
            copy.Data = SceneLight.Create(render->LightShape, poolPtr, null);
        }

        if (copy.Data.Value is null) return false;
        
        Task.Run(copy.Init);
        
        return true;
    }

    private static unsafe void CopyTo(RenderLight* source, RenderLight* target)
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

    private bool isLoaded = false;
    private Action<GameLight>? loadAction = null;
    
    private async Task Init()
    {
        Service.Log.Verbose("Creating new light");
        
        if (Data.IsNull) throw new InvalidOperationException("Data is null");
        if (Original.IsNull) throw new InvalidOperationException("Original is null");

        bool loaded = false;
        
        while (!loaded)
        {
            if (cts.IsCancellationRequested) return;

            unsafe
            {
                loaded = Data.Value->LoadState == 3;
            }
            
            await Task.Delay(16);
        }

        await Service.Framework.Run(() =>
        {
            if (Data.IsNull) return;
            if (IsCopy && !Original.IsNull)
            {
                unsafe
                {
                    Data.Value->Position = Original.Value->Position;
                    Data.Value->Rotation = Original.Value->Rotation;
                    Data.Value->Scale = Original.Value->Scale;

                    // need to add gobo stuff

                    Data.Value->RenderLight->Transform = (Transform*)&Data.Value->Position;
                    CopyTo(Original.Value->RenderLight, Data.Value->RenderLight);

                    Service.Log.Verbose($"Light range = {Data.Value->RenderLight->Range} (init)");
                }
            }

            loadAction?.Invoke(this);
            loadAction = null;

            isLoaded = true;
        });
        
        Service.Framework.Update += OnUpdate;
    }
    
    public void RunOnLoad(Action<GameLight> action)
    {
        Debug.Assert(Service.Framework.IsInFrameworkUpdateThread);
        
        if (isLoaded) action(this);
        else
        {
            loadAction = action;
        }
    }

    private void OnUpdate(IFramework framework) => Update();

    private unsafe void Update()
    {
        if (Data.IsNull) return;
        
        if (IsCopy) Original.Value->IsVisible = false;
        Data.Value->UpdateMaterials();
    }

    public unsafe void Dispose()
    {
        Service.Log.Verbose("Cleaning up light");
        Debug.Assert(Service.Framework.IsInFrameworkUpdateThread);
        
        cts.Cancel();
        
        Service.Framework.Update -= OnUpdate;

        if (IsCopy && !Original.IsNull)
            Original.Value->IsVisible = true;
        
        Data.Value->CleanupRender();
        Data.Value->Dtor(1);
        Data = null;
        
        loadAction = null;
    }
}