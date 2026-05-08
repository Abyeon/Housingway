using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace Housingway.Interface.Windows;

public class ConfigWindow : CustomWindow, IDisposable
{
    private readonly Configuration configuration;
    
    public ConfigWindow(Plugin plugin) : base("Housingway###HousingwayConfigWindow")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
        
        

        configuration = plugin.Configuration;
    }

    public void Dispose() { }

    protected override void Render()
    {
        
    }
}
