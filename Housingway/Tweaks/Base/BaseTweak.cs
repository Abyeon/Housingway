using System.Threading.Tasks;

namespace Housingway.Tweaks.Base;

public abstract class BaseTweak : ITweak
{
    public abstract string Name { get; init; }
    public abstract string Author { get; init; }
    public abstract string Description { get; init; }
    public bool Enabled { get; set; }

    public abstract Task Enable();
    public abstract Task Disable();
}
