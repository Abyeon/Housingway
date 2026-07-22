using System;
using FFXIVClientStructs.FFXIV.Client.Game;
using Housingway.Utils;

namespace Housingway.Profiles;

[Serializable]
public record struct Address(uint WorldId, uint TerritoryId, sbyte Ward, sbyte? Plot = null, short? Room = null)
{
    public static unsafe bool TryGetAddress(out Address address)
    {
        if (!Service.ClientState.IsLoggedIn || !HousingService.InHousingArea)
        {
            address = default;
            return false;
        }

        var man = HousingService.Manager;
        
        address = new Address
        {
            WorldId = Service.PlayerState.CurrentWorld.RowId,
            TerritoryId = Service.ClientState.TerritoryType,
            Ward = man->GetCurrentWard()
        };
        
        if (HousingService.IsInside)
        {
            if (!man->GetCurrentHouseId().IsApartment)
            {
                address.Plot = man->GetCurrentPlot();
            }
            else
            {
                address.TerritoryId = HousingManager.GetOriginalHouseTerritoryTypeId();
            }
            
            address.Room = man->GetCurrentRoom();
        }

        return true;
    }
}
