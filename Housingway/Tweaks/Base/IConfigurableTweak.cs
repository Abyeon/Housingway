namespace Housingway.Tweaks.Base;

public interface IConfigurableTweak : ITweak
{
    void DrawConfig();
    void ResetConfig();
    void ExportConfig();
    void ImportConfig();
}
