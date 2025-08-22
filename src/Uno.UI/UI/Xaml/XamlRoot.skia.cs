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

namespace Microsoft.UI.Xaml;

partial class XamlRoot
{
	private readonly SkiaRenderHelper.FpsHelper _fpsHelper = new();
	// TODO: prevent this from being bottlenecked by the dispatcher queue, and read the native refresh rate instead of hardcoding it
	private readonly Timer _renderTimer;
	private readonly object _frameGate = new();
	private readonly object _renderQueuingGate = new();
	private (SKPicture frame, SKPath nativeElementClipPath, Size size, long timestamp)? _lastRenderedFrame;
	private Size _lastCanvasSize = Size.Empty;
	private bool _paintQueued; // only reset on the UI thread, set not necessarily on the UI thread
	private bool _paintedAheadOfTime;

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

		lock (_renderQueuingGate)
		{
			_paintQueued = false;
		}

		var rootElement = VisualTree.RootElement;

		var (picture, path) = SkiaRenderHelper.RecordPictureAndReturnPath(
			(int)(Bounds.Width * RasterizationScale),
			(int)(Bounds.Height * RasterizationScale),
			rootElement,
			invertPath: FrameRenderingOptions.invertNativeElementClipPath,
			applyScaling: FrameRenderingOptions.applyScalingToNativeElementClipPath);
		var lastRenderedFrame = (picture, path, new Size((int)(Bounds.Width * RasterizationScale), (int)(Bounds.Height * RasterizationScale)), timestamp);
		lock (_frameGate)
		{
			_lastRenderedFrame = lastRenderedFrame;
		}

		if (CompositionTarget.IsRenderingActive)
		{
			RequestNewFrame();
		}

		FramePainted?.Invoke();
		XamlRootMap.GetHostForRoot(this)?.InvalidateRender();
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
		lock (_renderQueuingGate)
		{
			if (!_paintQueued)
			{
				_paintQueued = true;
				long? lastTimestampNullable;
				lock (_frameGate)
				{
					lastTimestampNullable = _lastRenderedFrame?.timestamp;
				}
				if (lastTimestampNullable is { } lastTimestamp)
				{
					var delta = Stopwatch.GetElapsedTime(lastTimestamp);
					var remainingPartOfTickSlice = delta > TimeSpan.FromSeconds(1 / 60.0) ? TimeSpan.Zero : TimeSpan.FromSeconds(1 / 60.0) - delta;
					_renderTimer.Change(remainingPartOfTickSlice, Timeout.InfiniteTimeSpan);
				}
				else
				{
					_renderTimer.Change(TimeSpan.FromSeconds(1 / 60.0), Timeout.InfiniteTimeSpan);
				}
			}
		}
	}

	private void OnRenderTimerTick()
	{
		lock (_renderQueuingGate)
		{
			if (_paintQueued && _paintedAheadOfTime)
			{
				_renderTimer.Change(TimeSpan.FromSeconds(1 / 60.0), Timeout.InfiniteTimeSpan);
			}

			if (!_paintedAheadOfTime && _paintQueued)
			{
				NativeDispatcher.Main.Enqueue(PaintFrame, NativeDispatcherPriority.High);
			}

			_paintedAheadOfTime = false;
		}
	}

	internal void OnPaintFrameOpportunity()
	{
		// If we get an opportunity to get call PaintFrame earlier than the timer tick, then we do that
		// but skip the PaintFrame call in the next timer tick so that overall we're still keeping
		// the rate of PaintFrame calls the same.
		NativeDispatcher.CheckThreadAccess();

		if (SkiaRenderHelper.CanRecordPicture(VisualTree.RootElement))
		{
			var shouldPaint = false;
			lock (_renderQueuingGate)
			{
				if (_paintQueued && !_paintedAheadOfTime)
				{
					_paintedAheadOfTime = true;
					shouldPaint = true;
				}
			}

			if (shouldPaint)
			{
				PaintFrame();
			}
		}
	}
}
