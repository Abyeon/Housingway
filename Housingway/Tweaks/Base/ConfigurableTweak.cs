using Housingway.Config;

namespace Housingway.Tweaks;

public abstract partial class ConfigurableTweak<T> : BaseTweak, IConfigurableTweak
{
    protected T Config = default!;
    protected Configuration PluginConfig = null!;
    
    public abstract void DrawConfig();
}
