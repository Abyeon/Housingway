using System;
using Dalamud.Bindings.ImGui;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using Housingway.Structs;
using Housingway.Utils;
using Pictomancy;

namespace Housingway.Tweaks;

public partial class DisplayPopRange : ConfigurableTweak<DisplayPopRangeConfig>
{
    public override string Name { get; init; } = "Display Pop Range";
    public override string Description { get; init; } = "Overlay's the area in which you may spawn in.";
    public override bool Enabled { get; set; }

    public DisplayPopRange(Plugin plugin)
    {
        PluginConfig = plugin.Configuration;
        Config = PluginConfig.Tweaks.DisplayPopRange;
    }
    
    public override void Enable()
    {
        Plugin.Overlay.OnDraw += OnOverlay;
    }

    private bool ShouldDraw()
    {
        return Config.Display switch
        {
            DisplayLocation.Outside => HousingService.IsOutside,
            DisplayLocation.Inside => HousingService.IsInside,
            DisplayLocation.Both => HousingService.InHousingArea,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
    
    private unsafe void OnOverlay(PctDrawList drawList)
    {
        if (!ShouldDraw()) return;
        
        var world = LayoutWorld.Instance();
        if (world == null) return;
        var active = world->ActiveLayout;
        if (active == null) return;

        var color = ImGui.ColorConvertFloat4ToU32(Config.Color);

        foreach (var (_, layerPtr) in active->Layers)
        {
            var layer = layerPtr.Value;
            if (layer == null) continue;
            foreach (var (_, instancePtr) in layer->Instances)
            {
                var instance = instancePtr.Value;
                if (instance == null) continue;
                if (instance->Id.Type != InstanceType.PopRange) continue;

                var range = (PopRangeLayoutInstance*)instance;
                var translation = *instance->GetTranslationImpl();
                foreach (var relPos in range->RelativePositions)
                {
                    drawList.AddDot(translation + relPos, Config.Size, color);
                }
            }
        }
    }

    public override void Disable()
    {
        Plugin.Overlay.OnDraw -= OnOverlay;
    }

    public override void Dispose() { }
}
