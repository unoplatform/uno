#nullable enable

using System;
using System.Diagnostics;
using System.Threading;
using Windows.Foundation;
using Uno.UI.Xaml.Core;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using Uno.UI.Dispatching;
using Uno.UI.Helpers;
using Uno.UI.Hosting;
using Timer = System.Timers.Timer;

namespace Microsoft.UI.Xaml;

partial class XamlRoot
{
	private readonly SkiaRenderHelper.FpsHelper _fpsHelper = new();
	// TODO: prevent this from being bottlenecked by the dispatcher queue, and read the native refresh rate instead of hardcoding it
	private readonly Timer _renderTimer;
	private readonly object _frameGate = new();
	private (SKPicture frame, SKPath nativeElementClipPath, Size size, long timestamp)? _lastRenderedFrame;
	private bool _renderQueued;
	private Size _lastCanvasSize = Size.Empty;

	private FocusManager? _focusManager;

	internal static (bool invertNativeElementClipPath, bool applyScalingToNativeElementClipPath) FrameRenderingOptions { get; set; } = (false, true);

	internal event Action? FramePainted;

	internal void InvalidateOverlays()
	{
		_focusManager ??= VisualTree.GetFocusManagerForElement(Content);
		_focusManager?.FocusRectManager?.RedrawFocusVisual();
		if (_focusManager?.FocusedElement is TextBox textBox)
		{
			textBox.TextBoxView?.Extension?.InvalidateLayout();
		}
	}

	private void PaintFrame()
	{
		var timestamp = Stopwatch.GetTimestamp();
		NativeDispatcher.CheckThreadAccess();

		var rootElement = VisualTree.RootElement;
		// TODO
		// if (!SkiaRenderHelper.CanRecordPicture(rootElement))
		// {
		// 	// Try again next tick
		// 	return;
		// }

		var (picture, path) = SkiaRenderHelper.RecordPictureAndReturnPath(
			(int)(Bounds.Width * RasterizationScale),
			(int)(Bounds.Height * RasterizationScale),
			rootElement,
			invertPath: FrameRenderingOptions.invertNativeElementClipPath,
			applyScaling: FrameRenderingOptions.applyScalingToNativeElementClipPath);
		var lastRenderedFrame = (picture, path, Bounds.Size, timestamp);
		lock (_frameGate)
		{
			_lastRenderedFrame = lastRenderedFrame;
		}

		FramePainted?.Invoke();
		XamlRootMap.GetHostForRoot(this)!.InvalidateRender();
	}

	internal SKPath OnNativePlatformFrameRequested(SKCanvas? canvas, Func<Size, SKCanvas> resizeFunc)
	{
		CompositionTarget.InvokeRendering();

		(SKPicture frame, SKPath nativeElementClipPath, Size size, long timestamp)? lastRenderedFrameNullable;
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

			return lastRenderedFrame.nativeElementClipPath;
		}
	}

	internal void RequestNewFrame()
	{
		if (!Interlocked.Exchange(ref _renderQueued, true))
		{
			// TODO: How do we prevent lagging over time? i.e. we do not want to wait 1/fps for the next
			// PaintFrame, we only want to wait the "remainder" of the time slice.
			// (SKPicture frame, SKPath nativeElementClipPath, Size size, long timestamp)? lastRenderedFrameNullable;
			// lock (_frameGate)
			// {
			// 	lastRenderedFrameNullable = _lastRenderedFrame;
			// }
			// var dueTime = lastRenderedFrameNullable is not { } lastRenderedFrame
			// 	? TimeSpan.Zero
			// 	: TimeSpan.FromTicks(Math.Max(0, Stopwatch.GetTimestamp() - lastRenderedFrame.timestamp));
			_renderTimer.Enabled = true;
		}
	}

	private void OnRenderTimerTick()
	{
		if (Interlocked.Exchange(ref _renderQueued, false))
		{
			// Very few things enqueue on High and all of them are rendering-related, so this is near guaranteed to
			// be ahead of everything else in the dispatcher queue.
			NativeDispatcher.Main.Enqueue(() => PaintFrame(), NativeDispatcherPriority.High);
		}
	}
}
