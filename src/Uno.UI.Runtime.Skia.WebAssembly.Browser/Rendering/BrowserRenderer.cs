using System;
using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;

namespace Uno.UI.Runtime.Skia;

internal partial class BrowserRenderer
{
	private readonly IXamlRootHost _host;
	private int _renderCount;

	private const int ResourceCacheBytes = 256 * 1024 * 1024; // 256 MB
	private const SKColorType colorType = SKColorType.Rgba8888;
	private const GRSurfaceOrigin surfaceOrigin = GRSurfaceOrigin.BottomLeft;

	private readonly JSObject _nativeSwapChainPanel;
	private readonly Stopwatch _renderStopwatch = new Stopwatch();

	private GRGlInterface? _glInterface;
	private GRContext? _context;
	private JsInfo? _jsInfo;
	private GRGlFramebufferInfo _glInfo;
	private GRBackendRenderTarget? _renderTarget;
	private SKBitmap? _bitmap;
	private SKSurface? _surface;
	private SKCanvas? _canvas;

	public BrowserRenderer(IXamlRootHost host)
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Initializing Renderer");
		}

		_host = host;

		_nativeSwapChainPanel = NativeMethods.CreateInstance(this);
	}

	internal void InvalidateRender()
	{
		NativeMethods.Invalidate(_nativeSwapChainPanel);
	}

	[JSExport]
	internal static void RenderFrame([JSMarshalAs<JSType.Any>] object instance)
	{
		if (instance is BrowserRenderer panel)
		{
			panel.RenderFrame();
		}
	}

	private void RenderFrame()
	{
		if (_jsInfo is null)
		{
			_jsInfo = NativeMethods.CreateContext(this, _nativeSwapChainPanel, WebAssemblyWindowWrapper.Instance?.CanvasId ?? "invalid");
		}

		_renderStopwatch.Restart();

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Render {_renderCount++}");
		}

		// create the SkiaSharp context
		if (!_jsInfo.Value.UseSoftware && _context == null)
		{
			_glInterface = GRGlInterface.Create();
			_context = GRContext.CreateGl(_glInterface);

			// bump the default resource cache limit
			_context.SetResourceCacheLimit(ResourceCacheBytes);
		}

		var currentClipPath = ((CompositionTarget)_host.RootElement!.Visual.CompositionTarget!).OnNativePlatformFrameRequested(_surface?.Canvas, size =>
		{
			if (_jsInfo.Value.UseSoftware)
			{
				_bitmap?.Dispose();
				_canvas?.Dispose();
				var pixels = NativeMethods.ResizePixelBuffer(_nativeSwapChainPanel, (int)size.Width, (int)size.Height);
				_bitmap = new SKBitmap();
				_bitmap.InstallPixels(new SKImageInfo((int)size.Width, (int)size.Height, colorType, SKAlphaType.Premul), pixels);
				_canvas = new SKCanvas(_bitmap);
			}
			else
			{
				_glInfo = new GRGlFramebufferInfo(_jsInfo.Value.FboId, colorType.ToGlSizedFormat());

				// destroy the old surface
				_surface?.Dispose();
				_surface = null;
				_canvas = null;

				// re-create the render target
				_renderTarget?.Dispose();
				_renderTarget = new GRBackendRenderTarget((int)size.Width, (int)size.Height, _jsInfo.Value.Samples, _jsInfo.Value.Stencil, _glInfo);

				// create the surface
				_surface = SKSurface.Create(_context, _renderTarget, surfaceOrigin, colorType);
				_canvas = _surface.Canvas;
			}

			return _canvas;
		});

		if (_jsInfo.Value.UseSoftware)
		{
			if (_canvas is not null)
			{
				_canvas.Flush();
				NativeMethods.BlitSoftware(_nativeSwapChainPanel, _bitmap!.Width, _bitmap.Height);
			}
		}
		else
		{
			_context!.Flush();
		}

		var (path, fillType) = !currentClipPath.IsEmpty ? (currentClipPath.ToSvgPathData(), currentClipPath.FillType is SKPathFillType.EvenOdd ? "evenodd" : "nonzero") : ("", "nonzero");
		BrowserNativeElementHostingExtension.SetSvgClipPathForNativeElementHost(path, fillType);

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Render time: {_renderStopwatch.Elapsed}");
		}
	}

	internal record struct JsInfo(bool UseSoftware, uint FboId, int Stencil, int Samples, int Depth);

	internal static partial class NativeMethods
	{
		public static JSObject CreateInstance(BrowserRenderer owner)
		{
			return CreateInstanceInternal(owner);
		}

		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(BrowserRenderer)}.createInstance")]
		internal static partial JSObject CreateInstanceInternal([JSMarshalAs<JSType.Any>] object owner);

		internal static JsInfo CreateContext(BrowserRenderer owner, JSObject nativeSwapChainPanel, string canvasId)
		{
			var jsObject = CreateContextStatic(nativeSwapChainPanel, canvasId);

			if (jsObject.GetPropertyAsBoolean("success"))
			{
				return new JsInfo(
					UseSoftware: false,
					FboId: (uint)jsObject.GetPropertyAsInt32("fboId"),
					Stencil: jsObject.GetPropertyAsInt32("stencil"),
					Samples: jsObject.GetPropertyAsInt32("samples"),
					Depth: jsObject.GetPropertyAsInt32("depth")
				);
			}
			else
			{
				typeof(BrowserRenderer).LogError()?.Error($"Failed to create WebGL context: {jsObject.GetPropertyAsString("error")}");
				return new JsInfo() { UseSoftware = true };
			}
		}

		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(BrowserRenderer)}.createContextStatic")]
		internal static partial JSObject CreateContextStatic(JSObject nativeSwapChainPanel, string canvasId);

		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(BrowserRenderer)}.invalidate")]
		internal static partial void Invalidate(JSObject nativeSwapChainPanel);

		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(BrowserRenderer)}.resizePixelBuffer")]
		internal static partial IntPtr ResizePixelBuffer(JSObject nativeSwapChainPanel, int width, int height);

		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(BrowserRenderer)}.blitSoftware")]
		internal static partial void BlitSoftware(JSObject nativeSwapChainPanel, int width, int height);
	}
}
