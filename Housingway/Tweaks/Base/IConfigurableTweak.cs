using Housingway.Config;

namespace Housingway.Tweaks;

public interface IConfigurableTweak : ITweak
{
    void DrawConfig();
}
