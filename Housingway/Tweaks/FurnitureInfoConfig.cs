using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Housingway.Interface;
using Housingway.Utils;
using Lumina.Data.Parsing;
using Pictomancy;

using Stains = Lumina.Excel.Sheets.Stain;

namespace Housingway.Tweaks;

public class FurnitureInfoConfig
{
    
}

public unsafe partial class FurnitureInfo
{
    private Furniture? selectedFurniture;
    private const int InfoHeight = 200;
    
    public override void DrawConfig()
    {
        var height = selectedFurniture == null
                         ? ImGui.GetContentRegionAvail().Y
                         : ImGui.GetContentRegionAvail().Y - InfoHeight;
        
        using (var child = ImRaii.Child($"Furniture List", new Vector2(ImGui.GetContentRegionAvail().X, height)))
        {
            if (!child.Success) return;
            DrawList();
        }

        if (selectedFurniture is not { IsValid: true }) return;

        using var _ = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(5f, 5f));
        using var color = ImRaii.PushColor(ImGuiCol.ChildBg, ImGuiColors.DalamudWhite with { W = 0.05f });
        using var rounding = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 5f);
        using (var infoChild = ImRaii.Child($"Furniture Info", new Vector2(ImGui.GetContentRegionAvail().X, InfoHeight - 7), false, ImGuiWindowFlags.AlwaysUseWindowPadding))
        {
            if (!infoChild.Success) return;
            DrawSelected();
        }
    }

    private void DrawList()
    {
        if (Plugin.ObjectTable.LocalPlayer == null) return;
        var playerPos = Plugin.ObjectTable.LocalPlayer.Position;
        
        int id = 0;
        foreach (var furn in HousingService.CurrentFurniture)
        {
            if (!furn.IsValid) continue;

            using var _ = ImRaii.PushId(id++);

            var dist = Vector3.Distance(playerPos, furn.Object->Position);
            if (ImGui.Selectable($"[{dist:F}] {furn.Object->NameString}"))
            {
                if (selectedFurniture is { IsValid: true })
                    furn.Object->Highlight(ObjectHighlightColor.None);
                
                selectedFurniture = furn;
                furn.Object->Highlight(ObjectHighlightColor.Magenta);
            }

            bool hovered = ImGui.IsItemHovered();
            if (hovered)
            {
                DrawLineToGamePos(furn.Object->Position, ImGuiColors.DalamudViolet.ToByteColor().RGBA);
            }

            if (selectedFurniture != null && selectedFurniture.Equals(furn)) continue;
            if (hovered)
            {
                furn.Object->Highlight(ObjectHighlightColor.Green);
            }
            else
            {
                furn.Object->Highlight(ObjectHighlightColor.None);
            }
        }
    }

    private void DrawSelected()
    {
        if (selectedFurniture is not { IsValid: true })
        {
            ImGui.Text("No furniture selected!");
            return;
        }

        var furn = selectedFurniture.Value;
        
        var name = furn.Object->NameString;
        ImGui.InputText("Name", ref name, flags: ImGuiInputTextFlags.ReadOnly);
        
        var path = furn.Group->ResourceHandle->FileName.ToString();
        ImGui.InputText("Path", ref path, flags: ImGuiInputTextFlags.ReadOnly);
            
        Vector3 pos = furn.Object->Position;
        ImGui.InputFloat3($"Position", ref pos);
        
        Vector4 boundSphere = new Vector4();
        furn.Group->GetBoundingSphereImpl(&boundSphere);

        ImGui.InputFloat4($"Bounding Sphere", ref boundSphere);
        
        DrawBoundingSphere(boundSphere);

        var stain = furn.Group->StainInfo;
        var chosenIndex = stain->ChosenStainIndex;
        var defaultIndex = stain->DefaultStainIndex;

        var stains = Plugin.DataManager.GetExcelSheet<Stains>();
        if (stains.TryGetRow(chosenIndex, out var chosenStain))
        {
            var chosenColor = UintToVector4(chosenStain.Color);
            ImGui.ColorEdit4($"Chosen Stain [{chosenStain.Name.ToString()}]", ref chosenColor, ImGuiColorEditFlags.NoInputs);
        }

        if (stains.TryGetRow(defaultIndex, out var defaultStain))
        {
            var defaultColor = UintToVector4(defaultStain.Color);
            ImGui.ColorEdit4($"Default Stain [{defaultStain.Name.ToString()}]", ref defaultColor, ImGuiColorEditFlags.NoInputs);
        }

        using var header = ImRaii.Header("Instances", ImGuiTreeNodeFlags.Framed);
        if (!header.Success) return;
            
        foreach (var child in furn.Group->Instances.Instances)
        {
            var ptr = child.Value;
            if (ptr == null) continue;

            var instance = ptr->Instance;
            if (instance == null) continue;

            var type = instance->Id.Type;
            ImGui.Text(type.ToString());

            if (ImGui.IsItemHovered())
            {
                Vector3 childPos = new Vector3();
                instance->GetTranslation(&childPos);
                    
                DrawLineToGamePos(childPos, ImGuiColors.DalamudYellow.ToByteColor().RGBA);
            }
        }
    }
    
    private static Vector4 UintToVector4(uint color)
    {
        return new Vector4(
            ((color >> 16) & 0xFF) / 255.0f,        // Red
            ((color >> 8)  & 0xFF) / 255.0f,        // Green
            (color         & 0xFF) / 255.0f,        // Blue
            ((color >> 24) & 0xFF) / 255.0f      // Alpha
        );
    }

    private void DrawBoundingSphere(Vector4 boundSphere)
    {
        if (Plugin.ObjectTable.LocalPlayer == null) return;
        
        using var drawList = PctService.Draw(ImGui.GetBackgroundDrawList(), new PctDrawHints
        {
            DrawWhenFaded = true,
            DrawInCutscene = true,
            DefaultParams = new PctDxParams
            {
                OccludedAlpha = 0,
                OcclusionTolerance = 0
            }
        });
        
        if (drawList is null) return;
        
        var pos = new Vector3(boundSphere.X, boundSphere.Y, boundSphere.Z);

        Vector4 fillColor = new(1f, 0.4f, 0.2f, 0.35f);
        DrawSphere(drawList, pos, boundSphere.W + Plugin.ObjectTable.LocalPlayer.HitboxRadius, ImGui.ColorConvertFloat4ToU32(fillColor));
    }
    
    public static void DrawSphere(PctDrawList drawList, Vector3 center, float radius, uint color, int rings = 16, int segments = 16)
    {
        var sinTheta = new float[segments + 1];
        var cosTheta = new float[segments + 1];
        for (var j = 0; j <= segments; j++)
        {
            var theta = 2.0f * (float)Math.PI * j / segments;
            sinTheta[j] = (float)Math.Sin(theta);
            cosTheta[j] = (float)Math.Cos(theta);
        }
        
        var y1 = radius;
        var r1 = 0f;

        for (var i = 0; i < rings; i++)
        {
            var phi2 = (float)Math.PI * (i + 1) / rings;
            var y2 = radius * (float)Math.Cos(phi2);
            var r2 = radius * (float)Math.Sin(phi2);

            for (int j = 0; j < segments; j++)
            {
                var v1 = center + new Vector3(r1 * cosTheta[j], y1, r1 * sinTheta[j]);
                var v2 = center + new Vector3(r1 * cosTheta[j + 1], y1, r1 * sinTheta[j + 1]);
                
                var v3 = center + new Vector3(r2 * cosTheta[j], y2, r2 * sinTheta[j]);
                var v4 = center + new Vector3(r2 * cosTheta[j + 1], y2, r2 * sinTheta[j + 1]);
                
                if (i != 0) 
                {
                    drawList.AddTriangleFilled(v1, v3, v2, color); 
                }
                
                if (i != rings - 1) 
                {
                    drawList.AddTriangleFilled(v2, v3, v4, color);
                }
            }
            
            y1 = y2;
            r1 = r2;
        }
    }

    private void DrawLineToGamePos(Vector3 pos, uint color)
    {
        if (Plugin.GameGui.WorldToScreen(pos, out var screenPos))
        {
            var draw = ImGui.GetForegroundDrawList();
            draw.AddLine(ImGui.GetMousePos(), screenPos, color);
            draw.AddCircleFilled(screenPos, 3f, color);
        }
    }
}
