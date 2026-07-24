using System;
using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;

namespace Housingway.Utils;

public static class TerritoryHelpers
{
    public static unsafe uint CorrectedTerritoryTypeId
    { 
        get
        {
            var manager = HousingManager.Instance();
            if (manager == null)
            {
                return Service.ClientState.TerritoryType;
            }
    
            var character = Service.ObjectTable.LocalPlayer;
            if (character != null && manager->CurrentTerritory != null)
            {
                var territoryType = manager->IndoorTerritory != null
                                        ? manager->IndoorTerritory->HouseId.TerritoryTypeId
                                        : Service.ClientState.TerritoryType;

                return territoryType;
            }
    
            return Service.ClientState.TerritoryType;
        }
    }
    
    public static readonly Dictionary<uint, string> HousingDistricts = new()
    {
        {502, "Mist"},
        {505, "Goblet"},
        {507, "The Lavender Beds"},
        {512, "Empyreum"},
        {513, "Shirogane"}
    };
    
    public static readonly Dictionary<sbyte, string> AptWings = new()
    {
        {-128, "wing 1" },
        {-127, "wing 2" }
    };
    
    public static uint GetAreaRowId(uint id)
    {
        try
        {
            return Service.DataManager.GetExcelSheet<TerritoryType>().GetRow(id).PlaceNameZone.RowId;
        }
        catch (Exception e)
        {
            Service.ChatGui.PrintError(e.Message);
            if (e.StackTrace != null) Service.Log.Error(e.StackTrace);
        }

        return 0;
    }

    public static string GetDistrictFromTerritory(uint territoryId)
    {
        return HousingDistricts[GetAreaRowId(territoryId)];
    }
}
