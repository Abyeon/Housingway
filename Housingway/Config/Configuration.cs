using System;
using System.Collections.Generic;
using System.Reflection;
using Dalamud.Configuration;
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
        Service.PluginInterface.SavePluginConfig(this);
    }
}

public class TweakConfigs
{
    public readonly OverrideInteriorLightingConfig OverrideInteriorLighting = new();
    public readonly ModelAdjustmentsConfig ModelAdjustments = new();
    public readonly HighlightPhasedObjectsConfig HighlightPhasedObjects = new();
    public readonly FurnitureInfoConfig FurnitureInfo = new();
    public readonly DisplayPopRangeConfig DisplayPopRange = new();
    public readonly OverrideSkyboxConfig OverrideSkybox = new();
}
