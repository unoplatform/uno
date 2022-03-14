using System;
using System.IO;
using SkiaSharp;
using Uno.Extensions;
using Uno.UI.Xaml.Core;
using Windows.UI.Xaml.Input;
using WUX = Windows.UI.Xaml;
using Uno.Foundation.Logging;
using Windows.UI.Xaml.Controls;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Uno.UI.Runtime.Skia.Helpers.Windows;
using Uno.UI.Runtime.Skia.Helpers.Dpi;
using Windows.Graphics.Display;

namespace Uno.UI.Runtime.Skia
{
	internal class SoftwareRenderSurface : Gtk.DrawingArea, IRenderSurface
	{
		private readonly DisplayInformation _displayInformation;
		private FocusManager _focusManager;
		private SKBitmap bitmap;
		private int renderCount;

		private float? _dpi = 1;

		public SoftwareRenderSurface()
		{
			_displayInformation = DisplayInformation.GetForCurrentView();
			_displayInformation.DpiChanged += OnDpiChanged;
			WUX.Window.InvalidateRender
				+= () =>
				{
					// TODO Uno: Make this invalidation less often if possible.
					InvalidateOverlays();
					Invalidate();
				};
		}

		private void OnDpiChanged(DisplayInformation sender, object args) =>
			UpdateDpi();

		private void InvalidateOverlays()
		{
			_focusManager ??= VisualTree.GetFocusManagerForElement(Windows.UI.Xaml.Window.Current?.RootElement);
			_focusManager?.FocusRectManager?.RedrawFocusVisual();
			if (_focusManager?.FocusedElement is TextBox textBox)
			{
				textBox.TextBoxView?.Extension?.InvalidateLayout();
			}
		}

		private void Invalidate()
			=> QueueDrawArea(0, 0, 10000, 10000);

		protected override bool OnDrawn(Cairo.Context cr)
		{
			var sw = Stopwatch.StartNew();

			int width, height;

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"Render {renderCount++}");
			}

			if (_dpi == null)
			{
				UpdateDpi();
			}
			
			width = (int)AllocatedWidth;
			height = (int)AllocatedHeight;

			var scaledWidth = (int)(width * _dpi.Value);
			var scaledHeight = (int)(height * _dpi.Value);

			var info = new SKImageInfo(scaledWidth, scaledHeight, SKImageInfo.PlatformColorType, SKAlphaType.Premul);

			// reset the bitmap if the size has changed
			if (bitmap == null || info.Width != bitmap.Width || info.Height != bitmap.Height)
			{
				bitmap = new SKBitmap(scaledWidth, scaledHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
			}

			using (var surface = SKSurface.Create(info, bitmap.GetPixels(out _)))
			{
				surface.Canvas.Clear(SKColors.White);

				surface.Canvas.Scale((float)_dpi);

				WUX.Window.Current.Compositor.Render(surface);

				using (var gtkSurface = new Cairo.ImageSurface(
					bitmap.GetPixels(out _),
					Cairo.Format.Argb32,
					bitmap.Width, bitmap.Height,
					bitmap.Width * 4))
				{
					gtkSurface.MarkDirty();
					cr.Scale(1 / _dpi.Value, 1 / _dpi.Value);
					cr.SetSourceSurface(gtkSurface, 0, 0);
					cr.Paint();
				}
			}

			sw.Stop();
			Console.WriteLine($"Frame: {sw.Elapsed}");

			return true;
		}

		public void TakeScreenshot(string filePath)
		{
			using Stream memStream = File.Open(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
			using SKManagedWStream wstream = new SKManagedWStream(memStream);

			bitmap.Encode(wstream, SKEncodedImageFormat.Png, 100);
		}

		private void UpdateDpi() => _dpi = (float)_displayInformation.RawPixelsPerViewPixel;
	}
}
