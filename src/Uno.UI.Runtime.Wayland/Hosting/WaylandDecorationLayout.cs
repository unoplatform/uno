#nullable enable

using System;
using System.Numerics;

namespace Uno.UI.Runtime.Wayland;

[Flags]
internal enum WaylandCaptionButtons
{
	None = 0,
	Minimize = 1,
	Maximize = 2,
	Close = 4,
}

internal enum WaylandDecorationPart
{
	None,
	TitleBar,
	Minimize,
	Maximize,
	Close,
	ResizeEdge,
}

// Geometry of client-side decorations, in logical pixels. The decoration surface sits below the content
// surface and spans the title bar plus an invisible resize margin around the window geometry.
internal readonly record struct WaylandDecorationLayout(int ContentWidth, int ContentHeight, int TitleBarHeight, int ResizeMargin, WaylandCaptionButtons Buttons)
{
	internal const int DefaultTitleBarHeight = 32;
	internal const int DefaultResizeMargin = 8;
	internal const int ButtonWidth = 46;
	// Corner zones extend along the edges so diagonal resizing does not need pixel precision.
	internal const int CornerSize = 16;

	private static readonly (WaylandCaptionButtons Button, WaylandDecorationPart Part)[] _buttonOrder =
	[
		(WaylandCaptionButtons.Close, WaylandDecorationPart.Close),
		(WaylandCaptionButtons.Maximize, WaylandDecorationPart.Maximize),
		(WaylandCaptionButtons.Minimize, WaylandDecorationPart.Minimize),
	];

	internal static int FitTitleBar(int geometryHeight, int titleBarHeight) => Math.Min(titleBarHeight, Math.Max(geometryHeight - 1, 0));

	internal bool IsVisible => TitleBarHeight > 0 || ResizeMargin > 0;

	// Decoration surface rectangle relative to the content surface origin.
	internal int SurfaceX => -ResizeMargin;
	internal int SurfaceY => -ResizeMargin - TitleBarHeight;
	internal int SurfaceWidth => ContentWidth + 2 * ResizeMargin;
	internal int SurfaceHeight => ContentHeight + TitleBarHeight + 2 * ResizeMargin;

	// xdg_surface.set_window_geometry, relative to the content surface origin.
	internal int GeometryY => -TitleBarHeight;
	internal int GeometryHeight => ContentHeight + TitleBarHeight;

	// Button rectangle in decoration surface coordinates, laid out right to left: close, maximize, minimize.
	internal bool TryGetButton(WaylandDecorationPart part, out int x, out int y, out int width, out int height)
	{
		x = y = width = height = 0;
		if (TitleBarHeight <= 0)
		{
			return false;
		}
		var right = ResizeMargin + ContentWidth;
		foreach (var (button, buttonPart) in _buttonOrder)
		{
			if ((Buttons & button) == 0)
			{
				continue;
			}
			right -= ButtonWidth;
			if (buttonPart == part)
			{
				x = Math.Max(right, ResizeMargin);
				y = ResizeMargin;
				width = right + ButtonWidth - x;
				height = TitleBarHeight;
				return width > 0;
			}
		}
		return false;
	}

	// Left edge of the caption buttons, in decoration surface coordinates.
	internal int ButtonsLeft => Math.Max(ResizeMargin + ContentWidth - BitOperations.PopCount((uint)Buttons) * ButtonWidth, ResizeMargin);

	// Hit test in decoration surface coordinates; edges use xdg_toplevel.resize_edge values.
	internal (WaylandDecorationPart Part, uint Edges) HitTest(double x, double y)
	{
		var gx = x - ResizeMargin;
		var gy = y - ResizeMargin;
		var width = ContentWidth;
		var height = GeometryHeight;
		if (gx < 0 || gy < 0 || gx >= width || gy >= height)
		{
			if (ResizeMargin <= 0 || gx < -ResizeMargin || gy < -ResizeMargin || gx >= width + ResizeMargin || gy >= height + ResizeMargin)
			{
				return (WaylandDecorationPart.None, 0);
			}
			uint edges = 0;
			var left = gx < 0;
			var right = gx >= width;
			var top = gy < 0;
			var bottom = gy >= height;
			if (top || bottom)
			{
				left |= gx < Math.Min(CornerSize, width / 2.0);
				right |= !left && gx >= width - Math.Min(CornerSize, width / 2.0);
			}
			if (left || right)
			{
				top |= gy < Math.Min(CornerSize, height / 2.0);
				bottom |= !top && gy >= height - Math.Min(CornerSize, height / 2.0);
			}
			edges |= top ? 1u : 0;
			edges |= bottom ? 2u : 0;
			edges |= left ? 4u : 0;
			edges |= right ? 8u : 0;
			return (WaylandDecorationPart.ResizeEdge, edges);
		}
		if (gy >= TitleBarHeight)
		{
			return (WaylandDecorationPart.None, 0);
		}
		foreach (var (_, part) in _buttonOrder)
		{
			if (TryGetButton(part, out var bx, out _, out var bw, out _) && x >= bx && x < bx + bw)
			{
				return (part, 0);
			}
		}
		return (WaylandDecorationPart.TitleBar, 0);
	}
}
