using System;
using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Group;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Node;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using FFXIVClientStructs.FFXIV.Common.Math;
using FFXIVClientStructs.Interop;
using Housingway.Structs;
using Lumina.Excel.Sheets;
using HousingFurnitureObj = FFXIVClientStructs.FFXIV.Client.Game.HousingFurniture;

namespace Housingway.Utils;

public readonly unsafe struct Furniture : IEquatable<Furniture>
{
    public readonly ulong Id;
    public readonly HousingObjectId HousingObjectId;
    
    public readonly Pointer<HousingFurnitureObj> HousingFurniture;
    public readonly Pointer<HousingObject> Object;
    public readonly Pointer<SharedGroupLayoutInstance> Group;
    public readonly Pointer<Collider> Collider;
    public readonly Pointer<BgObject> Graphics;
    
    public readonly List<Pointer<BgObject>> AllGraphics = [];
    
    public readonly HousingFurniture? FurnitureSheet;
    public readonly HousingYardObject? YardSheet;

    public Furniture(Pointer<HousingFurnitureObj> ptr)
    {
        HousingFurniture = ptr;

        if (!HousingService.InHousingArea)
        {
            Id = 0;
            return;
        }

        var arr = HousingService.FurnitureManager->ObjectManager.ObjectArray;
        int index = ptr.Value->Index;
        if (index >= 0 && index < arr.Objects.Length && index < arr.ObjectCount)
        {
            var obj = (HousingObject*)arr.Objects[index].Value;
            Id = obj == null ? 0 : obj->GetGameObjectId().Id;
            HousingObjectId = obj->HousingObjectId;
        }
        else
        {
            Id = 0;
        }

        Object = GetObject();

        if (Object.IsNull) return;

        switch (HousingObjectId.Type)
        {
            case HousingObjectType.YardObject:
            {
                var sheet = Service.DataManager.Excel.GetSheet<HousingYardObject>();
                YardSheet = sheet.GetRowOrDefault(Object.Value->HousingObjectId.Id);
                break;
            }
            case HousingObjectType.Furniture:
            {
                var sheet = Service.DataManager.Excel.GetSheet<HousingFurniture>();
                FurnitureSheet = sheet.GetRowOrDefault(Object.Value->HousingObjectId.Id);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException($"New, mysterious Housing Object Type found! {HousingObjectId.Type}");
        }

        Group = Object.Value->SharedGroupLayoutInstance;

        if (Group.IsNull) return;
        Collider = GetCollider();
        AllGraphics = GetAllGraphics();
        Graphics = GetGraphics();
    }

    public void SetTransparency(float transparency)
    {
        foreach (Pointer<BgObject> obj in AllGraphics)
        {
            obj.Value->SetTransparency(transparency);
        }
    }

    public void Highlight(ObjectHighlightColor color)
    {
        foreach (Pointer<BgObject> obj in AllGraphics)
        {
            obj.Value->OutlineColor = color;
        }
    }

    private HousingObject* GetObject()
    {
        var arr = HousingService.FurnitureManager->ObjectManager.ObjectArray;
        int index = HousingFurniture.Value->Index;
        if (index < 0) return null;
        if (index >= arr.Objects.Length || index >= arr.ObjectCount) return null;
        return (HousingObject*)arr.Objects[HousingFurniture.Value->Index].Value;
    }

    private Collider* GetCollider()
    {
        if (Group.IsNull) return null;

        Collider* foundCollider = null;
        foreach (Pointer<ChildNodeInstance> instance in Group.Value->Instances.Instances)
        {
            var ptr = instance.Value;
            if (ptr == null) continue;

            if (ptr->Instance->GetCollider() == null) continue;

            var coll = ptr->Instance->GetCollider();

            // Prefer mesh collision
            if (coll->GetColliderType() == ColliderType.Mesh) return coll;
            foundCollider = coll;
        }

        return foundCollider;
    }

    private List<Pointer<BgObject>> GetAllGraphics()
    {
        if (Group.IsNull || Group.Value->Instances.Instances.Count == 0) return [];

        List<Pointer<BgObject>> graphics = [];

        foreach (Pointer<ChildNodeInstance> child in Group.Value->Instances.Instances)
        {
            var ptr = child.Value;
            if (ptr == null) continue;

            var instance = ptr->Instance;
            if (instance == null) continue;

            if (instance->Id.Type != InstanceType.BgPart) continue;
            var obj = (BgObject*)instance->GetGraphics();

            if (obj != null || obj->LoadState == 7)
            {
                graphics.Add((BgObject*)instance->GetGraphics());
            }
        }

        return graphics;
    }

    private Pointer<BgObject> GetGraphics()
    {
        var all = AllGraphics;

        if (all.Count == 0) return null;

        foreach (Pointer<BgObject> obj in all)
        {
            if (obj.IsNull || obj.Value->LoadState == 7) continue;
            return obj;
        }

        return null;
    }

    private SphereCastRange* SphereCastRange
    {
        get
        {
            if (Group.IsNull) return null;
            
            foreach (Pointer<ChildNodeInstance> child in Group.Value->Instances.Instances)
            {
                var ptr = child.Value;
                if (ptr == null) continue;

                var instance = ptr->Instance;
                if (instance == null) continue;

                var type = instance->Id.Type;
                if (type == InstanceType.SphereCastRange)
                {
                    return (SphereCastRange*)instance;
                }
            }

            return null;
        }
    }
    
    public float GetSnapDistance()
    {
        // Object has override
        if (SphereCastRange is not null)
        {
            var dist = SphereCastRange->Cast;
            return dist.X;
        }

        // Object is a wall item
        if (FurnitureSheet?.HousingItemCategory == 15)
        {
            return GetTargetMarkerOffset();
        }
        
        if (Graphics.IsNull) return 0;
        
        // Calculate via AABB
        var aabb = new AxisAlignedBounds();
        Graphics.Value->ComputeAxisAlignedBounds(&aabb);
            
        var size = aabb.Max - aabb.Min;
        float min = MathF.Min(MathF.Abs(size.X), MathF.Abs(size.Z));
        return min / 2;
    }

    private float GetTargetMarkerOffset()
    {
        if (Group.IsNull || Group.Value->Instances.Instances.Count == 0) return 0;

        float max = float.MinValue;
        bool found = false;

        var pos = Object.Value->Position;
        
        foreach (Pointer<ChildNodeInstance> child in Group.Value->Instances.Instances)
        {
            var ptr = child.Value;
            if (ptr == null) continue;
                
            var instance = ptr->Instance;
            if (instance->Id.Type != InstanceType.TargetMarker) continue;
            
            found = true;

            var transform = *instance->GetTransformImpl();
            float distance = Vector2.Distance(
                new Vector2(pos.X, pos.Z),
                new Vector2(transform.Translation.X, transform.Translation.Z));
                
            if (distance > max)
            {
                max = distance;
            }
        }

        if (!found) return 0;
        return MathF.Abs(MathF.Round(max, 2)) * 0.5f;
    }


    public bool IsValid => !HousingFurniture.IsNull && !Object.IsNull && !Group.IsNull && !Graphics.IsNull;

    public bool Equals(Furniture other) => Id == other.Id;
    public override bool Equals(object? obj) => obj is Furniture other && Equals(other);
    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Furniture left, Furniture right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Furniture left, Furniture right)
    {
        return !(left == right);
    }
}
