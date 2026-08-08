using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Housingway.Tweaks.Base;
using Housingway.Utils;
using Housingway.Utils.Extensions;

namespace Housingway.Tweaks;

public partial class HighlightPhasedObjects : ConfigurableTweak<HighlightPhasedObjectsConfig>
{
    public override string Name { get; init; } = "Highlight Phased Objects";
    public override string Author { get; init; } = "Abyeon";
    public override string Description { get; init; } = "Highlights objects that have had their player collision disabled.";
    
    public override Task Enable()
    {
        Service.Framework.Update += OnUpdate;
        HousingService.OnEnterHousingArea += OnEnterHousingArea;
        return Task.CompletedTask;
    }

    private void OnEnterHousingArea(bool indoors)
    {
        highlightedObjects.Clear();
    }

    private void OnUpdate(IFramework framework)
    {
        foreach (Furniture furn in HousingService.CurrentFurniture.Values)
        {
            if (furn.Graphics.IsNull || furn.Collider.IsNull) continue;
            
            bool phased = furn.Collider.GetMaterialMask() == MaterialFlag.PlayerCollision;
            bool contains = highlightedObjects.Contains(furn.Id);
            
            if (phased && !contains)
            {
                furn.SetTransparency(0.25f);
                furn.Highlight(Config.HighlightColor);
                highlightedObjects.Add(furn.Id);
            } else if (!phased && contains)
            {
                furn.SetTransparency(0f);
                furn.Highlight(ObjectHighlightColor.None);
                highlightedObjects.Remove(furn.Id);
            }
        }
    }

    private readonly HashSet<ulong> highlightedObjects = [];

    public override async Task Disable()
    {
        Service.Framework.Update -= OnUpdate;
        HousingService.OnEnterHousingArea -= OnEnterHousingArea;

        await Service.Framework.Run(() =>
        {
            foreach ((ulong id, Furniture furn) in HousingService.CurrentFurniture.Where(x => highlightedObjects.Contains(x.Key)))
            {
                var collider = furn.Collider;
                if (collider == null) continue;

                furn.Highlight(ObjectHighlightColor.None);
            }
        });
        
        highlightedObjects.Clear();
    }
}
