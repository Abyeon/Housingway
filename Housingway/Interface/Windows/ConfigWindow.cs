using System;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using Housingway.Tweaks.Base;

namespace Housingway.Interface.Windows;

public class ConfigWindow : CustomWindow, IDisposable
{
    private string searchText = "";
    private ITweak[] tweaks = [];
    private ITweak? selectedTweak;

    
    public ConfigWindow() : base("Housingway###HousingwayConfigWindow")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(700, 550),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
        
        FilterTweaks();
    }

    public void Dispose() { }

    protected override void Render()
    {
        var white = ImGuiColors.DalamudWhite with { W = 0.05f };
        using var color = ImRaii.PushColor(ImGuiCol.FrameBg, white);
        
        using var _ = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(5f, 5f) * ImGuiHelpers.GlobalScale);
        using (ImRaii.Child("LeftSide", new Vector2(220, ImGui.GetWindowHeight())))
        {
            Search();
            
            using (ImRaii.PushColor(ImGuiCol.ChildBg, white))
            {
                using var rounding = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 5f);
                using var list = ImRaii.Child($"TweakList", ImGui.GetContentRegionAvail(), false, ImGuiWindowFlags.AlwaysUseWindowPadding);
                TweakList();
            }
        }

        ImGui.SameLine();
        using (ImRaii.Group())
        {
            Tray();
            using (ImRaii.Child("TweakConfig", ImGui.GetContentRegionAvail(), false, ImGuiWindowFlags.AlwaysUseWindowPadding))
                TweakConfig();
        }
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
        tweaks = Plugin.TweakManager.Tweaks.Where(x => x.Name.Contains(searchText, StringComparison.InvariantCultureIgnoreCase)).ToArray();
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
                if (enabled) TweakManager.EnableTweak(tweak);
                else TweakManager.DisableTweak(tweak);
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
            HomePage();
            return;
        }
        
        // Tray();
        
        using var _ = ImRaii.Disabled(!selectedTweak.Enabled);
        
        ImGui.Spacing();

        var color = ImGui.GetColorU32(ImGuiCol.Separator);
        
        Ui.CenteredTextWithLine(selectedTweak.Name, color);
        
        ImGuiHelpers.CenterCursorForText($"by {selectedTweak.Author}");
        ImGui.TextColored(ImGuiColors.DalamudGrey, "by");
        ImGui.SameLine();
        ImGui.TextColored(ImGui.GetColorU32(ImGuiCol.Text), selectedTweak.Author);
        ImGui.Spacing();
        
        ImGui.TextWrapped(selectedTweak.Description);
        ImGui.Spacing();

        if (selectedTweak is IConfigurableTweak config)
        {
            config.DrawConfig();
        }
    }

    private void Tray()
    {
        if (selectedTweak is not IConfigurableTweak { Enabled: true } tweak) return;
        
        using var childColor = ImRaii.PushColor(ImGuiCol.ChildBg, ImGuiColors.DalamudWhite with { W = 0.05f });
        using var padding = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, new Vector2(5f, 0));
        using var rounding = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 5f);
        
        using var _ = ImRaii.Child("Tray", new Vector2(0, ImGui.GetFrameHeight()), false);
        
        using var buttonColor = ImRaii.PushColor(ImGuiCol.Button, 0x00000000);
        if (ImGui.SmallButton("Export"))
        {
            tweak.ExportConfig();
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Export this tweak's config to the clipboard.");
        }

        ImGui.SameLine();
        if (ImGui.SmallButton("Import"))
        {
            tweak.ImportConfig();
        }
        
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Import a configuration from your clipboard.");
        }

        ImGui.SameLine();
        if (ImGui.SmallButton("Reset"))
        {
            tweak.ResetConfig();
        }
        
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Reset this tweak's config to default.");
        }
    }

    private static void HomePage()
    {
        var content = ImGui.GetContentRegionAvail();
        
        // Icon
        if (Service.TextureProvider.GetFromManifestResource(Assembly.GetExecutingAssembly(), "Housingway.Assets.IconNoBg.png").TryGetWrap(out var icon, out _))
        {
            var size = icon.Size * 0.25f * ImGuiHelpers.GlobalScale;
            ImGui.SetCursorPos((content / 2) - (size / 2));
            ImGui.Image(icon.Handle, size);
        }

        // Version + Last Updated display
        var version = $"v{Service.PluginInterface.Manifest.AssemblyVersion.ToString()}";
        var author = $"made by {Service.PluginInterface.Manifest.Author}";
        ImGuiHelpers.CenterCursorForText(version + author);
        ImGui.Text(version);
        ImGui.SameLine();
        ImGui.TextColored(ImGuiColors.DalamudGrey, author);
        
        // Links
        ImGui.Spacing();
        
        var group = new ImGuiHelpers.HorizontalButtonGroup
        {
            IsCentered = true,
            Height = ImGui.GetFrameHeightWithSpacing()
        };
        
        group.Add("Github", () => Util.OpenLink("https://github.com/Abyeon/Housingway"));
        group.Add("Donate", () => Util.OpenLink("https://ko-fi.com/abyeon"));
        group.Draw();
    }
}
