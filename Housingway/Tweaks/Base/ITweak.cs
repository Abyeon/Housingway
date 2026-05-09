namespace Housingway.Tweaks;

public interface ITweak
{
    string Name { get; init; }
    string Description { get; init; }
    bool Enabled { get; set; }

    void Enable();
    void Disable();
    void Dispose();
}
