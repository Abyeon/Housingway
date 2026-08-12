using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using Housingway.Interface;
using Housingway.Utils;

namespace Housingway.Tweaks;

public class OverrideInteriorLightingConfig
{
    public float Light = 1f;
    
    public LightShape LightShape = LightShape.PointLight;
    public Vector3 Color = Vector3.One;
    public float Intensity = 1f;
    public LightFalloffType FalloffType = LightFalloffType.Linear;
    public float FalloffFactor = 0f;
    public float AngularFalloffDegrees = 0f;
    public float Range = 10f;
    public float CharacterShadowRange = 10f;
}

public partial class OverrideInteriorLighting
{
    public override void DrawConfig()
    {
        float light = Config.Light;
        if (Ui.SliderWithDefault("Light", ref light, 0, 1, InitialValue))
        {
            Config.Light = light;
            SetLight(Config.Light);
        }

        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            Plugin.Configuration.Save();
        }
        
        using var _ = ImRaii.Disabled(!HousingService.IsInside);

        ImGui.Spacing();
        if (ImGui.Button("Restore"))
        {
            SetLight(InitialValue);
            Config.Light = IndoorLight;
            Plugin.Configuration.Save();
        }
        
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
        
        string[] types = Enum.GetNames<LightFalloffType>();
        int current = (int)Config.FalloffType;
        if (ImGui.Combo("Falloff Type", ref current, types, types.Length))
        {
            Config.FalloffType = (LightFalloffType)current;
            ApplySettings();
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
