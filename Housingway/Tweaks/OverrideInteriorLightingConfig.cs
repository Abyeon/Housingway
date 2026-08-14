using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using Housingway.Interface;
using Housingway.Utils;
using Housingway.Utils.Extensions;

namespace Housingway.Tweaks;

[Flags]
public enum LightConfigFlags
{
    None = 0,
    Brightness = 1 << 0,
    Object = 1 << 1,
    Color = 1 << 2,
    Range = 1 << 3,
    Flags = 1 << 4,
    Rave = 1 << 5
}

public class OverrideInteriorLightingConfig
{
    public LightConfigFlags ConfigFlags = LightConfigFlags.Brightness;
    
    public float Light = 1f;
    
    // public LightShape LightShape = LightShape.PointLight;
    public LightFlags Flags = LightFlags.SpecularHighlights | 
                              LightFlags.CharacterShadows | 
                              LightFlags.ObjectShadows |
                              LightFlags.SSAO_Omnishadows;
    
    public Vector3 Color = Vector3.One;
    public float Intensity = 5f;
    public float Range = 10f;

    public float RaveSpeed = 0.2f;
}

public partial class OverrideInteriorLighting
{
    public override void DrawConfig()
    {
        using var _ = ImRaii.Disabled(!HousingService.IsInside);
        
        uint flags = (uint)Config.ConfigFlags;
        
        // -- Brightness --
        if (ImGui.CheckboxFlags("Edit Brightness", ref flags, (uint)LightConfigFlags.Brightness))
        {
            Config.ConfigFlags = (LightConfigFlags)flags;
            SetLight(Config.ConfigFlags.HasFlag(LightConfigFlags.Brightness) ? Config.Light : InitialValue);
        }

        using (ImRaii.Disabled(!Config.ConfigFlags.HasFlag(LightConfigFlags.Brightness)))
        {
            using var brightnessIndent = ImRaii.PushIndent();
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

        // -- Light Object Editing --
        if (ImGui.CheckboxFlags("Edit Light Objects", ref flags, (uint)LightConfigFlags.Object))
        {
            Config.ConfigFlags = (LightConfigFlags)flags;
            Update();
        }
        
        using (ImRaii.Disabled(!Config.ConfigFlags.HasFlag(LightConfigFlags.Object)))
        {
            using var objectIndent = ImRaii.PushIndent();
            // -- Color / Intensity --
            if (ImGui.CheckboxFlags("Edit Color", ref flags, (uint)LightConfigFlags.Color))
            {
                Config.ConfigFlags = (LightConfigFlags)flags;
                ApplySettings();
            }

            using (ImRaii.Disabled(!Config.ConfigFlags.HasFlag(LightConfigFlags.Color)))
            {
                using var colorIndent = ImRaii.PushIndent();
                if (ImGui.CheckboxFlags("Rave Mode", ref flags, (uint)LightConfigFlags.Rave))
                {
                    Config.ConfigFlags = (LightConfigFlags)flags;
                    ApplySettings();
                }
                
                bool raveMode = Config.ConfigFlags.HasFlag(LightConfigFlags.Rave);

                if (!raveMode)
                {
                    if (ImGui.ColorEdit3("Color", ref Config.Color))
                    {
                        ApplySettings();
                    }
                }
                else
                {
                    if (ImGui.SliderFloat("Speed", ref Config.RaveSpeed, 0, 1f))
                    {
                        ApplySettings();
                    }
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
            }

            // -- Range --
            if (ImGui.CheckboxFlags("Edit Range", ref flags, (uint)LightConfigFlags.Range))
            {
                Config.ConfigFlags = (LightConfigFlags)flags;
                ApplySettings();
            }

            using (ImRaii.Disabled(!Config.ConfigFlags.HasFlag(LightConfigFlags.Range)))
            {
                using var rangeIndent = ImRaii.PushIndent();
                
                if (ImGui.DragFloat("Range", ref Config.Range))
                {
                    ApplySettings();
                }

                if (ImGui.IsItemDeactivatedAfterEdit())
                {
                    Plugin.Configuration.Save();
                }
            }
            
            // -- Flags --
            if (ImGui.CheckboxFlags("Edit Flags", ref flags, (uint)LightConfigFlags.Flags))
            {
                Config.ConfigFlags = (LightConfigFlags)flags;
                ApplySettings();
            }

            using (ImRaii.Disabled(!Config.ConfigFlags.HasFlag(LightConfigFlags.Flags)))
            {
                using var flagsIndent = ImRaii.PushIndent();
                
                using var combo = ImRaii.Combo("Flags", Config.Flags.ToString());
                if (combo.Success)
                {
                    foreach (LightFlags flag in Enum.GetValues<LightFlags>())
                    {
                        bool selected = Config.Flags.HasFlag(flag);
                        if (ImGui.Selectable(flag.ToString(), selected, ImGuiSelectableFlags.DontClosePopups))
                        {
                            if (selected)
                            {
                                Config.Flags &= ~flag;
                            }
                            else
                            {
                                Config.Flags |= flag;
                            }
                        
                            ApplySettings();
                        }
                    }
                }
            }
        }
    }
}
