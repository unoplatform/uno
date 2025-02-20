#nullable enable
using System;
using System.Linq;
using SkiaSharp;
using Uno.Extensions;
using Windows.Foundation;

namespace Windows.UI.Composition;

partial class CompositionClip
{
	/// <summary>
	/// Returns the bounds of the clip. The clip itself could be non-rectangular, e.g, rounded rectangle or path.
	/// Note that this already handles TransformMatrix
	/// </summary>
	internal Rect? GetBounds(Visual visual)
	{
		if (GetBoundsCore(visual) is { } bounds)
		{
			return TransformMatrix.Transform(bounds);
		}

		return null;
	}

	/// <summary>
	/// Returns the bounds of the clip. The clip itself could be non-rectangular, e.g, rounded rectangle or path.
	/// Note that implementors should not handle TransformMatrix. The result is already transformed by <see cref="GetBounds"/>.
	/// </summary>
	private protected virtual Rect? GetBoundsCore(Visual visual)
		=> null;

	internal virtual void Apply(SKCanvas canvas, Visual visual)
	{
	}
}
