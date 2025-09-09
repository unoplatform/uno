#nullable enable
using System;
using System.Diagnostics;
using Windows.Foundation;
using SkiaSharp;
using Uno.Foundation.Logging;
using Uno.UI.Composition;
using Uno.UI.Dispatching;
using Uno.UI.Helpers;
using Uno.UI.Hosting;

namespace Microsoft.UI.Xaml.Media;

public partial class CompositionTarget
{
	internal static (bool invertNativeElementClipPath, bool applyScalingToNativeElementClipPath) FrameRenderingOptions { get; set; } = (false, true);

	private readonly SkiaRenderHelper.FpsHelper _fpsHelper = new();
	private readonly object _frameGate = new();

	// Only read and set from the native rendering thread in OnNativePlatformFrameRequested
	private Size _lastCanvasSize = Size.Empty;
	// only set on the UI thread and under _frameGate, only read under _frameGate
	private (SKPicture frame, SKPath nativeElementClipPath, Size size)? _lastRenderedFrame;

	internal event Action? FrameRendered;

	private void Render()
	{
		this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()}: {nameof(Render)} begins with timestamp {Stopwatch.GetTimestamp()}");

		NativeDispatcher.CheckThreadAccess();

		var rootElement = ContentRoot.VisualTree.RootElement;
		var bounds = ContentRoot.VisualTree.Size;
		var scale = ContentRoot.XamlRoot?.RasterizationScale ?? 1;

		var (picture, path) = SkiaRenderHelper.RecordPictureAndReturnPath(
			(int)(bounds.Width * scale),
			(int)(bounds.Height * scale),
			rootElement,
			invertPath: FrameRenderingOptions.invertNativeElementClipPath,
			applyScaling: FrameRenderingOptions.applyScalingToNativeElementClipPath);
		var lastRenderedFrame = (picture, path, new Size((int)(bounds.Width * scale), (int)(bounds.Height * scale)));
		lock (_frameGate)
		{
			_lastRenderedFrame = lastRenderedFrame;
		}

		if (IsRenderingActive)
		{
			((ICompositionTarget)this).RequestNewFrame();
		}

		FrameRendered?.Invoke();
		if (rootElement.XamlRoot is not null)
		{
			XamlRootMap.GetHostForRoot(rootElement.XamlRoot)?.InvalidateRender();
		}

		this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()}: {nameof(Render)} ends");
	}

	private SKPath Draw(SKCanvas? canvas, Func<Size, SKCanvas> resizeFunc)
	{
		this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()}: {nameof(Draw)}");

		(SKPicture frame, SKPath nativeElementClipPath, Size size)? lastRenderedFrameNullable;
		lock (_frameGate)
		{
			lastRenderedFrameNullable = _lastRenderedFrame;
		}

		if (lastRenderedFrameNullable is not { } lastRenderedFrame)
		{
			return new SKPath();
		}
		else
		{
			if (canvas is null || _lastCanvasSize != lastRenderedFrame.size)
			{
				canvas = resizeFunc(lastRenderedFrame.size);
				_lastCanvasSize = lastRenderedFrame.size;
			}

			SkiaRenderHelper.RenderPicture(
				canvas,
				lastRenderedFrame.frame,
				SKColors.Transparent,
				_fpsHelper);

			InvokeRendering();

			return lastRenderedFrame.nativeElementClipPath;
		}
	}
}
