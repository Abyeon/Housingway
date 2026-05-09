using System;

namespace Housingway.Tweaks;

public abstract class BaseTweak : ITweak, IDisposable
{
    public abstract string Name { get; init; }
    public abstract string Description { get; init; }
    public abstract bool Enabled { get; set; }

    public abstract void Enable();
    public abstract void Disable();
    public abstract void Dispose();
}
