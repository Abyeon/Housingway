using System;
using Dalamud.Game.ClientState;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

namespace Housingway.Tweaks;

public unsafe class ToggleCastShadows : BaseTweak
{
    public override string Name { get; init; } = "Disable Cast Shadows";
    public override string Author { get; init; } = "Abyeon";
    public override string Description { get; init; } =
        "Disables the shadows casted by different light objects when within housing. " +
        "This may help with performance and some disgusting lighting pop-in.";
    
    public override void Enable()
    {
        SetCastShadows(false);
        Service.ClientState.ZoneInit += OnZoneInit;
    }

    private void OnZoneInit(ZoneInitEventArgs obj) => SetCastShadows(false);

    public override void Disable()
    {
        Service.ClientState.ZoneInit -= OnZoneInit;
        SetCastShadows(true);
    }

    public static void SetCastShadows(bool enabled)
    {
        var config = GraphicsConfig.Instance();
        if (config == null) throw new NullReferenceException("GraphicsConfig.Instance() returned null");
        
        var man = HousingManager.Instance();
        if (man == null) return;

        if (!enabled && man->IsInside())
        {
            config->ShadowLightValidType = 0;
        }
        else
        {
            if (Service.GameConfig.System.TryGetUInt("ShadowLightValidType", out var maxShadows))
            {
                config->ShadowLightValidType = maxShadows switch
                {
                    0 => 8,
                    1 => 14,
                    2 => 20,
                    _ => throw new ArgumentOutOfRangeException($"ShadowLightValidType returned an unexpected value {maxShadows}")
                };
            }
            else
            {
                Service.Log.Error("Could not find ShadowLightValidType.");
            }
        }
    }

    public override void Dispose() { }
}
