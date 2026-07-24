using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Housingway.Profiles;

namespace Housingway.Utils;

public unsafe class HousingService : IDisposable
{
    public static HousingManager*   Manager  => HousingManager.Instance();
    public static IndoorTerritory*  Indoors  => Manager != null ? Manager->IndoorTerritory  : null;
    public static OutdoorTerritory* Outdoors => Manager != null ? Manager->OutdoorTerritory : null;

    public static HousingFurnitureManager* FurnitureManager
    {
        get
        {
            if (IsInside) return &Indoors->FurnitureManager;
            if (IsOutside) return &Outdoors->FurnitureManager;
            return null;
        }
    }

    internal static bool IsInside => Manager != null && Manager->IsInside();
    internal static bool IsOutside => Manager != null && Manager->IsOutside();
    internal static bool InHousingArea => IsInside || IsOutside;

    internal static Address? CurrentAddress = null;
    
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
            
            if (InHousingArea)
                Service.Framework.RunOnFrameworkThread(UpdateFurniture);
        });
    }

    private void OnZoneLoaded()
    {
        CurrentFurniture.Clear();
        CheckForHousing();
    }

    private void CheckForHousing()
    {
        if (InHousingArea)
        {
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

            var exists = CurrentFurniture.Contains(furniture);
            
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
