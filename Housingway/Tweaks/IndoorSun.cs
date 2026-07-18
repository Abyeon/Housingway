using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using Housingway.Utils;

namespace Housingway.Tweaks;

public class IndoorSun : BaseTweak
{
    public override string Name { get; init; } = "Indoor Sun";
    public override string Description { get; init; } = "Enables the Sun when indoors!";
    public override void Enable()
    {
        HousingService.OnEnterHousingArea += OnEnterHousingArea;

        if (HousingService.IsInside)
        {
            ToggleSun();
        }
    }

    private void OnEnterHousingArea(bool indoors)
    {
        if (!indoors) return;
        ToggleSun();
    }

    private static unsafe void ToggleSun(bool indoor = false)
    {
        var graphics = GraphicsConfig.Instance();
        graphics->IsIndoor = indoor;
    }

    public override void Disable()
    {
        HousingService.OnEnterHousingArea -= OnEnterHousingArea;
        
        if (HousingService.IsInside)
        {
            ToggleSun(true);
        }
    }

    public override void Dispose() { }
}
