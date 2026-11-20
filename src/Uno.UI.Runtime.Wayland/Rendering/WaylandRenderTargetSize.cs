#nullable enable

namespace Uno.UI.Runtime.Wayland.Rendering;

internal static class WaylandRenderTargetSize
{
	// Even the bounded layout fallback must draw into a buffer matching the configure being acknowledged.
	internal static (int Width, int Height) GetSize(bool configured, int logicalWidth, int logicalHeight, double scale, int canvasWidth, int canvasHeight)
		=> configured
			? (WaylandRenderScale.ToBufferPixels(logicalWidth, scale), WaylandRenderScale.ToBufferPixels(logicalHeight, scale))
			: (canvasWidth, canvasHeight);
}
