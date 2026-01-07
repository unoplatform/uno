using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices.JavaScript;
using SkiaSharp;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia;

internal partial class SoftwareBrowserRenderer : IBrowserRenderer
{
	private const SKColorType ColorType = SKColorType.Rgba8888;

	private readonly JSObject _nativeInstance;
	private SKBitmap? _bitmap;

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

	public SKCanvas Resize(int width, int height)
	{
		_bitmap?.Dispose();
		var pixels = NativeMethods.ResizePixelBuffer(_nativeInstance, width, height);
		_bitmap = new SKBitmap();
		_bitmap.InstallPixels(new SKImageInfo(width, height, ColorType, SKAlphaType.Premul), pixels);
		return new SKCanvas(_bitmap);
	}

	public void Flush() => NativeMethods.BlitSoftware(_nativeInstance, _bitmap!.Width, _bitmap.Height);

	private static partial class NativeMethods
	{
		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(SoftwareBrowserRenderer)}.tryCreateInstance")]
		internal static partial JSObject TryCreateInstance(string canvasId);

		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(SoftwareBrowserRenderer)}.resizePixelBuffer")]
		internal static partial IntPtr ResizePixelBuffer(JSObject nativeSwapChainPanel, int width, int height);

		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(SoftwareBrowserRenderer)}.blitSoftware")]
		internal static partial void BlitSoftware(JSObject nativeSwapChainPanel, int width, int height);
	}
}
