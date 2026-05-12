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
        using (var child = ImRaii.Child($"Furniture List", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y - InfoHeight)))
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

        var name = selectedFurniture.Object->NameString;
        ImGui.InputText("Name", ref name, flags: ImGuiInputTextFlags.ReadOnly);
        
        var path = selectedFurniture.Group->ResourceHandle->FileName.ToString();
        ImGui.InputText("Path", ref path, flags: ImGuiInputTextFlags.ReadOnly);
            
        Vector3 pos = selectedFurniture.Object->Position;
        ImGui.InputFloat3($"Position", ref pos);
        
        Vector4 boundSphere = new Vector4();
        selectedFurniture.Group->GetBoundingSphereImpl(&boundSphere);

        ImGui.InputFloat4($"Bounding Sphere", ref boundSphere);

        var stain = selectedFurniture.Group->StainInfo;
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

        using var drawList = PctService.Draw();
        if (drawList is null) return;

        Vector4 fillColor = new(1f, 0.4f, 0.2f, 0.35f);
        drawList?.AddCircleFilled(pos, boundSphere.W, ImGui.ColorConvertFloat4ToU32(fillColor));
    }
    
    private static Vector4 UintToVector4(uint color)
    {
        return new Vector4(
            ((color >> 16) & 0xFF) / 255.0f,    // Red
            ((color >> 8)  & 0xFF) / 255.0f,    // Green
            (color         & 0xFF) / 255.0f,     // Blue
                ((color >> 24) & 0xFF) / 255.0f // Alpha
        );
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
