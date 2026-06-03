using System;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using Housingway.Utils;
using Pictomancy;

namespace Housingway.Interface.Windows;

public class Overlay : Window, IDisposable
{
    internal delegate void OverlayDraw(PctDrawList drawList);
    internal event OverlayDraw? OnDraw;
    
    public Overlay() : base("###HousingwayOverlay")
    {
        Flags = ImGuiWindowFlags.NoResize
                | ImGuiWindowFlags.NoCollapse
                | ImGuiWindowFlags.NoBackground
                | ImGuiWindowFlags.NoDocking
                | ImGuiWindowFlags.NoNavFocus
                | ImGuiWindowFlags.NoTitleBar
                | ImGuiWindowFlags.NoInputs
                | ImGuiWindowFlags.NoBringToFrontOnFocus
                | ImGuiWindowFlags.NoFocusOnAppearing;
        
        Scene.OnZoneLoaded += OnZoneLoaded;
    }

    private void OnZoneLoaded()
    {
        IsOpen = HousingService.InHousingArea;
    }

    public void Dispose()
    {
        Scene.OnZoneLoaded -= OnZoneLoaded;
    }

    public override void Draw()
    {
        if (OnDraw is null) return;
        
        using var drawList = PctService.Draw(ImGui.GetBackgroundDrawList(), new PctDrawHints
        {
            UIMask = UIMask.BackbufferAlpha,
            DrawWhenFaded = true,
            DrawInCutscene = true,
            DefaultParams = new PctDxParams
            {
                OccludedAlpha = 0.5f,
                OcclusionTolerance = 0.05f,
            }
        });
        
        if (drawList == null) return;
        
        OnDraw.Invoke(drawList);
    }
}
