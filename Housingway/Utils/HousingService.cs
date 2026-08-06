using System;
using System.Collections.Generic;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
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
    internal delegate void FurnitureUpdate(Furniture furniture);
    internal delegate void EnterHousingArea(bool indoors);
    
    internal static event FurnitureAdded? OnFurnitureAdded;
    internal static event FurnitureUpdate? OnFurnitureUpdate;
    internal static event EnterHousingArea? OnEnterHousingArea;

    internal static HashSet<Furniture> CurrentFurniture = [];

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
            Service.Framework.Update += OnUpdate;
            UpdateFurniture();
            
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

    private readonly HashSet<Furniture> touched = [];

    private void UpdateFurniture()
    {
        touched.Clear();
        if (FurnitureManager == null) return;

        foreach (var furn in FurnitureManager->FurnitureVector)
        {
            var ptr = furn.Value;
            if (ptr == null) continue;

            var furniture = new Furniture(ptr);
            if (furniture.Id == 0) continue;

            bool exists = CurrentFurniture.Contains(furniture);
            
            if (!exists && !furniture.IsValid) continue;

            touched.Add(furniture);
            
            if (!exists)
            {
                CurrentFurniture.Add(furniture);
                OnFurnitureAdded?.Invoke(furniture);
            }
            
            OnFurnitureUpdate?.Invoke(furniture);
        }

        CurrentFurniture.RemoveWhere(x => !touched.Contains(x));
    }

    public void Dispose()
    {
        Service.Framework.Update -= OnUpdate;
        Scene.OnZoneLoaded -= OnZoneLoaded;
        CurrentFurniture.Clear();
        GC.SuppressFinalize(this);
    }
}
