#nullable enable

using System;
using System.Numerics;
using SkiaSharp;

namespace Uno.UI.Composition;


// Accessing Surface.Canvas is slow due to SkiaSharp interop.
// Avoid using .Surface.Canvas and use .Canvas right away.
/// <param name="InitialTransform">An auxiliary transform matrix that the "TotalMatrix" should be applied on top of.</param>
internal record struct DrawingSession(SKSurface Surface, SKCanvas Canvas, in DrawingFilters Filters, in Matrix4x4 InitialTransform) : IDisposable
{
	public static void PushOpacity(ref DrawingSession session, float opacity)
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
