using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision.Math;
using Housingway.Interface;
using Housingway.Render;
using Housingway.Utils;
using Lumina.Data.Parsing;
using Pictomancy;

using Stains = Lumina.Excel.Sheets.Stain;

namespace Housingway.Tweaks;

public enum DebugView
{
    None = 0,
    Collision,
    PhaseRange
}

public class FurnitureInfoConfig
{
    public DebugView DebugView { get; set; } = DebugView.Collision;
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
        
        using var drawList = PctService.Draw(ImGui.GetBackgroundDrawList(), new PctDrawHints
        {
            UIMask = UIMask.BackbufferAlpha,
            DrawWhenFaded = true,
            DrawInCutscene = true,
            DefaultParams = new PctDxParams
            {
                OccludedAlpha = 0,
                OcclusionTolerance = 0,
                FresnelOpacity = 1f,
                FresnelIntensity = 1f,
                ProjectionHeight = 0.01f
            }
        });

        if (drawList is null) return;
        
        Vector4 fillColor = new(0.4f, 0.1f, 1f, 0.35f);
        uint convertedFillColor = ImGui.ColorConvertFloat4ToU32(fillColor);
        
        int id = 0;
        foreach (var furn in HousingService.CurrentFurniture)
        {
            if (!furn.IsValid) continue;

            using var _ = ImRaii.PushId(id++);
            
            // var boundSphere = new Vector4();
            // furn.Group->GetBoundingSphereImpl(&boundSphere);
            //
            // var pos = new Vector3(boundSphere.X, boundSphere.Y, boundSphere.Z);
            // drawList.AddSphere(pos, boundSphere.W, convertedFillColor);

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

        var collType = furn.Collider->GetColliderType().ToString();
        ImGui.InputText("Collision Type", ref collType, flags: ImGuiInputTextFlags.ReadOnly);

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
        
        var names = Enum.GetNames<DebugView>();
        var curr = (int)Config.DebugView;
        
        if (ImGui.Combo("Debug Drawing", ref curr, names, names.Length))
        {
            Config.DebugView = (DebugView)curr;
            PluginConfig.Save();
        }

        switch (Config.DebugView)
        {
            case DebugView.PhaseRange: 
                DrawBoundingSphere(boundSphere);
                break;
            case DebugView.Collision:
                if (furn.Collider->GetColliderType() == ColliderType.Mesh)
                    DrawCollision(pos, (ColliderMesh*)furn.Collider);
                break;
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

    private static void DrawCollision(Vector3 pos, ColliderMesh* coll)
    {
        using var drawList = PctService.Draw(ImGui.GetBackgroundDrawList(), new PctDrawHints
        {
            UIMask = UIMask.BackbufferAlpha,
            DrawWhenFaded = true,
            DrawInCutscene = true,
            DefaultParams = new PctDxParams
            {
                OccludedAlpha = 0f,
                OcclusionTolerance = 0.05f,
            }
        });
        
        if (drawList is null) return;
        
        if (coll != null && !coll->MeshIsSimple && coll->Mesh != null)
        {
            var mesh = (MeshPCB*)coll->Mesh;
            var node = mesh->RootNode;
            DrawNode(node, ref coll->World);
        }

        return;

        void DrawNode(MeshPCB.FileNode* node, ref Matrix4x3 world)
        {
            while (true)
            {
                if (node == null) return;

                if (node->NumPrims > 0)
                {
                    using var render = new SimpleRender(drawList);
                    render.AddInstance(world);

                    for (var i = 0; i < node->NumVertsRaw + node->NumVertsCompressed; ++i)
                        render.AddVertex(node->Vertex(i));

                    foreach (ref var prim in node->Primitives)
                    {
                        render.AddTriangle(prim.V1, prim.V2, prim.V3);
                    }
                }

                DrawNode(node->Child1, ref world);
                node = node->Child2;
            }
        }
    }

    private void DrawBoundingSphere(Vector4 boundSphere)
    {
        if (Plugin.ObjectTable.LocalPlayer == null) return;
        
        using var drawList = PctService.Draw(ImGui.GetBackgroundDrawList(), new PctDrawHints
        {
            UIMask = UIMask.BackbufferAlpha,
            DrawWhenFaded = true,
            DrawInCutscene = true,
            DefaultParams = new PctDxParams
            {
                OccludedAlpha = 0,
                OcclusionTolerance = 0,
                FresnelOpacity = 1f,
                FresnelIntensity = 1f,
                ProjectionHeight = 0.01f
            }
        });
        
        if (drawList is null) return;
        
        var pos = new Vector3(boundSphere.X, boundSphere.Y, boundSphere.Z);
        var radius = boundSphere.W + Plugin.ObjectTable.LocalPlayer.HitboxRadius;

        Vector4 fillColor = new(0.4f, 0.1f, 1f, 0.35f);
        drawList.AddSphere(pos, radius, ImGui.ColorConvertFloat4ToU32(fillColor));
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
