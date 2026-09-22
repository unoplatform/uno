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
			session.ClipRect(clipRect);
		}
		else if (GetClipRoundedRect(visual) is { } roundedRect)
		{
			session.ClipRoundRect(roundedRect);
		}
		else if (GetClipPath(visual) is { } clipPath)
		{
			session.ClipPath(clipPath);
			clipPath.Release();
		}
		// A clip that yields no shape at all (a geometric clip whose Geometry is null, or which builds to null)
		// deliberately does not clip: that is what master's render path did. GetPrePaintingClipping reads the same
		// state as "clips everything", so the shadow and automation bounds disagree with the pixels -- master had
		// that inconsistency too, and unifying on the clip-everything reading blanks the visual instead.
	}
}
