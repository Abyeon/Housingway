using System.Runtime.CompilerServices;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Housingway.Tweaks.Base;

namespace Housingway.Tweaks;

public class NoPositionalOrchestrion : BaseTweak
{
    public override string Name { get; init; } = "No Positional Orchestrion";
    public override string Author { get; init; } = "Abyeon";
    public override string Description { get; init; } = "Disables the IsPositional flag on orchestrions, so your music plays in your head!";
    
    public override void Enable()
    {
        Service.Framework.Update += OnUpdate;
    }

    private static unsafe void OnUpdate(IFramework framework)
    {
        SetPositional(false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void SetPositional(bool isPositional)
    {
        var man = OrchestrionManager.Instance();
        if (man is null) return;
        
        var sound = man->SoundData;
        if (sound is null) return;
        
        if (sound->IsPositional == isPositional) return;
        
        sound->IsPositional = isPositional;
        sound->SoundController.SetIsNonPositional(!isPositional);
    }

    public override void Disable()
    {
        Service.Framework.Update -= OnUpdate;
        SetPositional(true);
    }

    public override void Dispose() { }
}
