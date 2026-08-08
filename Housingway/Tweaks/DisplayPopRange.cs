using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Bindings.ImGui;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using Housingway.Tweaks.Base;
using Housingway.Utils;
using Pictomancy;
using PopRangeLayoutInstance = Housingway.Structs.PopRangeLayoutInstance;

namespace Housingway.Tweaks;

public partial class DisplayPopRange : ConfigurableTweak<DisplayPopRangeConfig>
{
    public override string Name { get; init; } = "Display Pop Range";
    public override string Author { get; init; } = "Abyeon";
    public override string Description { get; init; } = "Overlays the points in which you may spawn in.";
    
    private PopRange[] ranges = [];
    
    public override async Task Enable()
    {
        Plugin.Overlay.OnDraw += OnOverlay;
        HousingService.OnEnterHousingArea += OnEnterHousingArea;
        await Service.Framework.Run(() =>
        {
            ranges = GetPopRanges();
        });
    }

    private void OnEnterHousingArea(bool indoors)
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

        uint color = ImGui.ColorConvertFloat4ToU32(Config.Color);
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
        
        foreach ((ushort _, var layer) in active->Layers)
        {
            if (layer.IsNull) continue;
            foreach ((uint _, var instance) in layer.Value->Instances)
            {
                if (instance.IsNull) continue;
                if (instance.Value->Id.Type != InstanceType.PopRange) continue;

                var range = new PopRange((PopRangeLayoutInstance*)instance.Value);
                ranges.Add(range);
            }
        }
        
        return ranges.ToArray();
    }

    public override Task Disable()
    {
        Plugin.Overlay.OnDraw -= OnOverlay;
        HousingService.OnEnterHousingArea -= OnEnterHousingArea;
        return Task.CompletedTask;
    }
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
        
        float largestDistance = float.MinValue;
        foreach (var pos in RelativePositions)
        {
            float distSq = pos.LengthSquared();
            if (distSq > largestDistance)
            {
                largestDistance = distSq;
            }
        }
        
        Radius = float.Sqrt(largestDistance);
    }
}
