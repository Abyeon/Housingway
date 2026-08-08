using System;
using System.Collections.Generic;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.Interop;
using Housingway.Profiles;

namespace Housingway.Utils;

public unsafe class HousingService : IDisposable
{
    public static HousingManager*   Manager  => HousingManager.Instance();
    public static HousingFurnitureManager* FurnitureManager => Manager->GetFurnitureManager();

    internal static bool IsInside;
    internal static bool IsOutside;
    internal static bool InHousingArea => IsInside || IsOutside;

    internal static Address? CurrentAddress;
    
    internal delegate void FurnitureAdded(Furniture furniture);
    internal delegate void EnterHousingArea(bool indoors);
    
    internal static event FurnitureAdded? OnFurnitureAdded;
    internal static event EnterHousingArea? OnEnterHousingArea;

    internal static Dictionary<ulong, Furniture> CurrentFurniture = [];
    
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
        IsInside = Manager != null && Manager->IsInside();
        IsOutside = Manager != null && Manager->IsOutside();
        
        if (InHousingArea)
        {
            UpdateFurniture();
            Service.Framework.Update += OnUpdate;
            
            if (Address.TryGetAddress(out var address))
            {
                CurrentAddress = address;
            }
            else
            {
                CurrentAddress = null;
            }
            
            OnEnterHousingArea?.Invoke(IsInside);
        }
        else
        {
            CurrentAddress = null;
            Service.Framework.Update -= OnUpdate;
        }
    }

    private void OnUpdate(IFramework framework) => UpdateFurniture();
    
    private void UpdateFurniture()
    {
        touched.Clear();
        if (FurnitureManager == null) return;
        
        var arr = FurnitureManager->ObjectManager.ObjectArray;

        foreach (Pointer<HousingFurniture> furn in FurnitureManager->FurnitureVector)
        {
            if (furn.IsNull) continue;
            
            int index = furn.Value->Index;
            ulong id = 0;
            
            if (index >= 0 && index < arr.Objects.Length && index < arr.ObjectCount)
            {
                var obj = (HousingObject*)arr.Objects[index].Value;
                if (obj != null) id = obj->GetGameObjectId().Id;
            }

            if (id == 0) continue;

            touched.Add(id);
            if (CurrentFurniture.ContainsKey(id)) continue;

            Furniture furniture = new Furniture(furn);
            
            if (!furniture.IsValid) continue;
            
            CurrentFurniture[id] = furniture;
            OnFurnitureAdded?.Invoke(furniture);
        }

        if (CurrentFurniture.Count == touched.Count) return;
        
        toRemove.Clear();
        foreach (ulong id in CurrentFurniture.Keys)
            if (!touched.Contains(id)) toRemove.Add(id);
            
        foreach (ulong id in toRemove)
            CurrentFurniture.Remove(id);
    }

    public void Dispose()
    {
        Service.Framework.Update -= OnUpdate;
        Scene.OnZoneLoaded -= OnZoneLoaded;
        CurrentFurniture.Clear();
    }
}
