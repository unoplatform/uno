using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices.JavaScript;
using Uno.Foundation.Logging;
using Uno.UI.Composition.Drawing;
using Uno.UI.Runtime.WebAssembly.Browser.Graphics;

namespace Uno.UI.Runtime;

// Makes the emscripten WebGL context current and hands the backend a neutral IGLRenderTarget (the canvas
// default framebuffer).
internal partial class WebGlBrowserRenderer : IBrowserRenderer
{
	private record struct JsInfo(JSObject NativeInstance, uint FboId, int Stencil, int Samples, int Depth, bool UsesWorkerClock);

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
				Depth: jsObject.GetPropertyAsInt32("depth"),
				UsesWorkerClock: jsObject.GetPropertyAsBoolean("usesWorkerClock")
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

	// On the page canvas the browser presents on its own; with the worker clock, the drawn frame is handed over.
	public void Flush()
	{
		if (_jsInfo.UsesWorkerClock)
		{
			NativeMethods.Present(_jsInfo.NativeInstance);
		}
	}

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
		[JSImport($"globalThis.Uno.UI.Runtime.{nameof(WebGlBrowserRenderer)}.tryCreateInstance")]
		internal static partial JSObject TryCreateInstance(string canvasId);

		[JSImport($"globalThis.Uno.UI.Runtime.{nameof(WebGlBrowserRenderer)}.makeCurrent")]
		internal static partial void MakeCurrent(JSObject nativeInstance);

		[JSImport($"globalThis.Uno.UI.Runtime.{nameof(WebGlBrowserRenderer)}.present")]
		internal static partial void Present(JSObject nativeInstance);
	}
}
