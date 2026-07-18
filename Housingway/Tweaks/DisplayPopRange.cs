using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json.Serialization;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using Housingway.Structs;
using Housingway.Utils;
using Pictomancy;

namespace Housingway.Tweaks;

public partial class DisplayPopRange : ConfigurableTweak<DisplayPopRangeConfig>
{
    public override string Name { get; init; } = "Display Pop Range";
    public override string Author { get; init; } = "Abyeon";
    public override string Description { get; init; } = "Overlays the area in which you may spawn in.";
    
    private PopRange[] ranges = [];

    public DisplayPopRange()
    {
        Config = Plugin.Configuration.Tweaks.DisplayPopRange;
    }
    
    public override void Enable()
    {
        Plugin.Overlay.OnDraw += OnOverlay;
        Scene.OnZoneLoaded += OnZoneLoaded;
        ranges = GetPopRanges();
    }
    
    private void OnZoneLoaded()
    {
        ranges = GetPopRanges();
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
    
    private void OnOverlay(PctDrawList drawList)
    {
        if (!ShouldDraw()) return;

        var p = new PctDxParams
        {
            ProjectionHeight = 1f,
            OccludedAlpha = 0.1f
        };

        var color = ImGui.ColorConvertFloat4ToU32(Config.Color);
        foreach (var range in ranges)
        {
            switch (Config.Type)
            {
                case DisplayType.Radius:
                    drawList.AddCircleFilled(range.Translation, Math.Max(Config.Size * 0.01f, range.Radius), color, p: p);
                    break;
                case DisplayType.Points:
                    foreach (var pos in range.RelativePositions)
                    {
                        drawList.AddDot(range.Translation + pos, Config.Size, color);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private static unsafe PopRange[] GetPopRanges()
    {
        var world = LayoutWorld.Instance();
        if (world == null) return [];
        var active = world->ActiveLayout;
        if (active == null) return [];

        List<PopRange> ranges = [];
        
        foreach (var (_, layerPtr) in active->Layers)
        {
            var layer = layerPtr.Value;
            if (layer == null) continue;
            foreach (var (_, instancePtr) in layer->Instances)
            {
                var instance = instancePtr.Value;
                if (instance == null) continue;
                if (instance->Id.Type != InstanceType.PopRange) continue;

                var range = new PopRange((PopRangeLayoutInstance*)instance);
                ranges.Add(range);
            }
        }
        
        return ranges.ToArray();
    }

    public override void Disable()
    {
        Plugin.Overlay.OnDraw -= OnOverlay;
        Scene.OnZoneLoaded -= OnZoneLoaded;
    }

    public override void Dispose() { }
}

public readonly unsafe struct PopRange
{
    public Vector3 Translation { get; init; }
    public float Radius { get; init; }
    
    public Vector3[] RelativePositions { get; init; }
    
    public PopRange(PopRangeLayoutInstance* instance)
    {
        Translation = *((ILayoutInstance*)instance)->GetTranslationImpl();
        RelativePositions = instance->RelativePositions.ToArray();
        
        var largestDistance = float.MinValue;
        foreach (var pos in RelativePositions)
        {
            var distSq = pos.LengthSquared();
            if (distSq > largestDistance)
            {
                largestDistance = distSq;
            }
        }
        
        Radius = float.Sqrt(largestDistance);
    }
}
