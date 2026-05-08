using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Style;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace Housingway.Interface;

public abstract class CustomWindow : Window
{
    // Yoinked to re-apply default blur (yes, this is all to remove the additions button, I'm a hater)
    // https://github.com/goatcorp/Dalamud/blob/38b8e5f934dec4e7c28ed15b9e67573e737bb760/Dalamud/Interface/Windowing/WindowHost.cs#L32
    private const float BlurNoiseOpacity = 0.17f;
    private const float MaxBlurStrength = 14f;
    private static readonly Vector4 BlurTintMultiplier = new(158 / 255f, 158 / 255f, 158 / 255f, 25 / 255f);
    
    protected CustomWindow(string name, ImGuiWindowFlags flags = ImGuiWindowFlags.None, bool forceMainWindow = false)
        : base(name, flags, forceMainWindow)
    {
        // Setting these to false removes the additional button.
        AllowClickthrough = false;
        AllowPinning = false;
        AllowBackgroundBlur = false;
        
        // Replace additional button with custom pin button
        TitleBarButtons.Add(PinButton);
    }
    
    private bool isPinned = false;

    private TitleBarButton PinButton
    {
        get
        {
            var icon = isPinned ? FontAwesomeIcon.Lock : FontAwesomeIcon.LockOpen;
            return new TitleBarButton
            {
                Icon = icon,
                IconOffset = new Vector2(2.5f, 1),
                Priority = int.MinValue,
                Click = _ => TogglePin(),
                ShowTooltip = () => ImGui.SetTooltip($"{(isPinned ? "Unlock" : "Lock")} the window.")
            };
        }
    }

    public void TogglePin()
    {
        // Remove current pin button
        TitleBarButtons.RemoveAt(0);
        
        // Update flags
        isPinned = !isPinned;
        if (isPinned) Flags |= (ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);
        else Flags &= ~(ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);
        
        // Add new pin button with new icon
        TitleBarButtons.Add(PinButton);
    }

    private Vector2 padding;
    private bool needsPop = false;

    public override void PreDraw()
    {
        // Get the current title bar color
        var index = IsFocused ? ImGuiCol.TitleBgActive :
                    IsOpen ? ImGuiCol.TitleBg : ImGuiCol.TitleBgCollapsed;
        
        var vec4 = Ui.GetColorVec4(!IsFocused && isPinned ? ImGuiCol.TitleBgActive : index);
        if (IsFocused || isPinned) vec4.W = 1; // Make titlebar opaque if the window is focused.
        var titleCol = ImGui.ColorConvertFloat4ToU32(vec4);
        
        // Re-assign title bar color
        ImGui.PushStyleColor(index, titleCol);
        
        // Push custom border style
        var borderSize = IsFocused || isPinned ? 2f : ImGui.GetStyle().WindowBorderSize;
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, borderSize);
        ImGui.PushStyleColor(ImGuiCol.Border, titleCol);
        
        // Push zero padding
        padding = ImGui.GetStyle().WindowPadding;
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        
        needsPop = true;
        base.PreDraw();
    }

    protected abstract void Render();

    public override void Draw()
    {
        ImGui.PopStyleVar(2);
        ImGui.PopStyleColor(2);
        needsPop = false;
        
        try
        {
            var drawList = ImGui.GetWindowDrawList();
            
            // --- Add blur back ---
            var workStyle = StyleModel.GetFromCurrent();
            if (workStyle is StyleModelV1 workStyleV1)
            {
                var effectiveBlurFactor = workStyleV1.WindowBlurStrength;
                var shouldBlur = effectiveBlurFactor != 0f &&
                                 ImGui.GetWindowViewport().ID == ImGui.GetMainViewport().ID &&
                                 !Flags.HasFlag(ImGuiWindowFlags.NoBackground);

                if (shouldBlur)
                {
                    var wPos = ImGui.GetWindowPos();
                    ImGuiHelpers.PrependBlurBehind(
                        drawList,
                        wPos,
                        wPos + ImGui.GetWindowSize(),
                        effectiveBlurFactor * MaxBlurStrength,
                        ImGui.GetStyle().WindowRounding,
                        tintColor: ImGui.GetStyle()
                                        .Colors[
                                            ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows)
                                                ? (int)ImGuiCol.TitleBgActive
                                                : (int)ImGuiCol.TitleBg] * BlurTintMultiplier,
                        noiseOpacity: BlurNoiseOpacity * ImGui.GetStyle().Alpha);
                }
            }
            
            drawList.ChannelsSplit(2);
            drawList.ChannelsSetCurrent(1);

            // ---- Render the window via a child to add proper padding ----
            var start = ImGui.GetCursorPos() + padding;
            var end = ImGui.GetWindowSize() - padding;
            var childSize = end - start;
            
            ImGui.SetCursorPos(start);
            
            using (var child = ImRaii.Child($"###{WindowName}RenderArea", childSize))
            {
                if (child.Success)
                {
                    Render(); // <-- Main rendering code here
                }
            }
            
            // ---- Draw gradient in background ----
            drawList.ChannelsSetCurrent(0);
        
            if (IsFocused || isPinned)
            {
                var color = ImGui.GetColorU32(ImGuiCol.TitleBgActive);
                var color1 = ImGui.GetColorU32(ImGuiCol.WindowBg, 0U);
            
                var size = new Vector2
                {
                    X = ImGui.GetWindowSize().X + 5f,
                    Y = SizeConstraints.HasValue ? SizeConstraints.Value.MinimumSize.Y * 0.5f : ImGui.GetWindowSize().Y * 0.5f
                };

                var noDeco = (Flags & ImGuiWindowFlags.NoDecoration) == ImGuiWindowFlags.NoDecoration;
            
                var position = new Vector2
                {
                    X = ImGui.GetWindowPos().X - 5f,
                    Y = ImGui.GetWindowPos().Y + (noDeco ? 0 : ImGui.GetFrameHeight())
                };
            
                drawList.AddRectFilledMultiColor(position, position + size, color, color, color1, color1);
            }
        
            drawList.ChannelsMerge();
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex.ToString());
        }
    }

    public override void PostDraw()
    {
        if (needsPop)
        {
            ImGui.PopStyleVar(2);
            ImGui.PopStyleColor(2);
        }
        base.PostDraw();
    }
}
