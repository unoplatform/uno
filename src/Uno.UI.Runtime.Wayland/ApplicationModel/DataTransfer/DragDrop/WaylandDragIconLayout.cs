#nullable enable

using System;

namespace Uno.UI.Runtime.Wayland;

// Geometry of the drag icon surface: the wl_buffer size, how the compositor scales it and where it sits
// relative to the pointer hotspot, all derived from the captured bitmap and the window's rasterization scale.
internal readonly record struct WaylandDragIconLayout(int BufferWidth, int BufferHeight, int BufferScale, int LogicalWidth, int LogicalHeight, int OffsetX, int OffsetY)
{
	// Larger icons are scaled down; a whole-selection snapshot would otherwise cost a huge shared buffer.
	internal const int MaxLogicalSize = 512;

	/// <summary>
	/// Lays out a <paramref name="pixelWidth"/> x <paramref name="pixelHeight"/> bitmap rendered at <paramref name="scale"/>.
	/// With wp_viewporter the bitmap is shown as is at its logical size; otherwise it is resampled to an integer buffer scale.
	/// <paramref name="anchor"/> is the icon's top-left corner relative to the pointer in logical pixels, defaulting to
	/// Uno's DragView placement: centred horizontally, three quarters above the pointer.
	/// </summary>
	internal static WaylandDragIconLayout Compute(int pixelWidth, int pixelHeight, double scale, (double X, double Y)? anchor, bool hasViewport)
	{
		scale = scale > 0 ? scale : 1;
		var logicalWidth = Math.Max(1, (int)Math.Round(pixelWidth / scale));
		var logicalHeight = Math.Max(1, (int)Math.Round(pixelHeight / scale));
		var shrink = Math.Min(1.0, MaxLogicalSize / (double)Math.Max(logicalWidth, logicalHeight));
		if (shrink < 1)
		{
			logicalWidth = Math.Max(1, (int)Math.Round(logicalWidth * shrink));
			logicalHeight = Math.Max(1, (int)Math.Round(logicalHeight * shrink));
		}
		var (anchorX, anchorY) = anchor ?? (-logicalWidth / 2.0, -logicalHeight * 3 / 4.0);
		if (shrink < 1 && anchor is not null)
		{
			anchorX *= shrink;
			anchorY *= shrink;
		}
		int bufferScale, bufferWidth, bufferHeight;
		if (hasViewport && shrink >= 1)
		{
			bufferScale = 1;
			bufferWidth = pixelWidth;
			bufferHeight = pixelHeight;
		}
		else
		{
			bufferScale = Math.Clamp((int)Math.Ceiling(scale), 1, 8);
			bufferWidth = logicalWidth * bufferScale;
			bufferHeight = logicalHeight * bufferScale;
		}
		return new(bufferWidth, bufferHeight, bufferScale, logicalWidth, logicalHeight, (int)Math.Round(anchorX), (int)Math.Round(anchorY));
	}
}
