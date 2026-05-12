using System;
using System.Collections.Generic;
using System.Reflection;
using Dalamud.Configuration;
using Housingway.Tweaks;

namespace Housingway.Config;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;
    public HashSet<string> EnabledTweaks { get; set; } = [];
    public TweakConfigs Tweaks { get; set; } = new();

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}

public class TweakConfigs
{
    public readonly OverrideInteriorLightingConfig OverrideInteriorLighting = new();
    public readonly ModelAdjustmentsConfig ModelAdjustments = new();
    public readonly CameraCollisionConfig CameraCollision = new();
    public readonly HighlightPhasedObjectsConfig HighlightPhasedObjects = new();
}
