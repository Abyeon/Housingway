using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.Interop;
using Housingway.Profiles;

namespace Housingway.Utils;

public unsafe class HousingService : IDisposable
{
    public static HousingManager* Manager  => HousingManager.Instance();
    public static HousingFurnitureManager* FurnitureManager => Manager == null ? null : Manager->GetFurnitureManager();

    internal static bool IsInside;
    internal static bool IsOutside;
    internal static bool InHousingArea => IsInside || IsOutside;

    internal static Address? CurrentAddress;
    
    internal delegate void FurnitureAdded(Furniture furniture);
    internal delegate void EnterHousingArea(bool indoors);
    
    internal static event FurnitureAdded? OnFurnitureAdded;
    internal static event EnterHousingArea? OnEnterHousingArea;

    internal static ConcurrentDictionary<ulong, Furniture> CurrentFurniture = [];
    
    private readonly HashSet<ulong> touched = [];
    private readonly List<ulong> toRemove = [];

    public HousingService()
    {
        Scene.OnZoneLoaded += OnZoneLoaded;

        Service.Framework.Run(() =>
        {
            CheckForHousing();

            if (InHousingArea) UpdateFurniture();
        });
    }

    private void OnZoneLoaded()
    {
        CurrentFurniture.Clear();
        CheckForHousing();
    }

    private void CheckForHousing()
    {
        Service.Framework.Update -= OnUpdate;
        
        if (!Service.ClientState.IsLoggedIn) return;

        HousingManager* manager = Manager;
        IsInside = manager != null && manager->IsInside();
        IsOutside = manager != null && manager->IsOutside();
        
        if (InHousingArea)
        {
            UpdateFurniture();
            Service.Framework.Update += OnUpdate;

            CurrentAddress = Address.TryGetAddress(out var address) ? address : null;
            OnEnterHousingArea?.Invoke(IsInside);
        }
        else
        {
            CurrentAddress = null;
            CurrentFurniture.Clear();
        }
    }

    private void OnUpdate(IFramework framework) => UpdateFurniture();
    
    private void UpdateFurniture()
    {
        touched.Clear();
        
        var furnitureManager = FurnitureManager;
        if (furnitureManager == null)
        {
            if (!CurrentFurniture.IsEmpty) CurrentFurniture.Clear();
            return;
        }
        
        ref HousingObjectManager.HousingObjectArray arr = ref furnitureManager->ObjectManager.ObjectArray;
        int max = Math.Min(arr.Objects.Length, arr.ObjectCount);
        
        foreach (Pointer<HousingFurniture> furn in furnitureManager->FurnitureVector)
        {
            if (furn.IsNull) continue;
            
            int index = furn.Value->Index;
            if ((uint)index >= (uint)max) continue;
            
            var obj = (HousingObject*)arr.Objects[index].Value;
            if (obj == null) continue;
            
            ulong id = obj->GetGameObjectId().Id;

            if (CurrentFurniture.TryGetValue(id, out var existing))
            {
                if (existing.Index != index)
                {
                    CurrentFurniture[id] = new Furniture(id, index, obj->HousingObjectId);
                }

                touched.Add(id);
                continue;
            }

            Furniture furniture = new Furniture(id, index, obj->HousingObjectId);
            if (!furniture.IsValid) continue;

            touched.Add(id);
            CurrentFurniture[id] = furniture;
            OnFurnitureAdded?.Invoke(furniture);
        }

        if (CurrentFurniture.Count == touched.Count) return;
        
        toRemove.Clear();
        foreach (ulong id in CurrentFurniture.Keys)
            if (!touched.Contains(id)) toRemove.Add(id);

        foreach (ulong id in toRemove)
            CurrentFurniture.Remove(id, out _);
    }

    public void Dispose()
    {
        Service.Framework.Update -= OnUpdate;
        Scene.OnZoneLoaded -= OnZoneLoaded;
        CurrentFurniture.Clear();
    }
}
