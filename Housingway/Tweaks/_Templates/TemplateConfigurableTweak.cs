using Housingway.Tweaks.Base;

namespace Housingway.Tweaks._Templates;

public partial class TemplateConfigurableTweak : ConfigurableTweak<TemplateConfig>
{
    public override string Name { get; init; } = "Configurable Tweak Name";
    public override string Author { get; init; } = "Your Name Here";
    public override string Description { get; init; } = "Some Description";
    
    public TemplateConfigurableTweak()
    {
        // Config = Plugin.Configuration.Tweaks.ADD-YOUR-CONFIG-INSTANCE-HERE
    }
    
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
