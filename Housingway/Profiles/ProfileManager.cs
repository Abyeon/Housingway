using System;
using Housingway.Utils;

namespace Housingway.Profiles;

public class ProfileManager : IDisposable
{
    public ProfileManager()
    {
        HousingService.OnEnterHousingArea += OnEnterHousingArea;
    }

    private void OnEnterHousingArea(bool indoors)
    {
        if (Address.TryGetAddress(out var currAddress))
        {
            // fetch profile for address
        }
    }

    public void Dispose()
    {
        HousingService.OnEnterHousingArea -= OnEnterHousingArea;
    }
}
