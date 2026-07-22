using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision.Math;
using Housingway.Render;
using Housingway.Structs;
using Housingway.Utils;
using Pictomancy;

using Stains = Lumina.Excel.Sheets.Stain;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace Housingway.Tweaks;

public enum DebugView
{
    None = 0,
    Collision = 1,
    PhaseRange = 2,
    SnapRange = 3
}

public class FurnitureInfoConfig
{
    public DebugView DebugView { get; set; } = DebugView.Collision;
}

public unsafe partial class FurnitureInfo
{
    private Furniture? selectedFurniture;
    private const int InfoHeight = 200;

    private string search = string.Empty;

    private IEnumerable<Furniture> FilteredFurniture => HousingService.CurrentFurniture.Where(x => x.Object is not null && x.Object->NameString.Contains(search, StringComparison.InvariantCultureIgnoreCase));

    public override void DrawConfig()
    {
        var height = ImGui.GetContentRegionAvail().Y;
        
        if (selectedFurniture != null)
        {
            height -= InfoHeight;
        }
        
        Search();
        
        using (var child = ImRaii.Child($"Furniture List", new Vector2(ImGui.GetContentRegionAvail().X, height)))
        {
            if (!child.Success) return;
            DrawList();
        }

        if (selectedFurniture is not { IsValid: true }) return;

        using var _ = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(5f, 5f));
        using var color = ImRaii.PushColor(ImGuiCol.ChildBg, ImGuiColors.DalamudWhite with { W = 0.05f });
        using var rounding = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 5f);
        using (var infoChild = ImRaii.Child($"Furniture Info", ImGui.GetContentRegionAvail(), false, ImGuiWindowFlags.AlwaysUseWindowPadding))
        {
            if (!infoChild.Success) return;
            DrawSelected();
        }
    }
    
    private void Search()
    {
        using var frame = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, new Vector2(5f, 6f) * ImGuiHelpers.GlobalScale);
        using var round = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 5f);
        using var width = ImRaii.ItemWidth(ImGui.GetContentRegionAvail().X);

        ImGui.InputTextWithHint("###SearchBar", "Search for furniture...", ref search);
    }

    private void DrawList()
    {
        if (Service.ObjectTable.LocalPlayer == null) return;
        var playerPos = Service.ObjectTable.LocalPlayer.Position;
        
        var id = 0;
        foreach (var furn in FilteredFurniture)
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

        var addr = (IntPtr)furn.Group;
        var addrString = addr.ToString("X8");
        ImGui.InputText("Address", ref addrString, flags: ImGuiInputTextFlags.ReadOnly);
        
        var path = furn.Group->ResourceHandle->FileName.ToString();
        ImGui.InputText("Path", ref path, flags: ImGuiInputTextFlags.ReadOnly);
        
        var housingType = furn.Object->HousingObjectId.Type.ToString();
        ImGui.InputText("Type", ref housingType, flags: ImGuiInputTextFlags.ReadOnly);
        
        var collType = furn.Collider->GetColliderType().ToString();
        ImGui.InputText("Collision Type", ref collType, flags: ImGuiInputTextFlags.ReadOnly);

        var id = furn.Object->HousingObjectId.Id.ToString();
        ImGui.InputText("ID", ref id, flags: ImGuiInputTextFlags.ReadOnly);

        var entry = furn.Object->HousingObjectId.EntryId.ToString();
        ImGui.InputText("Entry", ref entry, flags: ImGuiInputTextFlags.ReadOnly);
            
        Vector3 pos = furn.Object->Position;
        ImGui.InputFloat3($"Position", ref pos);
        
        Vector4 boundSphere = new Vector4();
        furn.Group->GetBoundingSphereImpl(&boundSphere);

        ImGui.InputFloat4($"Bounding Sphere", ref boundSphere);

        // var loadState = furn.Graphics->LoadState;
        // ImGui.InputByte("Load State", ref loadState, flags: ImGuiInputTextFlags.ReadOnly);
        
        var stain = furn.Group->StainInfo;
        var chosenIndex = stain->ChosenStainIndex;
        var defaultIndex = stain->DefaultStainIndex;

        var stains = Service.DataManager.GetExcelSheet<Stains>();
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

        ImGui.Text("Properties:");
        var props = ((StainInfoEx*)furn.Group->StainInfo)->Properties;
        for (var i = 0; i < 8; i++)
        {
            var prop = props[i].ToString();
            ImGui.InputText($"x{i}", ref prop, flags: ImGuiInputTextFlags.ReadOnly);
        }
        
        var names = Enum.GetNames<DebugView>();
        var curr = (int)Config.DebugView;
        
        if (ImGui.Combo("Debug Drawing", ref curr, names, names.Length))
        {
            Config.DebugView = (DebugView)curr;
            Plugin.Configuration.Save();
        }

        switch (Config.DebugView)
        {
            case DebugView.Collision:
                if (furn.Collider->GetColliderType() == ColliderType.Mesh)
                    DrawCollision(pos, (ColliderMesh*)furn.Collider);
                break;
            case DebugView.PhaseRange: 
                DrawBoundingSphere(boundSphere);
                break;
            case DebugView.SnapRange:
                DrawSnapRange(pos, furn);
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
                var transform = *instance->GetTransformImpl();
                DrawLineToGamePos(transform.Translation, ImGuiColors.DalamudYellow.ToByteColor().RGBA);
            }

            if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
            {
                ImGui.SetClipboardText($"{((IntPtr)instance):X8}");
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
    
    private static void DrawSnapRange(Vector3 pos, Furniture furn)
    {
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
                FresnelSpread = 0.1f,
                ProjectionHeight = 0.1f,
                FadeStart = 0f,
            }
        });

        if (drawList is null) return;
        
        var radius = furn.GetSnapDistance();
        Service.Log.Debug(radius.ToString(CultureInfo.InvariantCulture));
        
        drawList.AddSphere(pos, radius, 0x0C5CFF5C);
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

    private static void DrawBoundingSphere(Vector4 boundSphere)
    {
        if (Service.ObjectTable.LocalPlayer == null) return;
        
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
        var radius = boundSphere.W + Service.ObjectTable.LocalPlayer.HitboxRadius;

        Vector4 fillColor = new(0.4f, 0.1f, 1f, 0.35f);
        drawList.AddSphere(pos, radius, ImGui.ColorConvertFloat4ToU32(fillColor));
    }

    private void DrawLineToGamePos(Vector3 pos, uint color)
    {
        if (Service.GameGui.WorldToScreen(pos, out var screenPos))
        {
            var draw = ImGui.GetForegroundDrawList();
            draw.AddLine(ImGui.GetMousePos(), screenPos, color);
            draw.AddCircleFilled(screenPos, 3f, color);
        }
    }
}
