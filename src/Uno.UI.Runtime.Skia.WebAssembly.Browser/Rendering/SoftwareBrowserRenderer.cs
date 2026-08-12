using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices.JavaScript;
using Uno.Foundation.Logging;
using Uno.UI.Composition.Drawing;

namespace Uno.UI.Runtime.Skia;

// Neutral software renderer: hands the backend a neutral ISoftwareRenderTarget over the JS-owned pixel buffer
// (BGRA/RGBA). The Skia backend wraps it as its surface; Flush blits the buffer to the canvas. No Skia here.
internal partial class SoftwareBrowserRenderer : IBrowserRenderer
{
	private readonly JSObject _nativeInstance;
	private int _width;
	private int _height;

	private SoftwareBrowserRenderer(JSObject nativeInstance)
	{
		_nativeInstance = nativeInstance;
	}

	public static bool TryCreate([NotNullWhen(true)] out SoftwareBrowserRenderer? renderer)
	{
		var jsObject = NativeMethods.TryCreateInstance(WebAssemblyWindowWrapper.Instance.CanvasId);

		if (jsObject.GetPropertyAsBoolean("success"))
		{
			renderer = new SoftwareBrowserRenderer(jsObject.GetPropertyAsJSObject("instance")!);
			typeof(SoftwareBrowserRenderer).LogInfo()?.Info($"Successfully created a software rendering context.");
			return true;
		}
		else
		{
			typeof(SoftwareBrowserRenderer).LogError()?.Error($"Failed to create 2D context: {jsObject.GetPropertyAsString("error")}");
			renderer = null;
			return false;
		}
	}

	public void MakeCurrent() { }

	public IRenderTarget Resize(int width, int height)
	{
		_width = width;
		_height = height;
		var pixels = NativeMethods.ResizePixelBuffer(_nativeInstance, width, height);
		return new SoftwareRenderTarget(pixels, width * 4, width, height);
	}

	public void Flush() => NativeMethods.BlitSoftware(_nativeInstance, _width, _height);

	public bool NeedsForceResize() => !NativeMethods.IsPixelBufferValid(_nativeInstance);

	private sealed class SoftwareRenderTarget(nint pixels, int rowBytes, int width, int height) : ISoftwareRenderTarget
	{
		public nint Pixels => pixels;
		public int RowBytes => rowBytes;
		public int Width => width;
		public int Height => height;
		// The JS 2D canvas expects RGBA; the neutral software surface is filled accordingly by the backend.
		public GraphicsColorFormat ColorFormat => GraphicsColorFormat.Rgba8888;
		public void Dispose() { }
	}

	private static partial class NativeMethods
	{
		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(SoftwareBrowserRenderer)}.tryCreateInstance")]
		internal static partial JSObject TryCreateInstance(string canvasId);

		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(SoftwareBrowserRenderer)}.resizePixelBuffer")]
		internal static partial IntPtr ResizePixelBuffer(JSObject nativeSwapChainPanel, int width, int height);

		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(SoftwareBrowserRenderer)}.blitSoftware")]
		internal static partial void BlitSoftware(JSObject nativeSwapChainPanel, int width, int height);

		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(SoftwareBrowserRenderer)}.isPixelBufferValid")]
		internal static partial bool IsPixelBufferValid(JSObject nativeSwapChainPanel);
	}
}
