using Microsoft.JSInterop;

namespace SkillSnap.Client.Services;

/// <summary>
/// Service for managing Bootstrap tooltips in Blazor components.
/// Ensures tooltips are reinitialized after component updates.
/// </summary>
public class TooltipService
{
    private readonly IJSRuntime _jsRuntime;

    public TooltipService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Reinitializes all tooltips on the page.
    /// Call this after dynamic content updates or component renders.
    /// </summary>
    public async Task ReinitializeTooltipsAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("reinitializeTooltips");
        }
        catch
        {
            // Ignore if function not available (during prerendering)
        }
    }
}
