using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Utility.Numerics;
using Housingway.Config;
using Housingway.Tweaks;

namespace Housingway.Interface.Windows;

public class ConfigWindow : CustomWindow, IDisposable
{
    private readonly Configuration configuration;
    private readonly Plugin plugin;
    
    public ConfigWindow(Plugin plugin) : base("Housingway###HousingwayConfigWindow")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(600, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        configuration = plugin.Configuration;
        this.plugin = plugin;
    }

    public void Dispose() { }

    protected override void Render()
    {
        using var _ = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(5f, 5f));

        using (var color = ImRaii.PushColor(ImGuiCol.ChildBg, ImGuiColors.DalamudWhite with { W = 0.05f }))
        {
            using var rounding = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 5f);
            using var list = ImRaii.Child($"TweakList", new Vector2(220, ImGui.GetWindowHeight()), false, ImGuiWindowFlags.AlwaysUseWindowPadding);
            TweakList();
        }

        ImGui.SameLine();
        using (var config = ImRaii.Child("TweakConfig", ImGui.GetContentRegionAvail(), false, ImGuiWindowFlags.AlwaysUseWindowPadding))
            TweakConfig();
    }

    private ITweak? selectedTweak;

    private void TweakList()
    {
        //using var _ = ImRaii.PushStyle(ImGuiStyleVar.Se, 5f);
        
        uint id = 0;
        foreach (var tweak in plugin.Tweaks)
        {
            ImGui.PushID(id++);

            var enabled = tweak.Enabled;
            if (ImGui.Checkbox($"###{nameof(tweak)}", ref enabled))
            {
                if (enabled) plugin.EnableTweak(tweak);
                else plugin.DisableTweak(tweak);
            }

            ImGui.SameLine();
            
            if (ImGui.Selectable(tweak.Name))
            {
                selectedTweak = tweak;
            }

            ImGuiHelpers.ScaledDummy(1);
        }
    }

    private void TweakConfig()
    {
        if (selectedTweak is null)
        {
            ImGui.Text("No tweak selected!");
            return;
        }
        
        using var _ = ImRaii.Disabled(!selectedTweak.Enabled);
        
        ImGui.Spacing();

        var color = ImGui.GetColorU32(ImGuiCol.Separator);
        
        Ui.CenteredTextWithLine(selectedTweak.Name, color);
        ImGui.TextWrapped(selectedTweak.Description);
        ImGui.Spacing();

        if (selectedTweak is IConfigurableTweak config)
        {
            config.DrawConfig();
        }
    }
}
