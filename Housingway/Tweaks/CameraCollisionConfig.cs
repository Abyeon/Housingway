using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Housingway.Structs;
using Housingway.Utils;

namespace Housingway.Tweaks;

public class CameraCollisionConfig
{
    
}

public unsafe partial class CameraCollision
{
    private int prevOffset = -1;
    private int offset = 0;
    
    byte color = 0;
    private float alpha = 0;
    
    public override void DrawConfig()
    {
        var man = AreaCullingManager.Instance();
        if (AreaCullingManager.Instance() == null) return;
        
        if (ImGui.Button($"{((IntPtr)AreaCullingManager.Instance()):X8}"))
        {
            ImGui.SetClipboardText($"{((IntPtr)AreaCullingManager.Instance()):X8}");
        }
        
        foreach (var furn in HousingService.CurrentFurniture)
        {
            if (furn.Graphics == null) continue;
            if (furn.Collider == null) continue;
            
            using var dropdown = ImRaii.Header($"{furn.Object->NameString}###{furn.Id}", ImGuiTreeNodeFlags.None);
            if (!dropdown.Success) continue;

            if (ImGui.Button($"Copy GameObject addr"))
            {
                ImGui.SetClipboardText($"{((IntPtr)furn.Object):X8}");
            }
            
            ImGui.Text($"{furn.Graphics->OutlineColor}");
            ImGui.Text($"{furn.Graphics->OutlineFlags}");
            ImGui.InputByte("color", ref color, 1, 1);
            if (ImGui.Button("Set Color"))
            {
                furn.Graphics->OutlineFlags = 131;
                furn.Graphics->ResetFlags();
            }

            if (ImGui.SliderFloat("Alpha", ref alpha, 0.0f, 1.0f))
            {
                furn.Graphics->SetTransparency(alpha);
            }
            
            ImGui.Text($"ID: {furn.Object->ObjectIndex}");

            //List<CullObject> cullObjs = [];
            foreach (var cull in man->CullObjects)
            {
                if ((short)cull.Unk0 == furn.Object->HousingObjectId.Id)
                {
                    ImGui.Text($"CullObj [{cull.Distance}] Unk0 {cull.Unk0} == HousingFurnitureIndex {furn.Object->HousingFurnitureIndex}");
                }
            }
            
            //ImGui.Text($"There are {cullObjs.Count} cull objects");

            Vector4 boundSphere = new Vector4();
            furn.Object->SharedGroupLayoutInstance->GetBoundingSphereImpl(&boundSphere);
        
            Vector3 pos = furn.Object->Position;
            ImGui.DragFloat3($"Position", ref pos);
            
            if (ImGui.DragFloat4($"Bound sphere", ref boundSphere))
            {
                
            }
            
            ImGui.Text($"{furn.Collider->ObjectMaterialMask:B}");
            if (ImGui.InputInt("Bit To Set", ref offset, 1, 1))
            {
                if (prevOffset != -1)
                {
                    SetCollisionBit(furn, prevOffset, true);
                }
                
                SetCollisionBit(furn, offset);
                prevOffset = offset;
            }
        }
    }
    
    private static void SetCollisionBit(Furniture furniture, int offset, bool enabled = false)
    {
        var collider = furniture.Collider;
        
        if (collider == null) return;
            
        if (enabled)
        {
            collider->ObjectMaterialMask &= ~(1UL << offset);
        }
        else
        {
            collider->ObjectMaterialMask |= (1UL << offset);
        }
    }
}
