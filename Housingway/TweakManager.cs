using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Housingway.Tweaks;

namespace Housingway;

public class TweakManager : IDisposable
{
    public List<ITweak> Tweaks { get; private set; } = [];
    
    public void LoadTweaks()
    {
        Tweaks = GetTweaks();
        Tweaks.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));
        
        foreach (var tweak in Tweaks)
        {
            if (Plugin.Configuration.EnabledTweaks.Contains(tweak.GetType().Name))
            {
                EnableTweak(tweak);
            }
        }
    }
    
    public static void EnableTweak(ITweak tweak)
    {
        if (tweak.Enabled) return;

        try
        {
            tweak.Enable();
            tweak.Enabled = true;
            Plugin.Configuration.EnabledTweaks.Add(tweak.GetType().Name);
            Plugin.Configuration.Save();
            Plugin.Log.Verbose($"Enabled Tweak {tweak.Name}");
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e.ToString());
            Plugin.ChatGui.PrintError($"Error while enabling {tweak.Name}");
        }
    }

    public static void DisableTweak(ITweak tweak)
    {
        if (!tweak.Enabled) return;
        
        try
        {
            tweak.Disable();
            tweak.Enabled = false;
            Plugin.Configuration.EnabledTweaks.Remove(tweak.GetType().Name);
            Plugin.Configuration.Save();
            Plugin.Log.Verbose($"Disabled Tweak {tweak.Name}");
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e.ToString());
            Plugin.ChatGui.PrintError($"Error while disabling {tweak.Name}");
        }
    }
    
    private static List<ITweak> GetTweaks() =>
        Assembly.GetCallingAssembly()
                .GetTypes()
                .Where(type => typeof(ITweak).IsAssignableFrom(type))
                .Where(type => type is { IsInterface: false, IsAbstract: false })
                .Select(Activator.CreateInstance)
                .OfType<ITweak>()
                .ToList();

    public void Dispose()
    {
        foreach (var tweak in Tweaks)
        {
            if (tweak.Enabled) tweak.Disable();
            tweak.Dispose();
        }
    }
}
