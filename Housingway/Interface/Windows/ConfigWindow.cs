using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Utility.Numerics;
using Housingway.Config;
using Housingway.Tweaks;

namespace Housingway.Interface.Windows;

public class ConfigWindow : CustomWindow, IDisposable
{
    private readonly Plugin plugin;
    
    private string searchText = "";
    private ITweak[] tweaks = [];
    private ITweak? selectedTweak;

    
    public ConfigWindow(Plugin plugin) : base("Housingway###HousingwayConfigWindow")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(700, 550),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
        
        this.plugin = plugin;
        FilterTweaks();
    }

    public void Dispose() { }

    protected override void Render()
    {
        using var color = ImRaii.PushColor(ImGuiCol.FrameBg, ImGuiColors.DalamudWhite with { W = 0.05f });
        
        using var _ = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(5f, 5f) * ImGuiHelpers.GlobalScale);
        using (ImRaii.Child("LeftSide", new Vector2(220, ImGui.GetWindowHeight())))
        {
            Search();
            
            using (ImRaii.PushColor(ImGuiCol.ChildBg, ImGuiColors.DalamudWhite with { W = 0.05f }))
            {
                using var rounding = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 5f);
                using var list = ImRaii.Child($"TweakList", ImGui.GetContentRegionAvail(), false, ImGuiWindowFlags.AlwaysUseWindowPadding);
                TweakList();
            }
        }

        ImGui.SameLine();
        using (ImRaii.Child("TweakConfig", ImGui.GetContentRegionAvail(), false, ImGuiWindowFlags.AlwaysUseWindowPadding))
            TweakConfig();
    }


    private void Search()
    {
        using var frame = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, new Vector2(5f, 6f) * ImGuiHelpers.GlobalScale);
        using var round = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 5f);

        if (ImGuiComponents.IconButton(FontAwesomeIcon.Home))
        {
            selectedTweak = null;
            searchText = "";
        }
        
        ImGui.SameLine();
        
        using var width = ImRaii.ItemWidth(ImGui.GetContentRegionAvail().X);
        
        if (ImGui.InputTextWithHint("###SearchBar", "Start typing...", ref searchText))
        {
            FilterTweaks();
        }
    }
    
    private void FilterTweaks()
    {
        tweaks = plugin.Tweaks.Where(x => x.Name.Contains(searchText, StringComparison.InvariantCultureIgnoreCase)).ToArray();
    }
    
    private void TweakList()
    {
        using var frame = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, new Vector2(3f, 4f) * ImGuiHelpers.GlobalScale);
        
        uint id = 0;
        foreach (var tweak in tweaks)
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
