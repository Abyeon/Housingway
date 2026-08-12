using System;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Housingway.Interface;
using Housingway.Utils;

namespace Housingway.Tweaks;

[Flags]
public enum LightConfigFlags
{
    None,
    Brightness,
    Object
}

public class OverrideInteriorLightingConfig
{
    public LightConfigFlags ConfigFlags = LightConfigFlags.Brightness;
    public float Light = 1f;
    // public LightShape LightShape = LightShape.PointLight;
    public Vector3 Color = Vector3.One;
    public float Intensity = 1f;
    // public float FalloffFactor = 0f;
    // public float AngularFalloffDegrees = 0f;
    public float Range = 10f;
    // public float CharacterShadowRange = 10f;
}

public partial class OverrideInteriorLighting
{
    public override void DrawConfig()
    {
        using var _ = ImRaii.Disabled(!HousingService.IsInside);
        
        uint flags = (uint)Config.ConfigFlags;
        if (ImGui.CheckboxFlags("Edit Brightness", ref flags, (uint)LightConfigFlags.Brightness))
        {
            Config.ConfigFlags = (LightConfigFlags)flags;
        }

        using (ImRaii.Disabled(!Config.ConfigFlags.HasFlag(LightConfigFlags.Brightness)))
        {
            float light = Config.Light;
            if (Ui.SliderWithDefault("Brightness", ref light, 0, 1, InitialValue))
            {
                Config.Light = light;
                SetLight(Config.Light);
            }
                
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                Plugin.Configuration.Save();
            }
        }

        if (ImGui.CheckboxFlags("Edit Light Object", ref flags, (uint)LightConfigFlags.Object))
        {
            Config.ConfigFlags = (LightConfigFlags)flags;
            ClearLights();
            Task.Run(Update);
        }
        
        using (ImRaii.Disabled(!Config.ConfigFlags.HasFlag(LightConfigFlags.Object)))
        {
            if (ImGui.ColorEdit3("Color", ref Config.Color))
            {
                ApplySettings();
            }
        
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                Plugin.Configuration.Save();
            }

            if (ImGui.DragFloat("Intensity", ref Config.Intensity))
            {
                ApplySettings();
            }
        
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                Plugin.Configuration.Save();
            }

            if (ImGui.DragFloat("Range", ref Config.Range))
            {
                ApplySettings();
            }

            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                Plugin.Configuration.Save();
            }
        }
    }
}
