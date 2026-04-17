namespace Uno.UI.Runtime.Skia.Android;

/// <summary>
/// Common interface for GL and Vulkan rendering views on Android.
/// Allows ApplicationActivity to work with either view without branching.
/// </summary>
internal interface IUnoSkiaRenderView
{
	void InvalidateRender();
	void ResetRendererContext();
	UnoExploreByTouchHelper ExploreByTouchHelper { get; }
	TextInputPlugin TextInputPlugin { get; }
}
