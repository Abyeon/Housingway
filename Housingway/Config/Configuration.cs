using System;
using System.Collections.Generic;
using Dalamud.Configuration;
using Housingway.Profiles;
using Housingway.Tweaks;
using Housingway.Tweaks.OverrideSkybox;

namespace Housingway.Config;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;
    public HashSet<string> EnabledTweaks { get; set; } = [];
    public TweakConfigs Tweaks { get; set; } = new();

    public void Save()
    {
        if (ProfileManager.Profile is { } profile)
        {
            profile.Save();
            return;
        }
        
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}

public class TweakConfigs
{
    public OverrideInteriorLightingConfig OverrideInteriorLighting { get; set; } = new();
    public ModelAdjustmentsConfig ModelAdjustments { get; set; } = new();
    public HighlightPhasedObjectsConfig HighlightPhasedObjects { get; set; } = new();
    public FurnitureInfoConfig FurnitureInfo { get; set; } = new();
    public DisplayPopRangeConfig DisplayPopRange { get; set; } = new();
    public OverrideSkyboxConfig OverrideSkybox { get; set; } = new();
}
