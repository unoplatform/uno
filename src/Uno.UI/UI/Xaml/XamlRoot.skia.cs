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
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.Dispatching;
using Uno.UI.Helpers;
using Uno.UI.Hosting;

namespace Microsoft.UI.Xaml;

partial class XamlRoot
{
	private readonly SkiaRenderHelper.FpsHelper _fpsHelper = new();
	// TODO: read the native refresh rate instead of hardcoding it
	private readonly float _fps = FeatureConfiguration.CompositionTarget.FrameRate;
	private readonly Timer _renderTimer;
	private readonly object _frameGate = new();
	private readonly object _renderingStateGate = new();
	private (SKPicture frame, SKPath nativeElementClipPath, Size size, long timestamp)? _lastRenderedFrame;
	private Size _lastCanvasSize = Size.Empty;
	private int _frameCount;

	private enum RenderCycleState
	{
		Clean,
		PaintRequested,
		PaintQueued
	}

	private RenderCycleState _renderState = RenderCycleState.Clean;
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
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace("PaintFrame begins");
			using var _ = Disposable.Create(() => this.Log().Trace("PaintFrame ends"));
		}

		var timestamp = Stopwatch.GetTimestamp();
		NativeDispatcher.CheckThreadAccess();

		_frameCount++;

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
		lock (_renderingStateGate)
		{
			if (_renderState is RenderCycleState.Clean)
			{
				_renderState = RenderCycleState.PaintRequested;
				WakeUpTimer();
			}
		}
	}

	private void WakeUpTimer()
	{
		long? lastTimestampNullable;
		lock (_frameGate)
		{
			lastTimestampNullable = _lastRenderedFrame?.timestamp;
		}

		TimeSpan dueTime;
		if (lastTimestampNullable is { } lastTimestamp)
		{
			var delta = Stopwatch.GetElapsedTime(lastTimestamp);
			dueTime = delta > TimeSpan.FromSeconds(1 / _fps) ? TimeSpan.Zero : TimeSpan.FromSeconds(1 / _fps) - delta;
		}
		else
		{
			dueTime = TimeSpan.FromSeconds(1 / _fps);
		}

		this.LogTrace()?.Trace($"Enabling render timer with dueTime = {dueTime}");
		_renderTimer.Change(dueTime, Timeout.InfiniteTimeSpan);
	}

	private void OnRenderTimerTick()
	{
		this.LogTrace()?.Trace($"Render timer tick");
		lock (_renderingStateGate)
		{
			if (_paintedAheadOfTime)
			{
				if (_renderState == RenderCycleState.PaintRequested)
				{
					_paintedAheadOfTime = false;
					this.LogTrace()?.Trace($"Render timer tick: painted ahead of time. Doing nothing this tick and rescheduling another tick");
					_renderTimer.Change(TimeSpan.FromSeconds(1 / 60.0), Timeout.InfiniteTimeSpan);
				}
			}
			else if (_renderState == RenderCycleState.PaintRequested)
			{
				this.LogTrace()?.Trace("Render timer tick: enqueued PaintFrame");
				NativeDispatcher.Main.Enqueue(() =>
				{
					this.LogTrace()?.Trace("PaintFrame fired from enqueued timer job");
					lock (_renderingStateGate)
					{
						_renderState = RenderCycleState.Clean;
					}
					PaintFrame();
				}, _frameCount % 5 == 0 ? NativeDispatcherPriority.Normal : NativeDispatcherPriority.High);
				_renderState = RenderCycleState.PaintQueued;
			}
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
			lock (_renderingStateGate)
			{
				if (_renderState is RenderCycleState.PaintRequested && !_paintedAheadOfTime)
				{
					_renderState = RenderCycleState.Clean;
					_paintedAheadOfTime = true;
					shouldPaint = true;
				}
			}

			if (shouldPaint)
			{
				this.LogTrace()?.Trace("OnPaintFrameOpportunity: Calling PaintFrame early ");
				PaintFrame();
			}
		}
	}
}
