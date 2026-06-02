using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace Housingway.Tweaks;

public enum DisplayLocation
{
    Outside,
    Inside,
    Both
}

public class DisplayPopRangeConfig
{
    public float Size { get; set; } = 5f;
    public Vector4 Color { get; set; } = new(1f, 1f, 1f, 0.75f);
    public DisplayLocation Display { get; set; } = DisplayLocation.Both;
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
            PluginConfig.Save();
        }

        var color = Config.Color;
        if (ImGui.ColorEdit4("Color", ref color))
        {
            Config.Color = color;
        }
        
        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            PluginConfig.Save();
        }
        
        var names = Enum.GetNames<DisplayLocation>();
        var curr = (int)Config.Display;
        
        if (ImGui.Combo("Display Location", ref curr, names, names.Length))
        {
            Config.Display = (DisplayLocation)curr;
            PluginConfig.Save();
        }
    }
}
