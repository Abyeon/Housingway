using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
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

    private readonly Dictionary<Type, Delegate> getters = new();
    private readonly Dictionary<Type, Delegate> setters = new();

    public TweakConfigs()
    {
        var properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var instance = Expression.Parameter(typeof(TweakConfigs), "instance");
        
        foreach (var property in properties)
        {
            var type = property.PropertyType;
            var access = Expression.Property(instance, property);
            
            var getter = Expression.Lambda(access, instance);
            getters[type] = getter.Compile();

            var valueParam = Expression.Parameter(type, "value");
            var assign = Expression.Assign(access, valueParam);
            
            var setter = Expression.Lambda(assign, instance, valueParam);
            setters[type] = setter.Compile();
        }
    }

    public T Get<T>()
    {
        if (getters.TryGetValue(typeof(T), out var del))
        {
            var getter = (Func<TweakConfigs, T>)del;
            return getter(this);
        }
        
        throw new KeyNotFoundException($"No getter found for type {typeof(T)}");
    }

    public void Set<T>(T value)
    {
        if (setters.TryGetValue(typeof(T), out var del))
        {
            var setter = (Action<TweakConfigs, T>)del;
            setter(this, value);
            return;
        }
        
        throw new KeyNotFoundException($"No configuration found for type {typeof(T)}");
    }
}
