using Uno.UI.Composition.Drawing;

namespace Uno.UI.Runtime.Android;

/// <summary>
/// Common interface for GL and Vulkan rendering views on Android.
/// Allows ApplicationActivity to work with either view without branching.
/// </summary>
internal interface IUnoRenderView
{
	void InvalidateRender();

	/// <summary>
	/// The backend negotiated for the current surface, or null while there is none (before the first surface,
	/// or between losing one and negotiating for the next).
	/// </summary>
	IDrawingFactory? Renderer { get; }

	/// <summary>
	/// Releases the GPU resources backing this view. Required on activity teardown: the peer
	/// finalizer never runs <c>Dispose(disposing: true)</c>, so without this each re-created
	/// activity strands its GL/Vulkan context.
	/// </summary>
	void TeardownRenderer();

	UnoExploreByTouchHelper ExploreByTouchHelper { get; }
	TextInputPlugin TextInputPlugin { get; }
}
