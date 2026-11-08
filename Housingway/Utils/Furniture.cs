using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Group;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Node;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using FFXIVClientStructs.FFXIV.Common.Math;
using FFXIVClientStructs.Interop;
using FFXIVClientStructs.STD;
using Housingway.Structs;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using HousingFurniture = Lumina.Excel.Sheets.HousingFurniture;
using HousingFurnitureObj = FFXIVClientStructs.FFXIV.Client.Game.HousingFurniture;

namespace Housingway.Utils;

public readonly unsafe struct Furniture : IEquatable<Furniture>
{
    public readonly ulong Id;
    public readonly int Index;
    public readonly HousingObjectId HousingObjectId;

    private static ExcelSheet<HousingFurniture>? FurnitureSheet;
    private static ExcelSheet<HousingYardObject>? YardSheet;

    public HousingFurniture? FurnitureRow
    {
        get
        {
            if (HousingObjectId.Type != HousingObjectType.Furniture) return null;
            FurnitureSheet ??= Service.DataManager.Excel.GetSheet<HousingFurniture>();
            return FurnitureSheet.GetRowOrDefault(HousingObjectId.Id);
        }
    }

    public HousingYardObject? YardRow
    {
        get
        {
            if (HousingObjectId.Type != HousingObjectType.YardObject) return null;
            YardSheet ??= Service.DataManager.Excel.GetSheet<HousingYardObject>();
            return YardSheet.GetRowOrDefault(HousingObjectId.Id);
        }
    }
    
    public Pointer<HousingFurnitureObj> HousingFurniture => GetFurniture();
    public Pointer<HousingObject> Object => GetObject();
    public Pointer<SharedGroupLayoutInstance> Group => GetGroup();
    public Pointer<Collider> Collider => GetCollider();
    public Pointer<BgObject> Graphics => GetGraphics();
    public List<Pointer<BgObject>> AllGraphics => GetAllGraphics();

    // public Furniture(Pointer<HousingFurnitureObj> ptr)
    // {
    //     Id = 0;
    //     Index = -1;
    //     HousingObjectId = default;
    //
    //     if (!HousingService.InHousingArea || ptr.IsNull) return;
    //
    //     HousingFurnitureManager* furnitureManager = HousingService.FurnitureManager;
    //     if (furnitureManager is null) return;
    //     
    //     ref HousingObjectManager.HousingObjectArray arr = ref furnitureManager->ObjectManager.ObjectArray;
    //     int index = ptr.Value->Index;
    //     if ((uint)index >= (uint)arr.Objects.Length || index >= arr.ObjectCount) return; // can avoid lower bound check by casting to uint o-o
    //     
    //     var obj = (HousingObject*)arr.Objects[index].Value;
    //     if (obj == null) return;
    //         
    //     ulong id = obj->GetGameObjectId().Id;
    //     if (id == 0) return;
    //             
    //     Id = id;
    //     Index = index;
    //     HousingObjectId = obj->HousingObjectId;
    // }

    public Furniture(ulong id, int index, HousingObjectId housingObjectId)
    {
        Id = id;
        Index = index;
        HousingObjectId = housingObjectId;
    }

    public void SetTransparency(float transparency)
    {
        foreach (Pointer<BgObject> obj in AllGraphics)
        {
            if (obj.IsNull) continue;
            obj.Value->SetTransparency(transparency);
        }
    }

    public void Highlight(ObjectHighlightColor color)
    {
        foreach (Pointer<BgObject> obj in AllGraphics)
        {
            if (obj.IsNull) continue;
            obj.Value->OutlineColor = color;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Pointer<HousingFurnitureObj> GetFurniture()
    {
        if (Id == 0 || Index < 0 || !HousingService.InHousingArea) return null;
        
        HousingFurnitureManager* furnitureManager = HousingService.FurnitureManager;
        if (furnitureManager is null) return null;

        foreach (Pointer<HousingFurnitureObj> furn in furnitureManager->FurnitureVector)
        {
            if (furn.IsNull || furn.Value->Index != Index) continue;
            return furn;
        }

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Pointer<HousingObject> GetObject()
    {
        if (Id == 0 || Index < 0 || !HousingService.InHousingArea) return null;
        
        HousingFurnitureManager* furnitureManager = HousingService.FurnitureManager;
        if (furnitureManager is null) return null;
        
        ref HousingObjectManager.HousingObjectArray arr = ref furnitureManager->ObjectManager.ObjectArray;
        var obj = (HousingObject*)arr.Objects[Index].Value;
        if (obj is null || obj->GetGameObjectId().Id != Id) return null;

        return obj;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private SharedGroupLayoutInstance* GetGroup()
    {
        Pointer<HousingObject> obj = Object;
        return obj.IsNull ? null : obj.Value->SharedGroupLayoutInstance;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Collider* GetCollider()
    {
        Pointer<SharedGroupLayoutInstance> group = Group;
        if (group.IsNull) return null;

        Collider* foundCollider = null;
        foreach (Pointer<ChildNodeInstance> instance in group.Value->Instances.Instances)
        {
            if (instance.IsNull) continue;

            if (instance.Value->Instance->GetCollider() == null) continue;

            var coll = instance.Value->Instance->GetCollider();

            // Prefer mesh collision
            if (coll->GetColliderType() == ColliderType.Mesh) return coll;
            foundCollider = coll;
        }

        return foundCollider;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private List<Pointer<BgObject>> GetAllGraphics()
    {
        Pointer<SharedGroupLayoutInstance> group = Group;
        if (group.IsNull) return [];
        
        ref StdVector<Pointer<ChildNodeInstance>> instances = ref group.Value->Instances.Instances;
        if (instances.Count == 0) return [];

        List<Pointer<BgObject>> graphics = [];

        foreach (Pointer<ChildNodeInstance> child in instances)
        {
            if (child.IsNull) continue;

            ILayoutInstance* instance = child.Value->Instance;
            if (instance == null) continue;

            if (instance->Id.Type != InstanceType.BgPart) continue;
            var obj = (BgObject*)instance->GetGraphics();

            if (obj == null || obj->LoadState != 3) continue;
            graphics.Add(obj);
        }

        return graphics;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Pointer<BgObject> GetGraphics()
    {
        Pointer<SharedGroupLayoutInstance> group = Group;
        if (group.IsNull) return null;
        
        var primary = (BgObject*)group.Value->GetGraphics();
        if (primary != null && primary->LoadState == 3) return primary;
        
        ref StdVector<Pointer<ChildNodeInstance>> instances = ref group.Value->Instances.Instances;
        if (instances.Count == 0) return null;
        
        foreach (Pointer<ChildNodeInstance> child in instances)
        {
            if (child.IsNull) continue;

            ILayoutInstance* instance = child.Value->Instance;
            if (instance == null) continue;

            if (instance->Id.Type != InstanceType.BgPart) continue;
            var obj = (BgObject*)instance->GetGraphics();

            if (obj == null || obj->LoadState != 3) continue;
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
        if (FurnitureRow?.HousingItemCategory == 15)
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


    public bool IsValid => !Graphics.IsNull;

    public bool Equals(Furniture other) => Id == other.Id;
    public override bool Equals(object? obj) => obj is Furniture other && Equals(other);
    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Furniture left, Furniture right) => left.Equals(right);
    public static bool operator !=(Furniture left, Furniture right) => !(left == right);
}
