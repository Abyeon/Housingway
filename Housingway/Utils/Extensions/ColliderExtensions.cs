using System;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using FFXIVClientStructs.Interop;

namespace Housingway.Utils.Extensions;

[Flags]
public enum MaterialFlag : ushort
{
    None = 0,
    Unk4 = 1 << 4,
    Unwalkable = 1 << 5,
    Swimmable = 1 << 6,
    Submergible = 1 << 7,
    Unk8 = 1 << 8,
    Unk9 = 1 << 9,
    Unk10 = 1 << 10,
    Unk11 = 1 << 11,
    CameraCollision = 1 << 12,
    PlayerCollision = 1 << 13,
    CursorCollision = 1 << 14,
    Unk15 = 1 << 15,
}

[Flags]
public enum MaterialType : sbyte
{
    None = 0,
    Dirt = 0x01, // "dart" in the scd path
    Grass = 0x02,
    Sand = 0x03,
    Stone = 0x04,
    Wood = 0x05,
    Metal = 0x06,
    Gravel = 0x07,
    Leaf = 0x08,
    Powder = 0x09, //tentatively named until can be confirmed
    Carpet = 0x0A,
    Snow = 0x0B,
    Space = 0x0C, //tentatively named
    Water = 0x0D,
    Mesh = 0x0E,
    Sticky = 0x0F
}

public static unsafe class ColliderExtensions
{
    extension (Pointer<Collider> collider)
    {
        private ulong GetAllMaterialValue()
        {
            if (collider.IsNull) return 0;
            
            ulong value = collider.Value->ObjectMaterialValue;

            if (collider.Value->GetColliderType() == ColliderType.Mesh)
            {
                var cast = (ColliderMesh*)collider.Value;
                if (!cast->MeshIsSimple && cast->Mesh != null)
                {
                    var mesh = (MeshPCB*)cast->Mesh;
                    value |= GetNodeMaterials(mesh->RootNode);
                }
            }
        
            return value;
        
            ulong GetNodeMaterials(MeshPCB.FileNode* mesh)
            {
                if (mesh == null) return 0;

                ulong nodeMat = 0;
                foreach (var prim in mesh->Primitives)
                    nodeMat |= prim.Material;

                nodeMat |= GetNodeMaterials(mesh->Child1);
                nodeMat |= GetNodeMaterials(mesh->Child2);
                return nodeMat;
            }
        }
        
        public MaterialFlag GetMaterialFlag()
        {
            if (collider.IsNull) return MaterialFlag.None;

            ulong value = collider.GetAllMaterialValue();
            return (MaterialFlag)((value & 0x0000FFF0));
        }

        public MaterialFlag GetMaterialMask()
        {
            if (collider.IsNull) return MaterialFlag.None;
        
            return (MaterialFlag)((collider.Value->ObjectMaterialMask & 0x0000FFFF));
        }

        public void SetMaterialMask(MaterialFlag mask, bool enabled = true)
        {
            if (collider.IsNull) return;

            if (enabled)
            {
                collider.Value->ObjectMaterialMask |= (ulong)mask;
            }
            else
            {
                collider.Value->ObjectMaterialMask &= ~(ulong)mask;
            }
        }
    
        public MaterialType GetMaterialType()
        {
            ulong value = collider.GetAllMaterialValue();
            return (MaterialType)(value & 0x7F);
        }
    }
}