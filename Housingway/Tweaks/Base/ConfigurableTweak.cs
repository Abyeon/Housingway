using System;
using Dalamud.Bindings.ImGui;
using Housingway.Utils;

namespace Housingway.Tweaks.Base;

public abstract class ConfigurableTweak<T> : BaseTweak, IConfigurableTweak
{
    protected T Config = default!;
    public ImGuiWindowFlags OverwriteFlags { get; init; }
    
    public abstract void DrawConfig();

    public void ResetConfig()
    {
        if (Config is null) return;
        
        var defaultInstance = Activator.CreateInstance<T>();
        Config = defaultInstance;
        
        Plugin.Configuration.Save();
    }

    public void ExportConfig()
    {
        Serializer.CompressToClipboard(Config);
    }

    public void ImportConfig()
    {
        if (Serializer.TryDecompressFromClipboard(out T newConfig))
        {
            Config = newConfig;
            Plugin.Configuration.Save();
        }
    }
}
