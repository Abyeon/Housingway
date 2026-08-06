using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Housingway.Tweaks.Base;
using Housingway.Utils;
using Housingway.Utils.Extensions;

namespace Housingway.Tweaks;

public unsafe partial class HighlightPhasedObjects : ConfigurableTweak<HighlightPhasedObjectsConfig>
{
    public override string Name { get; init; } = "Highlight Phased Objects";
    public override string Author { get; init; } = "Abyeon";
    public override string Description { get; init; } = "Highlights objects that have had their player collision disabled.";
    
    public override void Enable()
    {
        Service.Framework.Update += OnUpdate;
        HousingService.OnEnterHousingArea += OnEnterHousingArea;
    }

    private void OnEnterHousingArea(bool indoors)
    {
        highlightedObjects.Clear();
    }

    private void OnUpdate(IFramework framework)
    {
        foreach (Furniture furn in HousingService.CurrentFurniture)
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

    public override void Disable()
    {
        Service.Framework.Update -= OnUpdate;
        HousingService.OnEnterHousingArea -= OnEnterHousingArea;
        
        foreach (var furn in HousingService.CurrentFurniture.Where(x => highlightedObjects.Contains(x.Id)))
        {
            var collider = furn.Collider;
            if (collider == null) continue;

            var obj = furn.Object;
            obj.Value->Highlight(ObjectHighlightColor.None);
        }
        
        highlightedObjects.Clear();
    }

    public override void Dispose() { }
}
