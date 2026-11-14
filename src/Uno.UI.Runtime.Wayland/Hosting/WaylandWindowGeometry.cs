#nullable enable

using Uno.UI.Runtime.Wayland.Rendering;

namespace Uno.UI.Runtime.Wayland;

// Derive Uno's bounds and the render target from the same integer content geometry. Uno uses a float DPI
// scale and nearest-even rounding, while Wayland fractional buffers use a double scale and halves up.
internal readonly record struct WaylandWindowGeometry(double Width, double Height, float Scale, int BufferWidth, int BufferHeight, int OuterHeight)
{
	internal static WaylandWindowGeometry Create(int width, int height, int titleBarHeight, double scale)
	{
		var bufferWidth = WaylandRenderScale.ToBufferPixels(width, scale);
		var bufferHeight = WaylandRenderScale.ToBufferPixels(height, scale);
		var rasterizationScale = (float)scale;
		return new(bufferWidth / (double)rasterizationScale, bufferHeight / (double)rasterizationScale, rasterizationScale,
			bufferWidth, bufferHeight, WaylandRenderScale.ToBufferPixels(height + titleBarHeight, scale));
	}
}
