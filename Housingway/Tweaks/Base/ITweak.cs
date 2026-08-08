using System.Threading.Tasks;

namespace Housingway.Tweaks.Base;

public interface ITweak
{
    string Name { get; init; }
    string Author { get; init; }
    string Description { get; init; }
    bool Enabled { get; set; }

    Task Enable();
    Task Disable();
}
