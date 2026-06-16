#nullable enable

using System;
using SkiaSharp;

namespace Uno.UI.Composition;

/// <summary>
/// Accumulates, for a single frame, the region of the surface whose visual output changed since the
/// previous presented frame, for dirty-rectangles rendering. For now the region is tracked as a single
/// bounding rectangle (the union of all changed rects); this can be refined into a capped rect list with
/// coalescing later without changing the consumer contract.
/// </summary>
internal sealed class DirtyRegion
{
	private SKRect _bounds;
	private bool _hasContent;

	/// <summary>The whole surface must be repainted this frame (e.g. resize, or too many scattered changes).</summary>
	public bool IsFullFrame { get; private set; }

	/// <summary>Nothing changed this frame and a full repaint is not required.</summary>
	public bool IsEmpty => !IsFullFrame && !_hasContent;

	/// <summary>The union of all changed rectangles added this frame (valid only when not empty / not full-frame).</summary>
	public SKRect Bounds => _bounds;

	public void AddRect(SKRect rect)
	{
		if (IsFullFrame || rect.IsEmpty)
		{
			return;
		}

		if (!_hasContent)
		{
			_bounds = rect;
			_hasContent = true;
		}
		else
		{
			_bounds = new SKRect(
				Math.Min(_bounds.Left, rect.Left),
				Math.Min(_bounds.Top, rect.Top),
				Math.Max(_bounds.Right, rect.Right),
				Math.Max(_bounds.Bottom, rect.Bottom));
		}
	}

	public void SetFullFrame() => IsFullFrame = true;

	public void Reset()
	{
		_bounds = default;
		_hasContent = false;
		IsFullFrame = false;
	}
}
