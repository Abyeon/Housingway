using System;
using System.Threading.Tasks;
using Dalamud.Bindings.ImGui;
using Housingway.Utils;

namespace Housingway.Tweaks.Base;

public abstract class ConfigurableTweak<T> : BaseTweak, IConfigurableTweak
{
    protected abstract T Config { get; set; }
    public ImGuiWindowFlags Flags { get; set; }
    
    public abstract void DrawConfig();

    public void ResetConfig()
    {
        Task.Run(() =>
        {
            Config = Activator.CreateInstance<T>();
            Plugin.Configuration.Save();
        });
    }

    public void ExportConfig()
    {
        Task.Run(() => Serializer.CompressToClipboard(Config));
    }

    public void ImportConfig()
    {
        Task.Run(() =>
        {
            if (Serializer.TryDecompressFromClipboard(out T newConfig))
            {
                Config = newConfig;
                Plugin.Configuration.Save();
            }
        });
    }
}
