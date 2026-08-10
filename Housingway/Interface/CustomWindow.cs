using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
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

    private const float GradientPercent = 0.15f;
    
    protected CustomWindow(string name, ImGuiWindowFlags flags = ImGuiWindowFlags.None, bool forceMainWindow = false)
        : base(name, flags, forceMainWindow)
    {
        // Setting these to false removes the additional button.
        // AllowClickthrough = false;
        // AllowPinning = false;
        // AllowBackgroundBlur = false;
        
        // Replace additional button with custom pin button
        // TitleBarButtons.Add(PinButton);
    }
    
    private Vector2 padding;

    private readonly ImRaii.StyleDisposable style = new();
    private readonly ImRaii.ColorDisposable color = new();

    public override void PreDraw()
    {
        // Get the current title bar color
        var index = IsFocused ? ImGuiCol.TitleBgActive :
                    IsOpen ? ImGuiCol.TitleBg : ImGuiCol.TitleBgCollapsed;
        
        var vec4 = Ui.GetColorVec4(!IsFocused && IsPinned ? ImGuiCol.TitleBgActive : index);
        if (IsFocused || IsPinned) vec4.W = 1; // Make titlebar opaque if the window is focused.
        uint titleCol = ImGui.ColorConvertFloat4ToU32(vec4);
        
        // Re-assign title bar color
        color.Push(index, titleCol);
        
        // Push custom border style
        float borderSize = IsFocused || IsPinned ? 2f : ImGui.GetStyle().WindowBorderSize;
        style.Push(ImGuiStyleVar.WindowBorderSize, borderSize);
        color.Push(ImGuiCol.Border, titleCol);
        
        // Push zero padding
        padding = ImGui.GetStyle().WindowPadding;
        style.Push(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        base.PreDraw();
    }

    protected abstract void Render();

    public override void Draw()
    {
        style.Dispose();
        color.Dispose();
        
        try
        {
            var drawList = ImGui.GetWindowDrawList();
            
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
                    try
                    {
                        Render(); // <-- Main rendering code here
                    }
                    catch (Exception ex)
                    {
                        Service.Log.Error(ex, $"Error while trying to draw {WindowName}");
                    }
                }
            }
            
            // ---- Draw gradient in background ----
            drawList.ChannelsSetCurrent(0);
        
            if (IsFocused || IsPinned)
            {
                uint titleColor = ImGui.GetColorU32(ImGuiCol.TitleBgActive);
                uint windowBg = ImGui.GetColorU32(ImGuiCol.WindowBg, 0U);
            
                var size = new Vector2
                {
                    X = ImGui.GetWindowSize().X + 5f,
                    Y = (SizeConstraints.HasValue
                        ? SizeConstraints.Value.MinimumSize.Y * GradientPercent
                        : ImGui.GetWindowSize().Y * GradientPercent) * ImGuiHelpers.GlobalScale
                };

                bool noDeco = (Flags & ImGuiWindowFlags.NoDecoration) == ImGuiWindowFlags.NoDecoration;
            
                var position = new Vector2
                {
                    X = ImGui.GetWindowPos().X - 5f,
                    Y = ImGui.GetWindowPos().Y + (noDeco ? 0 : ImGui.GetFrameHeight())
                };
            
                drawList.AddRectFilledMultiColor(position, position + size, titleColor, titleColor, windowBg, windowBg);
            }
        
            drawList.ChannelsMerge();
        }
        catch (Exception ex)
        {
            Service.Log.Error(ex.ToString());
        }
    }

    public override void PostDraw()
    {
        style.Dispose();
        color.Dispose();
        base.PostDraw();
    }
}
