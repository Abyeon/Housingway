using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Housingway.Utils;

namespace Housingway.Profiles;

public class AddressSettings
{
    public Dictionary<Address, Guid> Profiles { get; set; } = new();

    public async Task<Profile?> GetProfile(Address address)
    {
        if (!Profiles.TryGetValue(address, out var guid))
        {
            return null;
        }

        var allProfiles = await ProfileManager.GetAllProfiles();
        foreach (var item in allProfiles)
        {
            if (item.Id == guid)
            {
                return item;
            }
        }

        return null;
    }

    public void Save() => Task.Run(async () => await SaveAsync());

    public async Task SaveAsync()
    {
        Service.Log.Verbose("Saving AddressSettings");
        await Serializer.SaveFile(Serializer.GetFileInfo("AddressSettings").FullName + ".json", this);
    }
}
