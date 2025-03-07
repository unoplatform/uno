#nullable enable

using System;
using System.IO;
using SkiaSharp;
using Uno.UI.Xaml.Core;
using Windows.UI.Xaml.Input;
using WUX = Windows.UI.Xaml;
using Uno.Foundation.Logging;
using Windows.UI.Xaml.Controls;
using Windows.Graphics.Display;
using Gtk;
using Uno.UI.Hosting;
using Windows.UI.Composition;
using Uno.UI.Runtime.Skia.Gtk.Hosting;
using Windows.UI.Xaml;
using Uno.UI.Helpers;

namespace Uno.UI.Runtime.Skia.Gtk
{
	internal abstract partial class GLRenderSurfaceBase : GLArea, IGtkRenderer
	{
		private const SKColorType colorType = SKColorType.Rgba8888;
		private const GRSurfaceOrigin surfaceOrigin = GRSurfaceOrigin.BottomLeft;

		/// <summary>
		/// Include a guard band for the creation of the OpenGL surface to avoid
		/// incorrect renders when the surface is exactly the size of the GLArea.
		/// The current GLArea seems to have an issue with OpenGL rendering and 
		/// the first line rendering from the top. Adding one offsets the rendering
		/// and prevents full area Skia surfaces to be blank, but causes issues in
		/// rendering clipped shapes. Placing the GuardBand to one reduces but does
		/// not remove the artifacts. See https://github.com/unoplatform/uno/issues/11139.
		/// </summary>
		private const int GuardBand = 1;

		private readonly IGtkXamlRootHost _host;

		private float? _scale = 1;
		private GRContext? _grContext;
		private GRBackendRenderTarget? _renderTarget;
		private SKSurface? _surface;

		/// <summary>
		/// This field determines if OpenGL ES shouls be used or not.
		/// </summary>
		/// <remarks>
		/// In order to avoid virtual calls to <see cref="ClearOpenGL"/> and <see cref="FlushOpenGL"/> for performance reasons.
		/// </remarks>
		protected bool _isGLES;
		private readonly XamlRoot _xamlRoot;

		public SKColor BackgroundColor { get; set; }

		public GLRenderSurfaceBase(IGtkXamlRootHost host)
		{
			_xamlRoot = GtkManager.XamlRootMap.GetRootForHost(host) ?? throw new InvalidOperationException("XamlRoot must not be null when renderer is initialized");
			_xamlRoot.Changed += OnXamlRootChanged;
			UpdateDpi();

			// Set some event handlers
			Render += UnoGLDrawingArea_Render;
			Realized += GLRenderSurface_Realized;

			HasDepthBuffer = false;
			HasStencilBuffer = true;

			// AutoRender must be disabled to avoid having the GLArea re-render the
			// composition Visuals after pointer interactions, causing undefined behaviors
			// for Visuals being updated during a pointer interaction, while not having measured
			// and arranged properly.
			AutoRender = false;
			_host = host;
		}

		public Widget Widget => this;

		public void InvalidateRender() => QueueRender();

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

			var scale = _scale ?? 1f;
			var scaleAdjustment = Math.Truncate(scale);

			var scaledGuardBand = (int)(GuardBand * scale);

			var w = (int)Math.Max(0, AllocatedWidth * scaleAdjustment + scaledGuardBand);
			var h = (int)Math.Max(0, AllocatedHeight * scaleAdjustment + scaledGuardBand);

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

				_renderTarget = new GRBackendRenderTarget(w, h, samples, 8, glInfo);

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
				canvas.Clear(BackgroundColor);

				if (_scale != null)
				{
					canvas.Scale(scale);
				}
				canvas.Translate(new SKPoint(0, GuardBand));

				if (_host.RootElement?.Visual is { } rootVisual)
				{
					// OpenGL rendering on Gtk doesn't work well with transparency
					// even though stencil buffer support is present and the color includes alpha
					// SkiaRenderHelper.RenderRootVisual(w, h, rootVisual, _surface, canvas);
					Compositor.GetSharedCompositor().RenderRootVisual(_surface, rootVisual, null);
				}
			}

			// update the control
			canvas.Flush();

			GLFlush();
		}

		private void GLClear()
		{
			if (_isGLES)
			{
				ClearOpenGLES();
			}
			else
			{
				ClearOpenGL();
			}
		}

		private void GLFlush()
		{
			if (_isGLES)
			{
				FlushOpenGLES();
			}
			else
			{
				FlushOpenGL();
			}
		}

		protected abstract (int framebuffer, int stencil, int samples) GetGLBuffers();

		protected abstract GRContext TryBuildGRContext();

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

		private void OnXamlRootChanged(XamlRoot sender, XamlRootChangedEventArgs args) => UpdateDpi();

		private void UpdateDpi()
		{
			var newScale = (float)_xamlRoot.RasterizationScale;
			if (_scale != newScale)
			{
				_scale = newScale;
				InvalidateRender();
			}
		}
	}
}
