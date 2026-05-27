using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Pictomancy;

namespace Housingway.Interface.Windows;

public class Overlay : Window
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
