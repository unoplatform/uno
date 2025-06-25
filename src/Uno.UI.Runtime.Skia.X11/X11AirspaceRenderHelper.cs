using System;
using System.Runtime.InteropServices;
using SkiaSharp;
using Uno.Foundation.Logging;

namespace Uno.WinUI.Runtime.Skia.X11;

// https://www.x.org/releases/X11R7.6/doc/xextproto/shape.html
// Thanks to Jörg Seebohn for providing an example on how to use X SHAPE
// https://gist.github.com/je-so/903479/834dfd78705b16ec5f7bbd10925980ace4049e17
internal class X11AirspaceRenderHelper : IDisposable
{
	private const int BitsPerByte = 8;
	private static bool? _xShapesPresent;

	private readonly SKBitmap _maskBitmap;
	private readonly SKSurface _maskSurface;
	private readonly IntPtr _maskData;
	private readonly int _width;
	private readonly int _height;
	private readonly int _bytesPerMaskScanline;
	private readonly IntPtr _display;
	private readonly IntPtr _window;
	private string? _lastSvgClipPath;

	public X11AirspaceRenderHelper(IntPtr display, IntPtr window, int width, int height)
	{
		_xShapesPresent ??= X11Helper.XShapeQueryExtension(display, out _, out _);
		if (!_xShapesPresent.Value)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error("The X Shape extension is not supported on this X server. Skipping airspace clipping.");
			}
		}

		_display = display;
		_window = window;
		_width = width;
		_height = height;
		var info = new SKImageInfo(width, height, SKColorType.Alpha8);
		_maskBitmap = new SKBitmap(info);
		_maskSurface = SKSurface.Create(info, _maskBitmap.GetPixels());
		_bytesPerMaskScanline = (width + (BitsPerByte - 1)) / BitsPerByte; // round up
		_maskData = Marshal.AllocHGlobal(height * _bytesPerMaskScanline);
	}

	public unsafe void XShapeClip(SKPath path)
	{
		if (!_xShapesPresent!.Value)
		{
			return;
		}

		if (path.ToSvgPathData() is var svgPathData && svgPathData == _lastSvgClipPath)
		{
			return;
		}

		_lastSvgClipPath = svgPathData;

		using var xLock = X11Helper.XLock(_display);

		var maskCanvas = _maskSurface.Canvas;
		using (new SKAutoCanvasRestore(maskCanvas, true))
		{
			maskCanvas.Clear(SKColor.Empty);
			maskCanvas.ClipPath(path, operation: SKClipOperation.Intersect, antialias: true);
			maskCanvas.Clear(SKColors.White);
			maskCanvas.Flush();
			_maskSurface.Snapshot();

			// XShapeCombineMask requires a mask with a bit depth of 1. Unfortunately, skia doesn't support
			// a 1-bit color type, so we have to loop over the pixels.
			var pixels = (byte*)_maskBitmap.GetPixels();
			var scanline = (byte*)_maskData.ToPointer();
			for (var y = 0; y < _height; y++, scanline += _bytesPerMaskScanline, pixels += _maskBitmap.RowBytes)
			{
				for (var x = 0; x < _width; x++)
				{
					var val = (byte)(pixels[x] == 255 ? 1 : 0);
					var temp = scanline[x / BitsPerByte];
					scanline[x / BitsPerByte] = (byte)((((temp & ~(1 << (x % BitsPerByte))) | (val << (x % BitsPerByte))))); // set bit to val
				}
			}

			var pixmap = X11Helper.XCreateBitmapFromData(_display, _window, _maskData, (uint)_width, (uint)_height);
			X11Helper.XShapeCombineMask(_display, _window, X11Helper.ShapeBounding, 0, 0, pixmap, X11Helper.ShapeSet);
			X11Helper.XShapeCombineMask(_display, _window, X11Helper.ShapeInput, 0, 0, pixmap, X11Helper.ShapeSet);

			_ = X11Helper.XFreePixmap(_display, pixmap);
			_ = XLib.XSync(_display, false);
		}
	}

	public void Dispose()
	{
		Marshal.FreeHGlobal(_maskData);
		_maskSurface.Dispose();
		_maskBitmap.Dispose();
	}
}
