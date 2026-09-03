#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using Uno.Foundation.Logging;
using Uno.UI.Composition.Drawing;

namespace Uno.WinUI.Runtime.Skia.X11;

// https://www.x.org/releases/X11R7.6/doc/xextproto/shape.html
// Thanks to Jörg Seebohn for providing an example on how to use X SHAPE:
// https://gist.github.com/je-so/903479/834dfd78705b16ec5f7bbd10925980ace4049e17
//
// Shapes the top render window to the native-element clip region so the native X11 sub-windows (siblings raised
// below the render window) show through the holes. The clip is a neutral IGeometry, rasterized here into a 1-bit
// XShape mask with a scanline fill — no SkiaSharp, keeping the X11 host backend-agnostic.
internal sealed class X11AirspaceRenderHelper : IDisposable
{
	private const int BitsPerByte = 8;
	private static bool? _xShapesPresent;

	private readonly IntPtr _display;
	private readonly IntPtr _window;
	private readonly int _width;
	private readonly int _height;
	private readonly int _bytesPerScanline;
	private readonly IntPtr _maskData;
	private readonly List<(float x, int dir)> _crossings = new();
	private readonly FlattenSink _sink = new();
	private string? _lastSvgClipPath;

	public X11AirspaceRenderHelper(IntPtr display, IntPtr window, int width, int height)
	{
		using var xLock = X11Helper.XLock(display);
		_xShapesPresent ??= X11Helper.XShapeQueryExtension(display, out _, out _);
		if (_xShapesPresent == false && this.Log().IsEnabled(LogLevel.Error))
		{
			this.Log().Error("The X Shape extension is not supported on this X server. Skipping airspace clipping.");
		}

		_display = display;
		_window = window;
		_width = width;
		_height = height;
		_bytesPerScanline = (width + (BitsPerByte - 1)) / BitsPerByte; // round up
		_maskData = Marshal.AllocHGlobal(height * _bytesPerScanline);
	}

	public unsafe void XShapeClip(IGeometry path)
	{
		if (_xShapesPresent != true)
		{
			return;
		}

		if (path.ToSvgPathData() is var svg && svg == _lastSvgClipPath)
		{
			return;
		}
		_lastSvgClipPath = svg;

		using var xLock = X11Helper.XLock(_display);

		if (path.IsEmpty)
		{
			// No holes: reset the window to its full rectangular shape (None mask == unshaped).
			X11Helper.XShapeCombineMask(_display, _window, X11Helper.ShapeBounding, 0, 0, IntPtr.Zero, X11Helper.ShapeSet);
			X11Helper.XShapeCombineMask(_display, _window, X11Helper.ShapeInput, 0, 0, IntPtr.Zero, X11Helper.ShapeSet);
			_ = XLib.XSync(_display, false);
			return;
		}

		var mask = (byte*)_maskData;
		new Span<byte>(mask, _height * _bytesPerScanline).Clear();

		_sink.Reset();
		path.StreamFlattened(_sink);
		Rasterize(_sink.Contours, path.FillRule, mask);

		var pixmap = X11Helper.XCreateBitmapFromData(_display, _window, _maskData, (uint)_width, (uint)_height);
		X11Helper.XShapeCombineMask(_display, _window, X11Helper.ShapeBounding, 0, 0, pixmap, X11Helper.ShapeSet);
		X11Helper.XShapeCombineMask(_display, _window, X11Helper.ShapeInput, 0, 0, pixmap, X11Helper.ShapeSet);
		_ = X11Helper.XFreePixmap(_display, pixmap);
		_ = XLib.XSync(_display, false);
	}

	// Scanline polygon fill of the flattened contours into the 1-bit mask (bit set == inside == Uno visible),
	// honoring the geometry's fill rule. Contours are treated as implicitly closed.
	private unsafe void Rasterize(List<List<Vector2>> contours, GeometryFillRule fillRule, byte* mask)
	{
		for (var y = 0; y < _height; y++)
		{
			var yc = y + 0.5f;
			_crossings.Clear();

			foreach (var contour in contours)
			{
				var n = contour.Count;
				for (var i = 0; i < n; i++)
				{
					var a = contour[i];
					var b = contour[(i + 1) % n];
					var y0 = a.Y;
					var y1 = b.Y;
					if (y0 == y1)
					{
						continue; // horizontal edge contributes no crossing
					}
					if ((yc >= y0 && yc < y1) || (yc >= y1 && yc < y0))
					{
						var t = (yc - y0) / (y1 - y0);
						_crossings.Add((a.X + t * (b.X - a.X), y1 > y0 ? 1 : -1));
					}
				}
			}

			if (_crossings.Count < 2)
			{
				continue;
			}

			_crossings.Sort(static (p, q) => p.x.CompareTo(q.x));
			var row = mask + y * _bytesPerScanline;

			if (fillRule == GeometryFillRule.EvenOdd)
			{
				for (var i = 0; i + 1 < _crossings.Count; i += 2)
				{
					FillSpan(row, _crossings[i].x, _crossings[i + 1].x);
				}
			}
			else // NonZero
			{
				var winding = 0;
				for (var i = 0; i + 1 < _crossings.Count; i++)
				{
					winding += _crossings[i].dir;
					if (winding != 0)
					{
						FillSpan(row, _crossings[i].x, _crossings[i + 1].x);
					}
				}
			}
		}
	}

	private unsafe void FillSpan(byte* row, float xStart, float xEnd)
	{
		var x0 = Math.Max(0, (int)MathF.Round(xStart));
		var x1 = Math.Min(_width, (int)MathF.Round(xEnd));
		for (var x = x0; x < x1; x++)
		{
			// LSB-first within each byte, matching the X bitmap format XCreateBitmapFromData expects.
			row[x / BitsPerByte] |= (byte)(1 << (x % BitsPerByte));
		}
	}

	public void Dispose() => Marshal.FreeHGlobal(_maskData);

	private sealed class FlattenSink : IFlattenedPathSink
	{
		public readonly List<List<Vector2>> Contours = new();
		private List<Vector2>? _current;

		public void Reset()
		{
			Contours.Clear();
			_current = null;
		}

		public void BeginContour(Vector2 start) => _current = new List<Vector2> { start };

		public void LineTo(Vector2 point) => _current?.Add(point);

		public void EndContour(bool closed)
		{
			if (_current is { Count: > 1 })
			{
				Contours.Add(_current);
			}
			_current = null;
		}
	}
}
