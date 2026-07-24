using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Housingway.Config;
using Housingway.Utils;

namespace Housingway.Profiles;

public class ProfileManager : IAsyncDisposable
{
    public static AddressSettings AddressSettings { get; set; } = null!;
    public static Profile? Profile { get; set; }

    public async Task LoadAsync()
    {
        AddressSettings = await Serializer.LoadFile<AddressSettings>(Serializer.GetFileInfo("AddressSettings").FullName + ".json");
        
        Address? currAddress = null;
        await Service.Framework.Run(() =>
        {
            if (Address.TryGetAddress(out var address))
            {
                currAddress = address;
            }
        });
        
        if (currAddress is not null)
        {
            if (AddressSettings.Profiles.ContainsKey((Address)currAddress))
            {
                var profile = await AddressSettings.GetProfile((Address)currAddress);
                if (profile is not null)
                    LoadProfile(profile);
            }
        }
        
        HousingService.OnEnterHousingArea += OnEnterHousingArea;
    }

    private void OnEnterHousingArea(bool indoors)
    {
        if (Address.TryGetAddress(out var currAddress))
        {
            if (AddressSettings.Profiles.ContainsKey(currAddress))
            {
                Task.Run(async () =>
                {
                    var profile = await AddressSettings.GetProfile(currAddress);
                    if (profile is not null)
                        LoadProfile(profile);
                });

                return;
            }
        }
        
        LoadDefaults();
    }

    public static void LoadProfile(Profile profile)
    {
        Plugin.Configuration.Save();
        
        Profile = profile;
        Plugin.TweakManager.ReloadTweaks();
    }

    public static void LoadDefaults()
    {
        if (Profile is null) return; // already using default config
        
        Plugin.Configuration.Save();
        
        Profile = null;
        Plugin.TweakManager.ReloadTweaks();
    }

    public static async Task<Profile[]> GetAllProfiles()
    {
        var files = Serializer.GetDirectoryFiles("Profiles");
        var profiles = new Profile[files.Length];

        for (var i = 0; i < files.Length; i++)
        {
            var profile = await Serializer.LoadFile<Profile>(files[i].FullName);
            profiles[i] = profile;
        }
        
        return profiles;
    }

    public async ValueTask DisposeAsync()
    {
        HousingService.OnEnterHousingArea -= OnEnterHousingArea;
        await AddressSettings.SaveAsync();
        
        if (Profile is not null)
        {
            await Profile.SaveAsync();
        }
    }
}
