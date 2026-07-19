namespace Housingway.Tweaks._Templates;

public class TemplateBaseTweak : BaseTweak
{
    public override string Name { get; init; } = "Tweak Name";
    public override string Author { get; init; } = "Your Name Here";
    public override string Description { get; init; } = "Some description.";
    
    public override void Enable()
    {
        // Subscribe to events, run some logic, etc.
        throw new System.NotImplementedException();
    }

    public override void Disable()
    {
        // Unsubscribe from events, undo some changes to the game, etc.
        throw new System.NotImplementedException();
    }

    public override void Dispose()
    {
        // Dispose anything necessary. (Usually you can just ignore this and throw everything in Disable();
        throw new System.NotImplementedException();
    }
}
