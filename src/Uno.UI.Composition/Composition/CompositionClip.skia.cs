#nullable enable
using System;
using System.Linq;
using Uno.Extensions;
using Uno.UI.Composition.Drawing;
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

	internal virtual IGeometry? GetClipPath(Visual visual) => null;
	/// <summary>
	/// Optionally overridable if the clip path can be provided as a rounded rect.
	/// </summary>
	private protected virtual RoundRectangle? GetClipRoundedRect(Visual visual) => null;
	/// <summary>
	/// Optionally overridable if the clip path can be provided as a rect.
	/// </summary>
	private protected virtual Rect? GetClipRect(Visual visual) => null;

	internal void ApplyClip(Visual visual, IDrawingSession session)
	{
		if (GetClipRect(visual) is { } clipRect)
		{
			session.ClipRect(clipRect, antialias: true);
		}
		else if (GetClipRoundedRect(visual) is { } roundedRect)
		{
			session.ClipRoundRect(roundedRect, antialias: true);
		}
		else if (GetClipPath(visual) is { } clipPath)
		{
			session.ClipPath(clipPath, antialias: true);
		}
	}
}
