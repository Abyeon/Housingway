using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.ImGuiFileDialog;
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
    
    private readonly FileDialogManager fileDialogManager;
    
    public ProfileWindow() : base("Profile Editor###HousingwayProfileWindow")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(300, 400),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        LoadedProfiles = [];
        
        fileDialogManager = new FileDialogManager();
        
        HousingService.OnEnterHousingArea += OnEnterHousingArea;
    }

    private void OnEnterHousingArea(bool indoors)
    {
        if (IsOpen)
        {
            Task.Run(async () => await BuildProfileList());
        }
        else
        {
            LoadedProfiles = [];
        }
    }

    public override void OnOpen() => Task.Run(async () => await BuildProfileList());
    public override void OnClose() => LoadedProfiles = [];

    public async Task BuildProfileList()
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
        
        fileDialogManager.Draw();
        
        if (IsBuilding)
        {
            var spinner = "|/-\\"[(int)(ImGui.GetTime() / 0.05f) & 3];
            ImGui.Text($"Loading {spinner}");
            return;
        }
        
        if (ImGuiComponents.IconButton(FontAwesomeIcon.Plus))
        {
            ImGui.OpenPopup("CreateProfile");
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Create a new profile.");
        }

        ImGui.SameLine();
        if (ImGuiComponents.IconButton(FontAwesomeIcon.FileImport))
        {
            fileDialogManager.OpenFileDialog("Import Profile", ".json", (success, pathToFile) =>
            {
                if (!success) return;

                Task.Run(async () =>
                {
                    var profile = await Serializer.LoadFile<Profile>(pathToFile);
                    await AddProfile(profile);
                });
            });
        }
        
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Import a profile from file.");
        }
        
        ImGui.SameLine();
        if (ImGuiComponents.IconButton(FontAwesomeIcon.ArrowsSpin))
        {
            ProfileManager.LoadDefaults();
            ProfileManager.AddressSettings.Profiles.Remove(currentAddress);
            ProfileManager.AddressSettings.Save();
        }
        
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Load the default profile.");
        }
        
        var name = "";
        if (Ui.AddTextConfirmationPopup("CreateProfile", "Create a new profile with the name: ", ref name))
        {
            Task.Run(async () =>
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
                
                await AddProfile(profile);
            });
        }
        
        var id = 0;
        foreach (var profile in LoadedProfiles)
        {
            using var _ = ImRaii.PushId(id++);
            var currentlySelected = ProfileManager.Profile != null && ProfileManager.Profile.Id == profile.Id;
            
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
                    await BuildProfileList();
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

    public async Task AddProfile(Profile profile)
    {
        if (!profile.IsValid())
        {
            Service.ChatGui.PrintError("Profile is not valid!");
            return;
        }

        if (profile.Name.Equals("default", StringComparison.InvariantCultureIgnoreCase))
        {
            Service.ChatGui.PrintError("Cannot create profile with the name \"default\"");
            return;
        }

        if (LoadedProfiles.Any(x => x.Name.Equals(profile.Name, StringComparison.InvariantCultureIgnoreCase)))
        {
            Service.ChatGui.PrintError($"Profile with the name {profile.Name} already exists!");
        }
                    
        await profile.SaveAsync();
        await BuildProfileList();
    }

    public void Dispose()
    {
        HousingService.OnEnterHousingArea -= OnEnterHousingArea;
    }
}
