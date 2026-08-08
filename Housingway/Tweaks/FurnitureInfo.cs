using System.Threading.Tasks;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Housingway.Tweaks.Base;
using Housingway.Utils;

namespace Housingway.Tweaks;

public partial class FurnitureInfo : ConfigurableTweak<FurnitureInfoConfig>
{
    public override string Name { get; init; } = "Furniture Info";
    public override string Author { get; init; } = "Abyeon";
    public override string Description { get; init; } = "Less of a tweak, more of a tool for learning about different furniture.";
    
    public FurnitureInfo()
    {
        Flags |= ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
    }

    public override Task Enable()
    {
        Service.ClientState.ZoneInit += OnZoneInit;
        
        return Task.CompletedTask;
    }

    private void OnZoneInit(ZoneInitEventArgs obj)
    {
        selectedFurniture = null;
    }

    public override async Task Disable()
    {
        Service.ClientState.ZoneInit -= OnZoneInit;

        await Service.Framework.Run(() =>
        {
            unsafe
            {
                foreach (var furn in HousingService.CurrentFurniture)
                {
                    if (!furn.IsValid) continue;
                    furn.Object.Value->Highlight(ObjectHighlightColor.None);
                }
            }
        });
        
        selectedFurniture = null;
    }
}
