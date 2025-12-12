using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using Uno.Foundation.Logging;
using Uno.UI.Dispatching;
using Uno.UI.Helpers;
using Uno.UI.Hosting;
using Windows.Graphics.Display;

namespace Uno.UI.Runtime.Skia;

internal partial class BrowserRenderer
{
	private readonly IXamlRootHost _host;
	private int _renderCount;
	private bool _isWindowInitialized;

	private const int ResourceCacheBytes = 256 * 1024 * 1024; // 256 MB
	private const SKColorType colorType = SKColorType.Rgba8888;
	private const GRSurfaceOrigin surfaceOrigin = GRSurfaceOrigin.BottomLeft;

	private readonly JSObject _nativeSwapChainPanel;
	private readonly Stopwatch _renderStopwatch = new Stopwatch();

	private GRGlInterface? _glInterface;
	private GRContext? _context;
	private JsInfo _jsInfo;
	private GRGlFramebufferInfo _glInfo;
	private GRBackendRenderTarget? _renderTarget;
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

	private void Initialize()
	{
		_jsInfo = NativeMethods.CreateContext(this, _nativeSwapChainPanel, WebAssemblyWindowWrapper.Instance?.CanvasId ?? "invalid");
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
		if (!_jsInfo.IsValid)
		{
			Initialize();
		}

		_renderStopwatch.Restart();

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Render {_renderCount++}");
		}

		// create the SkiaSharp context
		if (_context == null)
		{
			_glInterface = GRGlInterface.Create();
			_context = GRContext.CreateGl(_glInterface);

			// bump the default resource cache limit
			_context.SetResourceCacheLimit(ResourceCacheBytes);
		}

		var currentClipPath = ((CompositionTarget)_host.RootElement!.Visual.CompositionTarget!).OnNativePlatformFrameRequested(_surface?.Canvas, size =>
		{
			_glInfo = new GRGlFramebufferInfo(_jsInfo.FboId, colorType.ToGlSizedFormat());

			// destroy the old surface
			_surface?.Dispose();
			_surface = null;
			_canvas = null;

			// re-create the render target
			_renderTarget?.Dispose();
			_renderTarget = new GRBackendRenderTarget((int)size.Width, (int)size.Height, _jsInfo.Samples, _jsInfo.Stencil, _glInfo);

			// create the surface
			_surface = SKSurface.Create(_context, _renderTarget, surfaceOrigin, colorType);
			_canvas = _surface.Canvas;

			if (!_isWindowInitialized)
			{
				_isWindowInitialized = true;
				// Microsoft.UI.Xaml.Window.Current.OnNativeWindowCreated();
			}

			return _canvas;
		});

		_context.Flush();

		var (path, fillType) = !currentClipPath.IsEmpty ? (currentClipPath.ToSvgPathData(), currentClipPath.FillType is SKPathFillType.EvenOdd ? "evenodd" : "nonzero") : ("", "nonzero");
		BrowserNativeElementHostingExtension.SetSvgClipPathForNativeElementHost(path, fillType);

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Render time: {_renderStopwatch.Elapsed}");
		}
	}

	internal struct JsInfo
	{
		public bool IsValid { get; set; }

		public int ContextId { get; set; }

		public uint FboId { get; set; }

		public int Stencil { get; set; }

		public int Samples { get; set; }

		public int Depth { get; set; }
	}

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
			var jsInfo = new JsInfo();
			var jsObject = CreateContextInternal(nativeSwapChainPanel, canvasId);

			jsInfo.IsValid = true;
			jsInfo.ContextId = jsObject.GetPropertyAsInt32("contextId");
			jsInfo.FboId = (uint)jsObject.GetPropertyAsInt32("fboId");
			jsInfo.Stencil = jsObject.GetPropertyAsInt32("stencil");
			jsInfo.Samples = jsObject.GetPropertyAsInt32("samples");
			jsInfo.Depth = jsObject.GetPropertyAsInt32("depth");
			return jsInfo;
		}

		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(BrowserRenderer)}.createContextStatic")]
		internal static partial JSObject CreateContextInternal(JSObject nativeSwapChainPanel, string canvasId);

		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(BrowserRenderer)}.invalidate")]
		internal static partial void Invalidate(JSObject nativeSwapChainPanel);
	}
}
