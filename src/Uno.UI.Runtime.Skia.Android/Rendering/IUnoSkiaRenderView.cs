namespace Uno.UI.Runtime.Skia.Android;

/// <summary>
/// Common interface for GL and Vulkan rendering views on Android.
/// Allows ApplicationActivity to work with either view without branching.
/// </summary>
internal interface IUnoSkiaRenderView
{
	void InvalidateRender();
	void ResetRendererContext();

	/// <summary>
	/// Releases the GPU resources backing this view. Required on activity teardown: the peer
	/// finalizer never runs <c>Dispose(disposing: true)</c>, so without this each re-created
	/// activity strands its GL/Vulkan context.
	/// </summary>
	void TeardownRenderer();

	UnoExploreByTouchHelper ExploreByTouchHelper { get; }
	TextInputPlugin TextInputPlugin { get; }
}
