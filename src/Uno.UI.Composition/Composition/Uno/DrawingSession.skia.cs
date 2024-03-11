#nullable enable

using System;
using System.Linq;
using Microsoft.UI.Composition;
using SkiaSharp;

namespace Uno.UI.Composition;


// Accessing Surface.Canvas is slow due to SkiaSharp interop.
// Avoid using .Surface.Canvas and use Surface.Canvas right away.
internal record struct DrawingSession(SKSurface Surface, SKCanvas Canvas, in DrawingFilters Filters) : IDisposable
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
