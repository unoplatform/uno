#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Input;
using Windows.Graphics;

namespace Uno.UI.UI.Input.Internal;

/// <summary>
/// Stores the non-client region rects of a window and resolves which region a client point falls in.
/// </summary>
internal sealed class NonClientRegionSet
{
	// Regions overlap (e.g. TitleBar punches Passthrough holes into the Caption rect set by SetTitleBar),
	// so hit-test in a fixed precedence order rather than in registration order: Passthrough wins, as in WinUI.
	private static readonly NonClientRegionKind[] _hitTestOrder =
	[
		NonClientRegionKind.Passthrough,
		NonClientRegionKind.Close,
		NonClientRegionKind.Maximize,
		NonClientRegionKind.Minimize,
		NonClientRegionKind.Icon,
		NonClientRegionKind.TopBorder,
		NonClientRegionKind.LeftBorder,
		NonClientRegionKind.RightBorder,
		NonClientRegionKind.BottomBorder,
		NonClientRegionKind.Caption,
	];

	private readonly Dictionary<NonClientRegionKind, RectInt32[]> _regionRects = new();

	public void ClearAll() => _regionRects.Clear();

	public void Clear(NonClientRegionKind region) => _regionRects.Remove(region);

	public RectInt32[] Get(NonClientRegionKind region)
		=> _regionRects.TryGetValue(region, out var rects) ? rects : Array.Empty<RectInt32>();

	public void Set(NonClientRegionKind region, RectInt32[] rects)
	{
		if (rects.Length == 0)
		{
			_regionRects.Remove(region);
			return;
		}

		_regionRects[region] = rects;
	}

	public bool HasRects(NonClientRegionKind region) => Get(region).Length > 0;

	/// <summary>
	/// Returns the highest-precedence region containing the point, or null when the point is in none of them.
	/// </summary>
	/// <param name="point">The point, in the same (physical client) coordinates as the region rects.</param>
	public NonClientRegionKind? HitTest(PointInt32 point)
	{
		foreach (var kind in _hitTestOrder)
		{
			if (!_regionRects.TryGetValue(kind, out var rects))
			{
				continue;
			}

			foreach (var rect in rects)
			{
				if (rect.Contains(point))
				{
					return kind;
				}
			}
		}

		return null;
	}
}
