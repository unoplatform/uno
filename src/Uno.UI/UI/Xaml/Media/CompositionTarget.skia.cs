#nullable enable
using System;
using System.Diagnostics;
using Windows.Foundation;
using SkiaSharp;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.Composition;
using Uno.UI.Dispatching;
using Uno.UI.Helpers;
using Uno.UI.Hosting;

namespace Microsoft.UI.Xaml.Media;

public partial class CompositionTarget
{
	private readonly SkiaRenderHelper.FpsHelper _fpsHelper = new();
	// TODO: read the native refresh rate instead of hardcoding it
	private readonly float _fps = FeatureConfiguration.CompositionTarget.FrameRate;
	private readonly object _frameGate = new();
	private readonly object _renderingStateGate = new();
	private (SKPicture frame, SKPath nativeElementClipPath, Size size, long timestamp)? _lastRenderedFrame;
	private Size _lastCanvasSize = Size.Empty;

	private bool _paintRequested;
	private bool _paintedAheadOfTime;
	private bool _paintRequestedAfterAheadOfTimePaint;

	internal static (bool invertNativeElementClipPath, bool applyScalingToNativeElementClipPath) FrameRenderingOptions { get; set; } = (false, true);

	internal event Action? FramePainted;

	private bool PaintRequested
	{
		get => _paintRequested;
		set
		{
			_paintRequested = value;
			this.LogTrace()?.Trace($"_paintRequested = {_paintRequested}");
		}
	}

	private void PaintFrame()
	{
		var timestamp = Stopwatch.GetTimestamp();
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"PaintFrame begins with timestamp {timestamp}");
			using var _ = Disposable.Create(() => this.Log().Trace("PaintFrame ends"));
		}

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
		var lastRenderedFrame = (picture, path, new Size((int)(bounds.Width * scale), (int)(bounds.Height * scale)), timestamp);
		lock (_frameGate)
		{
			_lastRenderedFrame = lastRenderedFrame;
		}

		if (CompositionTarget.IsRenderingActive)
		{
			RequestNewFrame(true);
		}

		FramePainted?.Invoke();
		if (rootElement.XamlRoot is not null)
		{
			XamlRootMap.GetHostForRoot(rootElement.XamlRoot)?.InvalidateRender();
		}
	}

	internal SKPath OnNativePlatformFrameRequested(SKCanvas? canvas, Func<Size, SKCanvas> resizeFunc)
	{
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

			CompositionTarget.InvokeRendering();

			return lastRenderedFrame.nativeElementClipPath;
		}
	}

	void ICompositionTarget.RequestNewFrame() => RequestNewFrame(true);
	private void RequestNewFrame(bool preventDrifting)
	{
		var shouldEnqueue = false;
		lock (_renderingStateGate)
		{
			AssertRenderStateMachine();
			if (!_paintedAheadOfTime && !PaintRequested)
			{
				PaintRequested = true;
				shouldEnqueue = true;
			}
			else if (_paintedAheadOfTime)
			{
				_paintRequestedAfterAheadOfTimePaint = true;
			}
			AssertRenderStateMachine();
		}

		if (shouldEnqueue)
		{
			long? lastTimestampNullable;
			lock (_frameGate)
			{
				lastTimestampNullable = _lastRenderedFrame?.timestamp;
			}

			long minimumTimestamp = 0;
			if (preventDrifting && lastTimestampNullable is { } lastTimestamp)
			{
				minimumTimestamp = GetNextMultiple(lastTimestamp, (long)(Stopwatch.Frequency / _fps));
			}
			else
			{
				minimumTimestamp = GetNextMultiple(Stopwatch.GetTimestamp(), (long)(Stopwatch.Frequency / _fps));
			}

			this.LogTrace()?.Trace($"Requested paint with minimumTimestamp = {minimumTimestamp}");
			NativeDispatcher.Main.EnqueuePaint(OnRenderTimerTick, minimumTimestamp);
		}
	}

	private static long GetNextMultiple(long target, long divisor) => ((target + divisor - 1) / divisor) * divisor;

	private void OnRenderTimerTick()
	{
		this.LogTrace()?.Trace("Render timer tick");
		lock (_renderingStateGate)
		{
			AssertRenderStateMachine();
			if (_paintedAheadOfTime)
			{
				_paintedAheadOfTime = false;
				if (_paintRequestedAfterAheadOfTimePaint)
				{
					_paintRequestedAfterAheadOfTimePaint = false;
					this.LogTrace()?.Trace("Render timer tick: painted ahead of time and got a new frame request since. Doing nothing this tick and rescheduling another tick");
					RequestNewFrame(false);
				}
				else
				{
					this.LogTrace()?.Trace("Render timer tick: painted ahead of time and no new frame was requested since.");
				}
			}
			else if (PaintRequested)
			{
				this.LogTrace()?.Trace("PaintFrame fired from enqueued timer job");
				lock (_renderingStateGate)
				{
					PaintRequested = false;
				}
				PaintFrame();
			}
			AssertRenderStateMachine();
		}
	}

	internal void OnPaintFrameOpportunity()
	{
		// If we get an opportunity to get call PaintFrame earlier than the timer tick, then we do that
		// but skip the PaintFrame call in the next timer tick so that overall we're still keeping
		// the rate of PaintFrame calls the same.
		NativeDispatcher.CheckThreadAccess();

		if (SkiaRenderHelper.CanRecordPicture(ContentRoot.VisualTree.RootElement))
		{
			var shouldPaint = false;
			lock (_renderingStateGate)
			{
				AssertRenderStateMachine();
				if (PaintRequested && !_paintedAheadOfTime)
				{
					PaintRequested = false;
					_paintedAheadOfTime = true;
					shouldPaint = true;
				}
				AssertRenderStateMachine();
			}

			if (shouldPaint)
			{
				this.LogTrace()?.Trace("OnPaintFrameOpportunity: Calling PaintFrame early ");
				PaintFrame();
			}
		}
	}

	[Conditional("DEBUG")]
	private void AssertRenderStateMachine()
	{
		lock (_renderingStateGate)
		{
			Debug.Assert(!_paintRequestedAfterAheadOfTimePaint || _paintedAheadOfTime);
			Debug.Assert(!_paintedAheadOfTime || !PaintRequested);
		}
	}
}
