using System;
using System.Linq;
using System.Threading.Tasks;
using Housingway.Config;
using Housingway.Utils;

namespace Housingway.Profiles;

public class ProfileManager : IDisposable
{
    public Profile? Profile { get; set; }
    
    public ProfileManager()
    {
        HousingService.OnEnterHousingArea += OnEnterHousingArea;
    }

    private void OnEnterHousingArea(bool indoors)
    {
        if (Address.TryGetAddress(out var currAddress))
        {
            Task.Run(async () =>
            {
                var profile = await GetAddressProfile(currAddress);
                if (profile is not null)
                {
                    LoadProfile(profile);
                }
                else if (Profile is not null)
                {
                    LoadDefaults();
                }
            });
        }
    }
    
    public static void SaveProfile(Profile profile)
    {
        Task.Run(async () =>
        {
            try
            {
                await Serializer.SaveFile(Serializer.GetFileInfo("Profiles", profile.Name).FullName, profile);
            }
            catch (Exception e)
            {
                Service.Log.Error(e, $"Error while saving {profile.Name}");
            }
        });
    }

    public void Save()
    {
        if (Profile is not null)
        {
            Profile.Config = Plugin.Configuration;
            SaveProfile(Profile);
        }
    }

    public void LoadProfile(Profile profile)
    {
        Save();
        
        Profile = profile;
        Plugin.Configuration = Profile.Config;
        Plugin.TweakManager.ReloadTweaks();
    }

    public void LoadDefaults()
    {
        Profile = null;
        Plugin.Configuration = Service.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Plugin.TweakManager.ReloadTweaks();
    }

    private static async Task<Profile?> GetAddressProfile(Address address)
    {
        var all = await GetAllProfiles();
        var contains = all.Where(x => x.Addresses.Contains(address)).ToArray();

        if (contains.Length == 0) return null;
        if (contains.Length > 1)
        {
            Service.ChatGui.PrintError("There are multiple profiles for this address. Loading the first one, please correct this yourself.");
        }

        return contains[0];
    }

    private static async Task<Profile[]> GetAllProfiles()
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

    public void Dispose()
    {
        HousingService.OnEnterHousingArea -= OnEnterHousingArea;
    }
}
