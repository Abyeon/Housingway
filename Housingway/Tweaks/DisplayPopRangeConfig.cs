using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Dalamud.Bindings.ImGui;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using Housingway.Structs;

namespace Housingway.Tweaks;

public enum DisplayLocation
{
    Outside,
    Inside,
    Both
}

public enum DisplayType
{
    Radius,
    Points
}

public class DisplayPopRangeConfig
{
    public float Size { get; set; } = 5f;
    public Vector4 Color { get; set; } = new(1f, 1f, 1f, 0.75f);
    public DisplayLocation Display { get; set; } = DisplayLocation.Both;
    public DisplayType Type { get; set; } = DisplayType.Points;
}

public partial class DisplayPopRange
{
    public override void DrawConfig()
    {
        var size = Config.Size;
        if (ImGui.DragFloat("Size", ref size, 0.1f, 0.5f, 10f))
        {
            Config.Size = size;
        }

        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            Plugin.Configuration.Save();
        }

        var color = Config.Color;
        if (ImGui.ColorEdit4("Color", ref color))
        {
            Config.Color = color;
        }
        
        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            Plugin.Configuration.Save();
        }
        
        var names = Enum.GetNames<DisplayLocation>();
        var curr = (int)Config.Display;
        
        if (ImGui.Combo("Display Location", ref curr, names, names.Length))
        {
            Config.Display = (DisplayLocation)curr;
            Plugin.Configuration.Save();
        }
        
        var typeNames = Enum.GetNames<DisplayType>();
        var currType = (int)Config.Type;

        if (ImGui.Combo("Display Type", ref currType, typeNames, typeNames.Length))
        {
            Config.Type = (DisplayType)currType;
            Plugin.Configuration.Save();
        }
        
        Debug();
    }

    [Conditional("DEBUG")]
    private static void Debug()
    {
        if (ImGui.Button("Copy PopRanges"))
        {
            var ranges = GetPopRanges();
            var json = JsonSerializer.Serialize(ranges, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true });
            Plugin.Log.Debug(json);
            ImGui.SetClipboardText(json);
        }
    }
}
