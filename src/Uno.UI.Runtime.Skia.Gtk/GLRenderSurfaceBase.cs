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
using Gdk;
using System.Reflection;
using Gtk;

namespace Uno.UI.Runtime.Skia
{
	internal abstract class GLRenderSurfaceBase : GLArea, IRenderSurface
	{
		private const SKColorType colorType = SKColorType.Rgba8888;
		private const GRSurfaceOrigin surfaceOrigin = GRSurfaceOrigin.BottomLeft;

		/// <summary>
		/// Include a guard band for the creation of the OpenGL surface to avoid
		/// incorrect renders when the surface is exactly the size of the GLArea.
		/// </summary>
		private const int GuardBand = 32;

		private readonly DisplayInformation _displayInformation;
		private FocusManager? _focusManager;

		private float? _dpi = 1;
		private GRContext? _grContext;
		private GRBackendRenderTarget? _renderTarget;
		private SKSurface? _surface;

		public GLRenderSurfaceBase()
		{
			_displayInformation = DisplayInformation.GetForCurrentView();
			_displayInformation.DpiChanged += OnDpiChanged;
			WUX.Window.InvalidateRender
				+= () =>
				{
					// TODO Uno: Make this invalidation less often if possible.
					InvalidateOverlays();
					QueueRender();
				};

			// Set some event handlers
			Render += UnoGLDrawingArea_Render;
			Realized += GLRenderSurface_Realized;

			HasDepthBuffer = false;
			HasStencilBuffer = false;
			AutoRender = true;
		}

		private void GLRenderSurface_Realized(object? sender, EventArgs e)
		{
			Context.MakeCurrent();
		}

		private void UnoGLDrawingArea_Render(object o, RenderArgs args)
		{
			args.Context.MakeCurrent();

			// create the contexts if not done already
			if (_grContext == null)
			{
				_grContext = TryBuildGRContext();
			}

			// manage the drawing surface
			var res = (int)Math.Max(1.0, Screen.Resolution / 96.0);
			var w = Math.Max(0, AllocatedWidth * res);
			var h = Math.Max(0, AllocatedHeight * res);

			if (_renderTarget == null || _surface == null || _renderTarget.Width != w || _renderTarget.Height != h)
			{
				// create or update the dimensions
				_renderTarget?.Dispose();

				var (framebuffer, stencil, samples) = GetGLBuffers();
				var maxSamples = _grContext.GetMaxSurfaceSampleCount(colorType);

				if (samples > maxSamples)
				{
					samples = maxSamples;
				}

				var glInfo = new GRGlFramebufferInfo((uint)framebuffer, colorType.ToGlSizedFormat());

				_renderTarget = new GRBackendRenderTarget(w + GuardBand, h + GuardBand, samples, stencil, glInfo);

				// create the surface
				_surface?.Dispose();
				_surface = SKSurface.Create(_grContext, _renderTarget, surfaceOrigin, colorType);

				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"Recreate render surface {w}x{h} colorType:{colorType} sample:{samples}");
				}
			}

			GLClear();

			var canvas = _surface.Canvas;

			using (new SKAutoCanvasRestore(canvas, true))
			{
				canvas.Clear(SKColors.White);
				canvas.Translate(new SKPoint(0, GuardBand));

				WUX.Window.Current.Compositor.Render(_surface);
			}

			// update the control
			canvas.Flush();

			GLFlush();
		}

		protected abstract void GLClear();

		protected abstract void GLFlush();

		protected abstract (int framebuffer, int stencil, int samples)GetGLBuffers();

		protected abstract GRContext TryBuildGRContext();

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

		public void TakeScreenshot(string filePath)
		{
			if (_surface != null)
			{
				using Stream memStream = File.Open(filePath, FileMode.Create, FileAccess.Write, FileShare.None);

				var image = _surface.Snapshot();
				var pngData = image.Encode();
				pngData.SaveTo(memStream);
			}
		}

		private void UpdateDpi() => _dpi = (float)_displayInformation.RawPixelsPerViewPixel;
	}
}
