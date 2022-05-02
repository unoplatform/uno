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
using Silk.NET.OpenGL;
using Silk.NET.Core.Loader;

namespace Uno.UI.Runtime.Skia
{

	internal class GLRenderSurface : GLArea, IRenderSurface
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
		private GL _gl;
		private GRBackendRenderTarget? _renderTarget;
		private SKSurface? _surface;

		public GLRenderSurface()
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
			SetRequiredVersion(3, 3);

			_gl = new GL(new Silk.NET.Core.Contexts.DefaultNativeContext(new GLCoreLibraryNameContainer().GetLibraryName()));
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
				var glInterface = GRGlInterface.Create();

				if(glInterface == null)
				{
					throw new InvalidOperationException("Failed to create the GRGlInterface (See https://github.com/unoplatform/uno/issues/8643)");
				}

				_grContext = GRContext.CreateGl(glInterface);

				if(_grContext == null)
				{
					throw new InvalidOperationException("Failed to create the GRContext (See https://github.com/unoplatform/uno/issues/8643)");
				}
			}

			// manage the drawing surface
			var res = (int)Math.Max(1.0, Screen.Resolution / 96.0);
			var w = Math.Max(0, AllocatedWidth * res);
			var h = Math.Max(0, AllocatedHeight * res);

			if (_renderTarget == null || _surface == null || _renderTarget.Width != w || _renderTarget.Height != h)
			{
				// create or update the dimensions
				_renderTarget?.Dispose();

				_gl.GetInteger(GLEnum.FramebufferBinding, out var framebuffer);
				_gl.GetInteger(GLEnum.Stencil, out var stencil);
				_gl.GetInteger(GLEnum.Samples, out var samples);
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

			_gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.StencilBufferBit | ClearBufferMask.DepthBufferBit);
			_gl.ClearColor(1.0f, 1.0f, 1.0f, 1.0f);

			var canvas = _surface.Canvas;

			using (new SKAutoCanvasRestore(canvas, true))
			{
				canvas.Clear(SKColors.White);
				canvas.Translate(new SKPoint(0, GuardBand));

				WUX.Window.Current.Compositor.Render(_surface);
			}

			// update the control
			canvas.Flush();

			_gl.Flush();
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

		// Extracted from https://github.com/dotnet/Silk.NET/blob/23f9bd4d67ad21c69fbd69cc38a62fb2c0ec3927/src/OpenGL/Silk.NET.OpenGL/GLCoreLibraryNameContainer.cs
		internal class GLCoreLibraryNameContainer : SearchPathContainer
		{
			/// <inheritdoc />
			public override string Linux => "libGL.so.1";

			/// <inheritdoc />
			public override string MacOS => "/System/Library/Frameworks/OpenGL.framework/OpenGL";

			/// <inheritdoc />
			public override string Android => "libGL.so.1";

			/// <inheritdoc />
			public override string IOS => "/System/Library/Frameworks/OpenGL.framework/OpenGL";

			/// <inheritdoc />
			public override string Windows64 => "opengl32.dll";

			/// <inheritdoc />
			public override string Windows86 => "opengl32.dll";
		}
	}
}
