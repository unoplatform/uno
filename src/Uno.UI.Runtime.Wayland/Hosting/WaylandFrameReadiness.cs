#nullable enable

using System.Threading;

namespace Uno.UI.Runtime.Wayland;

// Decides when a window configure may be presented: the render thread must have its buffer size,
// and the UI thread must have recorded a frame laid out at the configured bounds.
internal sealed class WaylandFrameReadiness
{
	// Bounds the layout wait; the render target still has to match the configured buffer size.
	internal const int MaxStaleFrames = 8;

	private long _uiGeneration;

	// UI thread state.
	private bool _layoutPending;
	private long _layoutGeneration;
	private int _layoutFrames;

	// Render thread state.
	private long _frameUIGeneration;

	internal long RenderGeneration { get; private set; }

	// UI thread. Returns whether the generation waits for a frame recorded after layout.
	internal bool ConfigureUI(long generation, bool boundsChanged)
	{
		if (boundsChanged || _layoutPending)
		{
			_layoutPending = true;
			_layoutGeneration = generation;
			_layoutFrames = 0;
			return true;
		}
		Interlocked.Exchange(ref _uiGeneration, generation);
		return false;
	}

	// UI thread, after Uno recorded a frame. Returns whether a waiting generation became ready.
	internal bool OnFrameRecorded(bool arrangedAtBounds)
	{
		if (!_layoutPending || (!arrangedAtBounds && ++_layoutFrames <= MaxStaleFrames))
		{
			return false;
		}
		_layoutPending = false;
		Interlocked.Exchange(ref _uiGeneration, _layoutGeneration);
		return true;
	}

	// Render thread.
	internal void ConfigureRender(long generation) => RenderGeneration = generation;

	// Render thread, before drawing Uno's latest picture. A picture taken before the UI became ready
	// can still have the old layout, even if the UI becomes ready while it is drawn.
	internal void BeginFrame() => _frameUIGeneration = Interlocked.Read(ref _uiGeneration);

	internal bool IsFrameReady => RenderGeneration == _frameUIGeneration;

	// Render thread, after BeginFrame. A generation mismatch has no safety valve: presenting it would acknowledge the wrong configure.
	internal bool CanPresent(int canvasWidth, int canvasHeight, int bufferWidth, int bufferHeight)
		=> IsFrameReady && canvasWidth == bufferWidth && canvasHeight == bufferHeight;
}
