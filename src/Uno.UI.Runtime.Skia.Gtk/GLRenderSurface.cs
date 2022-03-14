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
			var sw = Stopwatch.StartNew();

			args.Context.MakeCurrent();


			// create the contexts if not done already
			if (_grContext == null)
			{
				var glInterface = GRGlInterface.Create();
				_grContext = GRContext.CreateGl(glInterface);
			}

			_gl.Clear(ClearBufferMask.ColorBufferBit);
			_gl.ClearColor(1.0f, 1.0f, 1.0f, 1.0f);

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

				_renderTarget = new GRBackendRenderTarget(w, h, samples, stencil, glInfo);

				// create the surface
				_surface?.Dispose();
				_surface = SKSurface.Create(_grContext, _renderTarget, surfaceOrigin, colorType);

				Console.WriteLine($"Recreated surfaces: {w}x{h} Samples:{samples} colorType:{colorType}");
			}

			using (new SKAutoCanvasRestore(_surface.Canvas, true))
			{
				_surface.Canvas.Clear(SKColors.White);

				// _surface.Canvas.Scale((float)(1/_dpi));

				WUX.Window.Current.Compositor.Render(_surface);
			}

			// update the control
			_surface.Canvas.Flush();

			_gl.Flush();
			sw.Stop();
			Console.WriteLine($"Frame: {sw.Elapsed}");
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
			using Stream memStream = File.Open(filePath, FileMode.Create, FileAccess.Write, FileShare.None);

			var image = _surface.Snapshot();
			var pngData = image.Encode();
			pngData.SaveTo(memStream);
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
