using System;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Housingway.Config;
using Housingway.Profiles;
using Housingway.Utils;

namespace Housingway.Interface.Windows;

public class ProfileWindow : CustomWindow, IDisposable
{
    public Profile[] LoadedProfiles { get; private set; }
    public bool IsBuilding;
    
    public ProfileWindow() : base("Profile Editor###HousingwayProfileWindow")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(300, 400),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        LoadedProfiles = [];
        HousingService.OnEnterHousingArea += OnEnterHousingArea;
    }

    private void OnEnterHousingArea(bool indoors)
    {
        if (IsOpen)
        {
            Task.Run(async () => await BuildProfiles());
        }
        else
        {
            LoadedProfiles = [];
        }
    }

    public override void OnOpen() => Task.Run(async () => await BuildProfiles());

    public async Task BuildProfiles()
    {
        IsBuilding = true;
        LoadedProfiles = await ProfileManager.GetAllProfiles();
        IsBuilding = false;
    }

    protected override void Render()
    {
        if (!HousingService.IsInside)
        {
            ImGui.TextColoredWrapped(ImGuiColors.WarningForeground, "Profiles are only for the indoors at the moment!");
            return;
        }

        if (HousingService.CurrentAddress is not { } currentAddress)
        {
            ImGui.TextColoredWrapped(ImGuiColors.WarningForeground, "Current address not found!");
            return;
        }
        
        if (IsBuilding)
        {
            var spinner = "|/-\\"[(int)(ImGui.GetTime() / 0.05f) & 3];
            ImGui.Text($"Loading {spinner}");
            return;
        }

        if (ImGui.Button("Create Profile"))
        {
            ImGui.OpenPopup("CreateProfile");
        }
        
        ImGui.SameLine();
        if (ImGui.Button("Load Default Profile"))
        {
            ProfileManager.LoadDefaults();
            ProfileManager.AddressSettings.Profiles.Remove(currentAddress);
            ProfileManager.AddressSettings.Save();
        }
        
        var name = "";
        if (Ui.AddTextConfirmationPopup("CreateProfile", "Create a new profile with the name: ", ref name))
        {
            if (string.IsNullOrEmpty(name) || name.Equals("default", StringComparison.InvariantCultureIgnoreCase))
            {
                Service.ChatGui.PrintError("Cannot create a new profile without a name!");
                return; // one frame of POO, I DO NOT CARE!
            }
            
            var profile = new Profile(name)
            {
                Config = new Configuration(),
            };
            
            Task.Run(async () =>
            {
                await profile.SaveAsync();
                await BuildProfiles();
            });
        }
        
        var id = 0;
        foreach (var profile in LoadedProfiles)
        {
            using var _ = ImRaii.PushId(id++);
            var currentlySelected = ProfileManager.Profile == profile;
            
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Trash))
            {
                ImGui.OpenPopup("DeleteProfile");
            }
            
            if (Ui.AddConfirmationPopup("DeleteProfile", "Are you sure you want to delete this profile?"))
            {
                if (currentlySelected)
                {
                    ProfileManager.LoadDefaults();
                }
                
                Task.Run(async () =>
                {
                    await profile.DeleteAsync();
                    await BuildProfiles();
                });
            }
            
            ImGui.SameLine();
            
            if (ImGui.Selectable(profile.Name, currentlySelected))
            {
                if (currentlySelected)
                {
                    ProfileManager.AddressSettings.Profiles.Remove(currentAddress);
                    ProfileManager.AddressSettings.Save();
                    ProfileManager.LoadDefaults();
                }
                else
                {
                    ProfileManager.AddressSettings.Profiles[currentAddress] = profile.Id;
                    ProfileManager.AddressSettings.Save();
                    ProfileManager.LoadProfile(profile);
                }
            }
        }
    }

    public void Dispose()
    {
        HousingService.OnEnterHousingArea -= OnEnterHousingArea;
    }
}
