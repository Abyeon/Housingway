using System.Collections.Generic;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Housingway.Tweaks.Base;
using Housingway.Utils;

namespace Housingway.Tweaks;

public unsafe partial class HighlightPhasedObjects : ConfigurableTweak<HighlightPhasedObjectsConfig>
{
    public override string Name { get; init; } = "Highlight Phased Objects";
    public override string Author { get; init; } = "Abyeon";
    public override string Description { get; init; } = "Highlights objects that have had their player collision disabled.";

    public HighlightPhasedObjects()
    {
        Config = Plugin.Configuration.Tweaks.HighlightPhasedObjects;
    }
    
    public override void Enable()
    {
        HousingService.OnFurnitureUpdate += OnFurnitureUpdate;
    }

    private void OnFurnitureUpdate(Furniture furn)
    {
        if (furn.Collider == null) return;
        if (furn.Graphics == null) return;
            
        bool phased = (furn.Collider->ObjectMaterialMask & (1UL << 13)) != 0;
        if (phased)
        {
            furn.Object->Highlight(Config.HighlightColor);
            highlightedObjects.Add(furn.Id);
        } else if (highlightedObjects.Contains(furn.Id))
        {
            furn.Object->Highlight(ObjectHighlightColor.None);
            highlightedObjects.Remove(furn.Id);
        }
    }

    private readonly HashSet<ulong> highlightedObjects = [];

    public override void Disable()
    {
        HousingService.OnFurnitureUpdate -= OnFurnitureUpdate;
        
        foreach (var furn in HousingService.CurrentFurniture.Where(x => highlightedObjects.Contains(x.Id)))
        {
            var collider = furn.Collider;
            if (collider == null) continue;

            var obj = furn.Object;
            obj->Highlight(ObjectHighlightColor.None);
        }
        
        highlightedObjects.Clear();
    }

    public override void Dispose() { }
}
