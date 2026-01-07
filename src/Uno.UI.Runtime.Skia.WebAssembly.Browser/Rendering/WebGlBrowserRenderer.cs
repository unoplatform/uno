using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices.JavaScript;
using SkiaSharp;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia;

internal partial class WebGlBrowserRenderer : IBrowserRenderer
{
	private record struct JsInfo(JSObject NativeInstance, uint FboId, int Stencil, int Samples, int Depth);

	private readonly JsInfo _jsInfo;

	private const int ResourceCacheBytes = 256 * 1024 * 1024; // 256 MB
	private const SKColorType ColorType = SKColorType.Rgba8888;
	private const GRSurfaceOrigin SurfaceOrigin = GRSurfaceOrigin.BottomLeft;

	private readonly GRContext _context;

	private GRBackendRenderTarget? _renderTarget;
	private SKSurface? _surface;

	private WebGlBrowserRenderer(JsInfo jsInfo)
	{
		_jsInfo = jsInfo;
		var glInterface = GRGlInterface.Create();
		_context = GRContext.CreateGl(glInterface);

		_context.SetResourceCacheLimit(ResourceCacheBytes);
	}

	public static bool TryCreate([NotNullWhen(true)] out WebGlBrowserRenderer? renderer)
	{
		var jsObject = NativeMethods.TryCreateInstance(WebAssemblyWindowWrapper.Instance.CanvasId);

		if (jsObject.GetPropertyAsBoolean("success"))
		{
			var jsInfo = new JsInfo(
				NativeInstance: jsObject.GetPropertyAsJSObject("instance")!,
				FboId: (uint)jsObject.GetPropertyAsInt32("fboId"),
				Stencil: jsObject.GetPropertyAsInt32("stencil"),
				Samples: jsObject.GetPropertyAsInt32("samples"),
				Depth: jsObject.GetPropertyAsInt32("depth")
			);
			renderer = new WebGlBrowserRenderer(jsInfo);
			return true;
		}
		else
		{
			typeof(WebGlBrowserRenderer).LogError()?.Error($"Failed to create WebGL context: {jsObject.GetPropertyAsString("error")}");
			renderer = null;
			return false;
		}
	}

	public void MakeCurrent() => NativeMethods.MakeCurrent(_jsInfo.NativeInstance);

	public SKCanvas Resize(int width, int height)
	{
		var glInfo = new GRGlFramebufferInfo(_jsInfo.FboId, ColorType.ToGlSizedFormat());

		_surface?.Dispose();
		_surface = null;

		_renderTarget?.Dispose();
		_renderTarget = new GRBackendRenderTarget(width, height, _jsInfo.Samples, _jsInfo.Stencil, glInfo);

		_surface = SKSurface.Create(_context, _renderTarget, SurfaceOrigin, ColorType);
		return _surface.Canvas;
	}

	public void Flush() => _context.Flush();

	private static partial class NativeMethods
	{
		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(WebGlBrowserRenderer)}.tryCreateInstance")]
		internal static partial JSObject TryCreateInstance(string canvasId);

		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(WebGlBrowserRenderer)}.makeCurrent")]
		internal static partial void MakeCurrent(JSObject nativeInstance);
	}
}
