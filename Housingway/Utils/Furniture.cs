using System;
using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Group;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using FFXIVClientStructs.FFXIV.Common.Math;
using Housingway.Structs;

namespace Housingway.Utils;

public readonly unsafe struct Furniture : IEquatable<Furniture>
{
    public readonly HousingFurniture HousingFurniture;
    public readonly ulong Id;

    public Furniture(HousingFurniture* ptr)
    {
        HousingFurniture = *ptr;

        if (!HousingService.InHousingArea)
        {
            Id = 0;
            return;
        }

        var arr = HousingService.FurnitureManager->ObjectManager.ObjectArray;
        var index = HousingFurniture.Index;
        if (index >= 0 && index < arr.Objects.Length && index < arr.ObjectCount)
        {
            var obj = (HousingObject*)arr.Objects[index].Value;
            Id = obj == null ? 0 : Object->GetGameObjectId().Id;
        }
        else
        {
            Id = 0;
        }
    }

    public HousingObject* Object
    {
        get
        {
            if (!HousingService.InHousingArea) return null;
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

            Collider* foundCollider = null;
            foreach (var instance in Group->Instances.Instances)
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
    }

    public List<IntPtr> AllGraphics
    {
        get
        {
            if (Group == null || Group->Instances.Instances.Count == 0) return [];

            List<IntPtr> graphics = [];

            foreach (var child in Group->Instances.Instances)
            {
                var ptr = child.Value;
                if (ptr == null) continue;

                var instance = ptr->Instance;
                if (instance == null) continue;

                if (instance->Id.Type != InstanceType.BgPart) continue;

                if (instance->GetGraphics() != null)
                {
                    graphics.Add((IntPtr)instance->GetGraphics());
                }
            }

            return graphics;
        }
    }

    public BgObject* Graphics
    {
        get
        {
            if (Group == null) return null;
            if (Group->Instances.Instances.Count == 0) return null;

            var all = AllGraphics;

            if (all.Count == 0) return null;

            foreach (var obj in all)
            {
                var graphics = (BgObject*)obj;
                if (graphics == null || graphics->LoadState == 7) continue;
                return graphics;
            }

            return null;
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

    public SphereCastRange* SphereCastRange
    {
        get
        {
            if (Group == null) return null;
            
            foreach (var child in Group->Instances.Instances)
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


    public Lumina.Excel.Sheets.HousingFurniture? Sheet
    {
        get
        {
            var mask = Object->HousingObjectId.Type == HousingObjectType.Furniture ? 0x20000u : 0x30000u;
            var row = mask | HousingFurniture.Id;
            var sheet = Plugin.DataManager.Excel.GetSheet<Lumina.Excel.Sheets.HousingFurniture>();
            return sheet.GetRowOrDefault(row);
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
        if (Sheet?.HousingItemCategory == 15)
        {
            return GetTargetMarkerOffset();
        }
        
        if (Graphics is null) return 0;
        
        // Calculate via AABB
        var aabb = new AxisAlignedBounds();
        Graphics->ComputeAxisAlignedBounds(&aabb);
            
        var size = aabb.Max - aabb.Min;
        var min = MathF.Min(MathF.Abs(size.X), MathF.Abs(size.Z));
        return min / 2;
    }

    private float GetTargetMarkerOffset()
    {
        if (Group == null || Group->Instances.Instances.Count == 0) return 0;

        var max = float.MinValue;
        var found = false;

        var pos = Object->Position;
        
        foreach (var child in Group->Instances.Instances)
        {
            var ptr = child.Value;
            if (ptr == null) continue;
                
            var instance = ptr->Instance;
            if (instance->Id.Type == InstanceType.TargetMarker)
            {
                //if (1 != index++) continue;
                found = true;

                var transform = *instance->GetTransformImpl();
                var distance = Vector2.Distance(
                    new Vector2(pos.X, pos.Z),
                    new Vector2(transform.Translation.X, transform.Translation.Z));
                
                if (distance > max)
                {
                    max = distance;
                }
            }
        }

        if (!found) return 0;
        return MathF.Abs(MathF.Round(max, 2)) * 0.5f;
    }

    public bool IsValid => AllGraphics.Count != 0 && Collider != null;

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
