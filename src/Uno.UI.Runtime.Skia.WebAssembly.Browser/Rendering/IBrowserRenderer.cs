using SkiaSharp;

namespace Uno.UI.Runtime.Skia;

internal interface IBrowserRenderer
{
	void MakeCurrent();
	SKCanvas Resize(int width, int height);
	void Flush();
	bool NeedsForceResize();

	/// <summary>
	/// True if this renderer's surface preserves the previous frame's pixels between presents, so the
	/// composition layer may repaint only the dirty region. The software renderer keeps a persistent backing
	/// bitmap; the WebGL renderer achieves this via a retained offscreen layer (see <see cref="UsesRetainedLayer"/>).
	/// </summary>
	bool SurfaceRetainsContents => false;

	/// <summary>
	/// True if this renderer presents through a persistent offscreen GPU layer: the frame is rendered onto
	/// the retained layer (returned by <see cref="Resize"/>), which is blitted to the (non-retaining) WebGL
	/// swapchain in <see cref="Flush"/>. Lets dirty-rectangles work without relying on preserveDrawingBuffer.
	/// Requires <see cref="SurfaceRetainsContents"/> to also be true.
	/// </summary>
	bool UsesRetainedLayer => false;
}
