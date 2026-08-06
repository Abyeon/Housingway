using System;
using System.Collections.Generic;
using System.Linq;
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
        return allProfiles.FirstOrDefault(item => item.Id == guid);
    }

    public void Save() => Task.Run(async () => await SaveAsync());

    public async Task SaveAsync()
    {
        Service.Log.Verbose("Saving AddressSettings");
        await Serializer.SaveFile(Serializer.GetFileInfo("AddressSettings").FullName + ".json", this);
    }
}
