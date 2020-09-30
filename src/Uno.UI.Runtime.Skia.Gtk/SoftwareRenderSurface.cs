#nullable enable

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
		private FocusManager? _focusManager;
		private SKBitmap? _bitmap;
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
			Stopwatch? sw = null;

			int width, height;

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				sw = Stopwatch.StartNew();
				this.Log().Trace($"Render {renderCount++}");
			}

			var dpi = UpdateDpi();
			
			width = (int)AllocatedWidth;
			height = (int)AllocatedHeight;

			var scaledWidth = (int)(width * dpi);
			var scaledHeight = (int)(height * dpi);

			var info = new SKImageInfo(scaledWidth, scaledHeight, SKImageInfo.PlatformColorType, SKAlphaType.Premul);

			// reset the bitmap if the size has changed
			if (_bitmap == null || info.Width != _bitmap.Width || info.Height != _bitmap.Height)
			{
				_bitmap = new SKBitmap(scaledWidth, scaledHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
			}

			using (var surface = SKSurface.Create(info, _bitmap.GetPixels(out _)))
			{
				surface.Canvas.Clear(SKColors.White);

				surface.Canvas.Scale(dpi);

				WUX.Window.Current.Compositor.Render(surface);

				using (var gtkSurface = new Cairo.ImageSurface(
					_bitmap.GetPixels(out _),
					Cairo.Format.Argb32,
					_bitmap.Width, _bitmap.Height,
					_bitmap.Width * 4))
				{
					gtkSurface.MarkDirty();
					cr.Scale(1 / dpi, 1 / dpi);
					cr.SetSourceSurface(gtkSurface, 0, 0);
					cr.Paint();
				}
			}

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

		private float UpdateDpi() => _dpi ??= (float)_displayInformation.RawPixelsPerViewPixel;
	}
}
