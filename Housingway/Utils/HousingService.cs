using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Group;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Layer;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using Housingway.Structs;

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
    
    public static bool IsInside  => Manager != null && Manager->IsInside();
    public static bool IsOutside => Manager != null && Manager->IsOutside();
    
    public delegate void FurnitureAdded(Furniture furniture);
    public delegate void FurnitureUpdate(Furniture furniture);
    
    public static event FurnitureAdded? OnFurnitureAdded;
    public static event FurnitureUpdate? OnFurnitureUpdate;

    internal static HashSet<Furniture> CurrentFurniture = [];

    public HousingService()
    {
        Plugin.ClientState.ZoneInit += OnZoneInit;
        
        CheckForHousing();
        UpdateFurniture();
    }

    private void OnZoneInit(ZoneInitEventArgs obj)
    {
        CurrentFurniture.Clear();
        CheckForHousing();
    }

    private void CheckForHousing()
    {
        if (Manager != null)
        {
            Plugin.Framework.Update += OnUpdate;
        }
        else
        {
            Plugin.Framework.Update -= OnUpdate;
        }
    }

    private void OnUpdate(IFramework framework) => UpdateFurniture();

    private void UpdateFurniture()
    {
        if (Manager == null)
        {
            CurrentFurniture.Clear();
            return;
        }

        if (!IsInside && !IsOutside) return;
        if (FurnitureManager == null) return;
        
        HashSet<Furniture> touched = [];

        foreach (var furn in FurnitureManager->FurnitureVector)
        {
            var ptr = furn.Value;
            if (ptr == null) continue;

            var furniture = new Furniture(ptr);
            if (furniture.Id == 0) continue;

            OnFurnitureUpdate?.Invoke(furniture);
            
            touched.Add(furniture);
            if (CurrentFurniture.Add(furniture))
            {
                OnFurnitureAdded?.Invoke(furniture);
            }
        }
        
        CurrentFurniture.IntersectWith(touched);
    }

    public void Dispose()
    {
        Plugin.Framework.Update -= OnUpdate;
        Plugin.ClientState.ZoneInit -= OnZoneInit;
        CurrentFurniture.Clear();
        GC.SuppressFinalize(this);
    }
}

public unsafe class Furniture : IEquatable<Furniture>
{
    public readonly HousingFurniture HousingFurniture;
    public readonly ulong Id;

    public Furniture(HousingFurniture* ptr)
    {
        HousingFurniture = *ptr;
        Id = Object == null ? 0 : Object->GetGameObjectId().Id;
    }

    public HousingObject* Object
    {
        get
        {
            if (HousingService.FurnitureManager == null) return null;
            var arr = HousingService.FurnitureManager->ObjectManager.ObjectArray;
            var index = HousingFurniture.Index;
            if (index < 0) return null;
            if (index >= arr.Objects.Length || index >= arr.ObjectCount) return null;
            return (HousingObject*)arr.Objects[HousingFurniture.Index].Value;
        }
    }

    public SharedGroupLayoutInstance* Group => Object == null ? null : Object->SharedGroupLayoutInstance;

    public Collider* Collider
    {
        get
        {
            if (Group == null) return null;
            foreach (var instance in Group->Instances.Instances)
            {
                var ptr = instance.Value;
                if (ptr == null) continue;

                if (ptr->Instance->GetCollider() == null) continue;
                
                return ptr->Instance->GetCollider();
            }
        
            return null;
        }
    }

    public BgObject* Graphics
    {
        get
        {
            if (Group == null) return null;
            if (Group->Instances.Instances.Count == 0) return null;
            
            var parts = (BgPartsLayoutInstance*)Group->Instances.Instances[0].Value->Instance;
            return parts == null ? null : parts->GraphicsObject;
        }
    }

    public CullObject* Cull
    {
        get
        {
            if (!HousingService.IsInside) return null;
            
            var man = AreaCullingManager.Instance();
            if (man == null) return null;

            return &man->CullObjects[HousingFurniture.Index];
        }
    }

    public bool IsValid => Graphics != null && Collider != null;

    public bool Equals(Furniture? other)
    {
        if (other is null) return false;
        if (other.Object == null) return false;
        return Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((Furniture)obj);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}
