using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Housingway.Structs.Env;
using Housingway.Structs.Env.Weather;
using Housingway.Utils;
using EnvState = Housingway.Structs.Env.EnvState;

namespace Housingway.Tweaks.OverrideSkybox;

public class OverrideSkyboxConfig
{
    public EnvOverride Override = EnvOverride.SkyId | EnvOverride.Clouds | EnvOverride.Stars;
    public EnvState State = new()
    {
        SkyId = 29,
        Clouds = new EnvClouds
        {
            CloudTexture = 6,
            CloudSideTexture = 45,
            CloudColor = new Vector3(0.47197425f, 0.53638506f, 0.62441313f),
            Color2 = new Vector3(0.20952633f, 0.34112024f, 0.45539904f),
            Gradient = 0.919f,
            SideHeight = 0.575f,
        },
        Stars = new EnvStars
        {
            ConstellationIntensity = 1.053f,
            Constellations = 2.877f,
            Stars = 9.614f,
            GalaxyIntensity = 4.772f,
            StarIntensity = 0.184f,
        }
    };
}

// Ktisis stuff <3
public partial class OverrideSkybox
{
    private SetTextureSelect texSky;
    private SetTextureSelect texCloudTop;
    private SetTextureSelect texCloudSide;
    
    public override unsafe void DrawConfig()
    {
        if (!Enabled) return;
        
        var env = EnvManagerEx.Instance();
        if (env is null) return;

        using var _ = ImRaii.Disabled(!HousingService.IsInside);

        if (DrawSkyEditor(ref Config.State))
        {
            Config.Override = envService!.Override;
            env->EnvState = Config.State;
            
            PluginConfig.Save();
        }
        
        ImGui.Spacing();

        if (DrawStarsEditor(ref Config.State))
        {
            Config.Override = envService!.Override;
            env->EnvState = Config.State;
            
            PluginConfig.Save();
        }
    }

    private bool DrawSkyEditor(ref EnvState state)
    {
        var result = false;
        result |= DrawToggleCheckbox("Override Sky", EnvOverride.SkyId);
        
        using (ImRaii.Disabled(!Config.Override.HasFlag(EnvOverride.SkyId)))
        {
            result |= texSky.Draw("Sky", ref state.SkyId, id => $"bgcommon/nature/sky/texture/sky_{id:D3}.tex");
        }
        
        result |= DrawToggleCheckbox("Override Cloud", EnvOverride.Clouds);
        using var clouds = ImRaii.Disabled(!Config.Override.HasFlag(EnvOverride.Clouds));
        
        result |= texCloudTop.Draw("Cloud Top", ref state.Clouds.CloudTexture, id => $"bgcommon/nature/cloud/texture/cloud_{id:D3}.tex");
        result |= texCloudSide.Draw("Cloud Side", ref state.Clouds.CloudSideTexture, id => $"bgcommon/nature/cloud/texture/cloudside_{id:D3}.tex");

        result |= ImGui.ColorEdit3("Sky Color", ref state.Clouds.CloudColor);
        result |= ImGui.ColorEdit3("Shadow Color", ref state.Clouds.Color2);
        result |= ImGui.SliderFloat("Shadows", ref state.Clouds.Gradient, 0.0f, 2.0f);
        result |= ImGui.SliderFloat("Side Height", ref state.Clouds.SideHeight, 0.0f, 2.0f);
        
        return result;
    }

    private bool DrawStarsEditor(ref EnvState state)
    {
        var result = false;
        result |= DrawToggleCheckbox("Override Stars", EnvOverride.Stars);

        using var _ = ImRaii.Disabled(!Config.Override.HasFlag(EnvOverride.Stars));
        
        result |= ImGui.SliderFloat("Stars", ref state.Stars.Stars, 0.0f, 20.0f);
        result |= ImGui.SliderFloat("Intensity" + "##1", ref state.Stars.StarIntensity, 0.0f, 2.5f);
        ImGui.Spacing();
        result |= ImGui.SliderFloat("Constellations", ref state.Stars.Constellations, 0.0f, 10.0f);
        result |= ImGui.SliderFloat("Constellation Intensity" + "##2", ref state.Stars.ConstellationIntensity, 0.0f, 2.5f);
        ImGui.Spacing();
        result |= ImGui.SliderFloat("Galaxy Intensity", ref state.Stars.GalaxyIntensity, 0.0f, 10.0f);
        // ImGui.Spacing();
        // result |= ImGui.ColorEdit4("Moon Color", ref state.Stars.MoonColor);
        // result |= ImGui.SliderFloat("Moon Brightness", ref state.Stars.MoonBrightness, 0.0f, 1.0f);
        //
        return result;
    }
    
    private bool DrawToggleCheckbox(string label, EnvOverride flag) {
        var active = Config.Override.HasFlag(flag);
        var toggled = ImGui.Checkbox(label, ref active);
        if (toggled)
        {
            Config.Override ^= flag;
            envService!.Override = Config.Override;
        }
        
        ImGui.Spacing();
        return toggled;
    }
}
