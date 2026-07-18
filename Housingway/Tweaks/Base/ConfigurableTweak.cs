using Housingway.Config;

namespace Housingway.Tweaks;

public abstract partial class ConfigurableTweak<T> : BaseTweak, IConfigurableTweak
{
    protected T Config = default!;
    
    public abstract void DrawConfig();
}
