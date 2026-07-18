using System;
using Dalamud.Game.ClientState;
using Dalamud.Game.Command;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

namespace Housingway.Tweaks;

public unsafe class DoorCommand : BaseTweak
{
    public override string Name { get; init; } = "Door Command";
    public override string Author { get; init; } = "Abyeon";
    public override string Description { get; init; } = "Adds the /door command to easily return to the entrance of a house, plot, or apartment.";
    
    public override void Enable()
    {
        Service.CommandManager.AddHandler("/door", new CommandInfo(OnCommand)
        {
            HelpMessage = "Moves the player to the entrance."
        });
    }
    
    public override void Disable()
    {
        Service.CommandManager.RemoveHandler("/door");
    }
    
    private void OnCommand(string command, string args)
    {
        var man = HousingManager.Instance();
        if (man is null) return;
        man->MoveToEntry();
    }
    
    public override void Dispose() { }
}
