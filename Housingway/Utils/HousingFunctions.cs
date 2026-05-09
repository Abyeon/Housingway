using System;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using InteropGenerator.Runtime;

namespace Housingway.Utils;

public unsafe class HousingFunctions : IDisposable
{
    public delegate void PrefetchZoneDelegate(LayoutManager* layoutManager, uint id, CStringPointer bg, CStringPointer bgNoExtension, uint territoryType, uint layerFilterKey, int type, GameMain.Festival[] festivals, uint cfcId);
    private delegate IntPtr LoadFurnitureDelegate(IntPtr a1, IntPtr a2);
    public delegate void LoadFurniture(IntPtr a1, IntPtr a2);
    private delegate nint SetInteriorLightDelegate(IndoorTerritory* indoor, nint inverse, nint a3);
    public delegate void UpdateInteriorLight(IndoorTerritory* indoor, nint inverse, nint a3);

    [Signature("40 53 48 83 EC ?? 8B 44 24 ?? 48 8B D9 89 41", DetourName = nameof(PrefetchZoneDetour))]
    private readonly Hook<PrefetchZoneDelegate>? prefetchZoneHook = null!;

    [Signature("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 48 89 7C 24 ?? 41 56 48 83 EC 20 48 8B 71 08 48 8B FA", DetourName = nameof(LoadFurnitureDetour))]
    private readonly Hook<LoadFurnitureDelegate>? loadFurnitureHook = null!;

    [Signature("48 89 5C 24 ?? 57 48 83 EC ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 44 24 ?? 0F B6 FA 48 8B D9 40 80 FF")]
    private readonly Hook<SetInteriorLightDelegate>? setInteriorLightHook = null!;
    
    public event PrefetchZoneDelegate? OnPrefetchZone;
    public event LoadFurniture? OnLoadFurniture;
    public event UpdateInteriorLight? OnSetInteriorLight;

    public HousingFunctions()
    {
        Plugin.GameInteropProvider.InitializeFromAttributes(this);
        prefetchZoneHook?.Enable();
        loadFurnitureHook?.Enable();
        setInteriorLightHook?.Enable();
    }
    
    public void PrefetchZoneDetour(LayoutManager* layoutManager, uint id, CStringPointer bg, CStringPointer bgNoExtension, uint territoryType, uint layerFilterKey, int type, GameMain.Festival[] festivals, uint cfcId)
    {
        Plugin.Log.Verbose($"Loading: {bg}");
        
        prefetchZoneHook!.Original(layoutManager, id, bg, bgNoExtension, territoryType, layerFilterKey, type, festivals, cfcId);
        OnPrefetchZone?.Invoke(layoutManager, id, bg, bgNoExtension, territoryType, layerFilterKey, type, festivals, cfcId);
    }

    public IntPtr LoadFurnitureDetour(IntPtr a1, IntPtr a2)
    {
        Plugin.Log.Verbose($"Furniture loading: {a1}, {a2}");
        
        var ret = loadFurnitureHook!.Original(a1, a2);
        OnLoadFurniture?.Invoke(a1, a2);

        return ret;
    }

    public nint SetInteriorLightDetour(IndoorTerritory* indoor, nint inverse, nint a3)
    {
        var ret = setInteriorLightHook!.Original(indoor, inverse, a3);
        OnSetInteriorLight?.Invoke(indoor, inverse, a3);
        return ret;
    }

    public void Dispose()
    {
        prefetchZoneHook?.Dispose();
        loadFurnitureHook?.Dispose();
        setInteriorLightHook?.Dispose();
    }
}
