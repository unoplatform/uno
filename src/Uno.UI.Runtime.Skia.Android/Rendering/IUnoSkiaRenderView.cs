namespace Uno.UI.Runtime.Skia.Android;

/// <summary>
/// Common interface for GL and Vulkan rendering views on Android.
/// Allows ApplicationActivity to work with either view without branching.
/// </summary>
internal interface IUnoSkiaRenderView
{
	void InvalidateRender();
	void ResetRendererContext();

	// Activity lifecycle forwarding. GLSurfaceView documents OnPause/OnResume as
	// required to be called by the hosting Activity for correct render-thread state.
	void OnPause();
	void OnResume();

	UnoExploreByTouchHelper ExploreByTouchHelper { get; }
	TextInputPlugin TextInputPlugin { get; }
}
