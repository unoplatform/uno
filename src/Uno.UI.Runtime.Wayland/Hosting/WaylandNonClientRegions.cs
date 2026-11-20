#nullable enable

using System;

namespace Uno.UI.Runtime.Wayland;

// Same values as Microsoft.UI.Input.NonClientRegionKind; kept separate so the hit test has no Uno dependency.
internal enum WaylandNonClientRegionKind
{
	Close,
	Maximize,
	Minimize,
	Icon,
	Caption,
	TopBorder,
	LeftBorder,
	BottomBorder,
	RightBorder,
	Passthrough,
}

// What a press starts: client input, an interactive move, the window menu or an interactive resize.
internal enum WaylandNonClientArea
{
	Client,
	Caption,
	Icon,
	Resize,
}

internal readonly record struct WaylandNonClientRect(int X, int Y, int Width, int Height)
{
	internal bool Contains(double x, double y) => x >= X && y >= Y && x < X + (double)Width && y < Y + (double)Height;
}

// Non-client region rectangles of a window whose content extends into the title bar, in physical pixels
// relative to the content origin, as set through InputNonClientPointerSource. UI thread only.
internal sealed class WaylandNonClientRegions
{
	private const uint TopEdge = 1, BottomEdge = 2, LeftEdge = 4, RightEdge = 8; // xdg_toplevel.resize_edge bits

	private readonly WaylandNonClientRect[][] _regions = new WaylandNonClientRect[(int)WaylandNonClientRegionKind.Passthrough + 1][];

	internal WaylandNonClientRegions()
	{
		Array.Fill(_regions, []);
	}

	internal WaylandNonClientRect[] Get(WaylandNonClientRegionKind kind) => _regions[(int)kind];

	internal void Set(WaylandNonClientRegionKind kind, WaylandNonClientRect[] rects) => _regions[(int)kind] = rects;

	internal void Clear() => Array.Fill(_regions, []);

	// Passthrough and caption button regions stay client input (the caption buttons are XAML buttons),
	// borders resize, the icon opens the window menu, then caption regions move. Without any caption region,
	// the top defaultCaptionHeight pixels act as the caption, like the default title bar on Windows.
	internal (WaylandNonClientArea Area, uint Edges) HitTest(double x, double y, double defaultCaptionHeight)
	{
		if (Contains(WaylandNonClientRegionKind.Passthrough, x, y)
			|| Contains(WaylandNonClientRegionKind.Close, x, y)
			|| Contains(WaylandNonClientRegionKind.Maximize, x, y)
			|| Contains(WaylandNonClientRegionKind.Minimize, x, y))
		{
			return (WaylandNonClientArea.Client, 0);
		}
		var edges = (Contains(WaylandNonClientRegionKind.TopBorder, x, y) ? TopEdge : 0)
			| (Contains(WaylandNonClientRegionKind.BottomBorder, x, y) ? BottomEdge : 0)
			| (Contains(WaylandNonClientRegionKind.LeftBorder, x, y) ? LeftEdge : 0)
			| (Contains(WaylandNonClientRegionKind.RightBorder, x, y) ? RightEdge : 0);
		// Opposite borders cancel out: a resize needs one edge per axis.
		if ((edges & (TopEdge | BottomEdge)) == (TopEdge | BottomEdge))
		{
			edges &= ~(TopEdge | BottomEdge);
		}
		if ((edges & (LeftEdge | RightEdge)) == (LeftEdge | RightEdge))
		{
			edges &= ~(LeftEdge | RightEdge);
		}
		if (edges != 0)
		{
			return (WaylandNonClientArea.Resize, edges);
		}
		if (Contains(WaylandNonClientRegionKind.Icon, x, y))
		{
			return (WaylandNonClientArea.Icon, 0);
		}
		var captions = Get(WaylandNonClientRegionKind.Caption);
		var isCaption = captions.Length > 0
			? Contains(WaylandNonClientRegionKind.Caption, x, y)
			: x >= 0 && y >= 0 && y < defaultCaptionHeight;
		return (isCaption ? WaylandNonClientArea.Caption : WaylandNonClientArea.Client, 0);
	}

	private bool Contains(WaylandNonClientRegionKind kind, double x, double y)
	{
		foreach (var rect in _regions[(int)kind])
		{
			if (rect.Contains(x, y))
			{
				return true;
			}
		}
		return false;
	}
}
