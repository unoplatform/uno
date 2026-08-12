using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices.JavaScript;
using Uno.Foundation.Logging;
using Uno.UI.Composition.Drawing;

namespace Uno.UI.Runtime.Skia;

// Neutral WebGL renderer: makes the emscripten WebGL context current and hands the backend a neutral
// IGLRenderTarget (the canvas default framebuffer). The Skia backend builds its GRContext-GL against the
// current context. No Skia/GR type lives here.
internal partial class WebGlBrowserRenderer : IBrowserRenderer
{
	private record struct JsInfo(JSObject NativeInstance, uint FboId, int Stencil, int Samples, int Depth);

	private readonly JsInfo _jsInfo;

	private WebGlBrowserRenderer(JsInfo jsInfo)
	{
		_jsInfo = jsInfo;
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
			typeof(WebGlBrowserRenderer).LogInfo()?.Info($"WebGL context created successfully: {jsInfo}");
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

	public IRenderTarget Resize(int width, int height)
		=> new WebGlRenderTarget(_jsInfo.FboId, _jsInfo.Samples, _jsInfo.Stencil, width, height);

	public void Flush() { }

	public bool NeedsForceResize() => false;

	private sealed class WebGlRenderTarget(uint framebufferId, int sampleCount, int stencilBits, int width, int height) : IGLRenderTarget
	{
		public uint FramebufferId => framebufferId;
		public int SampleCount => sampleCount;
		public int StencilBits => stencilBits;
		public int Width => width;
		public int Height => height;
		public GraphicsColorFormat ColorFormat => GraphicsColorFormat.Rgba8888;
		public void Dispose() { }
	}

	private static partial class NativeMethods
	{
		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(WebGlBrowserRenderer)}.tryCreateInstance")]
		internal static partial JSObject TryCreateInstance(string canvasId);

		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(WebGlBrowserRenderer)}.makeCurrent")]
		internal static partial void MakeCurrent(JSObject nativeInstance);
	}
}
