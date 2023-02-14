#nullable enable

using System;
using System.IO;
using System.Runtime.InteropServices;
using Cairo;
using SkiaSharp;
using Uno.Extensions;
using Uno.UI.Xaml.Core;
using Microsoft.UI.Xaml.Input;
using WUX = Microsoft.UI.Xaml;
using Uno.Foundation.Logging;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;
using Uno.UI.Runtime.Skia.Helpers.Windows;
using Uno.UI.Runtime.Skia.Helpers.Dpi;
using Windows.Graphics.Display;
using Gtk;

namespace Uno.UI.Runtime.Skia
{
	internal class SoftwareRenderSurface : Gtk.DrawingArea, IRenderSurface
	{
		private readonly DisplayInformation _displayInformation;
		private FocusManager? _focusManager;
		private SKSurface? _surface;
		private SKBitmap? _bitmap;
		private int _bheight, _bwidth;
		private ImageSurface? _gtkSurface;
		private int renderCount;

		private float _dpi = 1;

		private readonly SKColorType _colorType;

		public SKColor BackgroundColor { get; set; }
			= SKColors.White;

		public SoftwareRenderSurface()
		{
			_displayInformation = DisplayInformation.GetForCurrentView();
			_displayInformation.DpiChanged += OnDpiChanged;

			_colorType = SKImageInfo.PlatformColorType;
			// R and B channels are inverted on macOS running on arm64 CPU and this is not detected by Skia
			if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
				{
					_colorType = SKColorType.Bgra8888;
				}
			}
		}

		public Widget Widget => this;

		public void InvalidateRender()
		{
			// TODO Uno: Make this invalidation less often if possible.
			InvalidateOverlays();
			Invalidate();
		}

		private void OnDpiChanged(DisplayInformation sender, object args) =>
			UpdateDpi();

		private void InvalidateOverlays()
		{
			_focusManager ??= VisualTree.GetFocusManagerForElement(Microsoft.UI.Xaml.Window.Current?.RootElement);
			_focusManager?.FocusRectManager?.RedrawFocusVisual();
			if (_focusManager?.FocusedElement is TextBox textBox)
			{
				textBox.TextBoxView?.Extension?.InvalidateLayout();
			}
		}

		private void Invalidate()
			=> QueueDrawArea(0, 0, 10000, 10000);

		protected override bool OnDrawn(Context cr)
		{
			Stopwatch? sw = null;

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				sw = Stopwatch.StartNew();
				this.Log().Trace($"Render {renderCount++}");
			}

			var scaledWidth = (int)(AllocatedWidth * _dpi);
			var scaledHeight = (int)(AllocatedHeight * _dpi);

			// reset the surfaces (skia/cairo) and bitmap if the size has changed
			if (_surface == null || scaledWidth != _bwidth || scaledHeight != _bheight)
			{
				_gtkSurface?.Dispose();
				_surface?.Dispose();
				_bitmap?.Dispose();

				var info = new SKImageInfo(scaledWidth, scaledHeight, _colorType, SKAlphaType.Premul);
				_bitmap = new SKBitmap(info);
				_bwidth = _bitmap.Width;
				_bheight = _bitmap.Height;
				var pixels = _bitmap.GetPixels(out _);
				_surface = SKSurface.Create(info, pixels);
				_gtkSurface = new ImageSurface(pixels, Format.Argb32, _bwidth, _bheight, _bwidth * 4);
			}

			var canvas = _surface.Canvas;

			using (new SKAutoCanvasRestore(canvas, true))
			{
				canvas.Clear(BackgroundColor);
				canvas.Scale(_dpi);

				WUX.Window.Current.Compositor.Render(_surface);
			}

			_gtkSurface!.MarkDirty();
			cr.Save();
			cr.Scale(1 / _dpi, 1 / _dpi);
			cr.SetSourceSurface(_gtkSurface, 0, 0);
			cr.Paint();
			cr.Restore();

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				sw?.Stop();
				this.Log().Trace($"Frame: {sw?.Elapsed}");
			}

			return true;
		}

		public void TakeScreenshot(string filePath)
		{
			using Stream memStream = File.Open(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
			using SKManagedWStream wstream = new(memStream);

			_bitmap?.Encode(wstream, SKEncodedImageFormat.Png, 100);
		}

		private float UpdateDpi() => _dpi = (float)_displayInformation.RawPixelsPerViewPixel;
	}
}
