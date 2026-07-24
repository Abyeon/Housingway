using System.Text;
using System.Text.Json.Serialization;
using FFXIVClientStructs.FFXIV.Client.Game;
using Housingway.Utils;
using Lumina.Excel.Sheets;

namespace Housingway.Profiles;

public record struct Address(uint WorldId, uint TerritoryId, sbyte Ward, sbyte Plot = -1, short Room = -1)
{
    public static unsafe bool TryGetAddress(out Address address)
    {
        if (!Service.ClientState.IsLoggedIn || !HousingService.IsInside)
        {
            address = default;
            return false;
        }

        var man = HousingService.Manager;
        
        address = new Address
        {
            WorldId = Service.PlayerState.CurrentWorld.RowId,
            TerritoryId = TerritoryHelpers.CorrectedTerritoryTypeId,
            Ward = man->GetCurrentWard()
        };

        address.Plot = man->GetCurrentPlot();
        address.TerritoryId = HousingManager.GetOriginalHouseTerritoryTypeId();
        address.Room = man->GetCurrentRoom();

        return true;
    }

    [JsonIgnore]
    public string ReadableName
    {
        get
        {
            StringBuilder sb = new();
            sb.Append(Service.DataManager.Excel.GetSheet<World>().GetRow(WorldId).Name.ExtractText() + " "); // world
            sb.Append(TerritoryHelpers.GetDistrictFromTerritory(TerritoryId)); // district
            sb.Append($" w{Ward + 1}"); // ward
            sb.Append(TerritoryHelpers.AptWings.TryGetValue(Plot, out var wing) ? $" {wing}" : $" p{Plot + 1}"); // plot / wing
            sb.Append(Room == 0 ? "" : $" room {Room}"); // room
            
            return sb.ToString();
        }
    }
}
