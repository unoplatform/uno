#nullable enable
using System;
using System.Linq;
using SkiaSharp;
using Uno.Extensions;
using Windows.Foundation;

namespace Microsoft.UI.Composition;

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

	internal virtual SKPath? GetClipPath(Visual visual) => null;
	/// <summary>
	/// Optionally overridable if the clip path can be provided as a rounded rect.
	/// </summary>
	private protected virtual SKRoundRect? GetClipRoundedRect(Visual visual) => null;
	/// <summary>
	/// Optionally overridable if the clip path can be provided as a rect.
	/// </summary>
	private protected virtual SKRect? GetClipRect(Visual visual) => null;

	internal void ApplyClip(Visual visual, SKCanvas canvas)
	{
		if (GetClipRect(visual) is { } clipRect)
		{
			canvas.ClipRect(clipRect, antialias: true);
		}
		else if (GetClipRoundedRect(visual) is { } roundedRect)
		{
			canvas.ClipRoundRect(roundedRect, antialias: true);
		}
		else if (GetClipPath(visual) is { } clipPath)
		{
			canvas.ClipPath(clipPath, antialias: true);
		}
	}
}
