#nullable enable

using System;
using System.Numerics;
using SkiaSharp;

namespace Uno.UI.Composition;


// Accessing Surface.Canvas is slow due to SkiaSharp interop.
// Avoid using .Surface.Canvas and use .Canvas right away.
/// <param name="RootTransform">The transform matrix to the root visual of this drawing session (which isn't necessarily the identity matrix due to scaling (DPI) and/or RenderTargetBitmap.</param>
internal readonly record struct PaintingSession(SKSurface Surface, SKCanvas Canvas, in DrawingFilters Filters, in Matrix4x4 RootTransform) : IDisposable
{
	public static void PushOpacity(ref PaintingSession session, float opacity)
	{
		// We try to keep the filter ref as long as possible in order to share the same filter.OpacityColorFilter
		if (opacity is not 1.0f)
		{
			var filters = session.Filters;
			session = session with { Filters = filters with { Opacity = filters.Opacity * opacity } };
		}
	}

	/// <inheritdoc />
	public void Dispose()
		=> Canvas.Restore();
}
