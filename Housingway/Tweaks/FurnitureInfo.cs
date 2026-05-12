using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Housingway.Utils;

namespace Housingway.Tweaks;

public unsafe partial class FurnitureInfo : ConfigurableTweak<FurnitureInfoConfig>
{
    public override string Name { get; init; } = "Furniture Info";

    public override string Description { get; init; } = "Less of a tweak, more of a tool for learning about different furniture.";
    public override bool Enabled { get; set; }

    public FurnitureInfo(Plugin plugin)
    {
        PluginConfig = plugin.Configuration;
        Config = PluginConfig.Tweaks.FurnitureInfo;
    }
    
    public override void Enable() { }

    public override void Disable()
    {
        foreach (var furn in HousingService.CurrentFurniture)
        {
            if (!furn.IsValid) continue;
            furn.Object->Highlight(ObjectHighlightColor.None);
        }

        selectedFurniture = null;
    }

    public override void Dispose() { }
}
