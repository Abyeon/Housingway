using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Housingway.Tweaks.Base;

namespace Housingway;

public class TweakManager : IAsyncDisposable
{
    public List<ITweak> Tweaks { get; private set; } = [];
    
    public async Task LoadTweaks()
    {
        Tweaks = GetTweaks();
        Tweaks.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));

        List<Task> tasks = [];
        foreach (ITweak tweak in Tweaks)
        {
            if (Plugin.Configuration.EnabledTweaks.Contains(tweak.GetType().Name))
            {
                tasks.Add(Task.Run(() => EnableTweak(tweak, false)));
            }
        }
        
        await Task.WhenAll(tasks);
    }

    public async Task ReloadTweaks()
    {
        List<Task> tasks = [];
        tasks.AddRange(Tweaks.Select(tweak => Task.Run(() => ReloadTweak(tweak))));
        await Task.WhenAll(tasks);
    }

    private static async Task ReloadTweak(ITweak tweak)
    {
        if (tweak.Enabled)
        {
            await DisableTweak(tweak, false);
        }
            
        if (Plugin.Configuration.EnabledTweaks.Contains(tweak.GetType().Name))
        {
            await EnableTweak(tweak, false);
        }
    }
    
    public static async Task EnableTweak(ITweak tweak, bool updateConfig = true)
    {
        if (tweak.Enabled) return;
        
        Service.Log.Verbose($"Trying to enable Tweak {tweak.Name}");
        
        try
        {
            await tweak.Enable();
            tweak.Enabled = true;

            if (updateConfig)
            {
                Plugin.Configuration.EnabledTweaks.Add(tweak.GetType().Name);
                Plugin.Configuration.Save();
            }
            
            Service.Log.Verbose($"Enabled Tweak {tweak.Name}");
        }
        catch (Exception e)
        {
            Service.Log.Error(e.ToString());
            Service.ChatGui.PrintError($"Error while enabling {tweak.Name}");
        }
    }

    public static async Task DisableTweak(ITweak tweak, bool updateConfig = true)
    {
        if (!tweak.Enabled) return;
        
        Service.Log.Verbose($"Trying to disable Tweak {tweak.Name}");
        
        try
        {
            await tweak.Disable();
            tweak.Enabled = false;

            if (updateConfig)
            {
                Plugin.Configuration.EnabledTweaks.Remove(tweak.GetType().Name);
                Plugin.Configuration.Save();
            }
            
            Service.Log.Verbose($"Disabled Tweak {tweak.Name}");
        }
        catch (Exception e)
        {
            Service.Log.Error(e.ToString());
            Service.ChatGui.PrintError($"Error while disabling {tweak.Name}");
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
    
    public async ValueTask DisposeAsync()
    {
        List<Task> tasks = [];
        tasks.AddRange(Tweaks.Select(tweak => Task.Run(() => DisableTweak(tweak, false))));

        await Task.WhenAll(tasks);
    }
}
