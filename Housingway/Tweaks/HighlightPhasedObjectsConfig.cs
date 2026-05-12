using System;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Components;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace Housingway.Tweaks;

public class HighlightPhasedObjectsConfig
{
    public ObjectHighlightColor HighlightColor = ObjectHighlightColor.Black;
}

public partial class HighlightPhasedObjects
{
    public override void DrawConfig()
    {
        var names = Enum.GetNames<ObjectHighlightColor>();
        var curr = (int)Config.HighlightColor;

        if (ImGui.Combo("Highlight Color", ref curr, names, names.Length))
        {
            Config.HighlightColor = (ObjectHighlightColor)curr;
            PluginConfig.Save();
        }
        
        ImGuiComponents.HelpMarker($"Black causes a fun graphical glitch that makes seeing phased objects very easy.");
    }
}
