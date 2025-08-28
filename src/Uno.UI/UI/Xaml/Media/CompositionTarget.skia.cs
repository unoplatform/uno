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
using Uno.UI.Xaml.Core;

namespace Microsoft.UI.Xaml.Media;

public partial class CompositionTarget
{
	private readonly SkiaRenderHelper.FpsHelper _fpsHelper = new();
	// TODO: read the native refresh rate instead of hardcoding it
	private readonly float _fps = FeatureConfiguration.CompositionTarget.FrameRate;

	private readonly object _frameGate = new();
	private readonly object _renderingStateGate = new();

	// Only read and set from the native rendering thread in OnNativePlatformFrameRequested
	private Size _lastCanvasSize = Size.Empty;
	// only set on the UI thread and under _frameGate, only read under _frameGate
	private (SKPicture frame, SKPath nativeElementClipPath, Size size, long timestamp)? _lastRenderedFrame;
	private bool _paintRequested; // only set or read under _renderingStateGate
	private bool _paintedAheadOfTime; // only set or read under _renderingStateGate
	private bool _paintRequestedAfterAheadOfTimePaint; // only set or read under _renderingStateGate

	internal static (bool invertNativeElementClipPath, bool applyScalingToNativeElementClipPath) FrameRenderingOptions { get; set; } = (false, true);

	internal event Action? FramePainted;

	private bool PaintRequested
	{
		get => _paintRequested;
		set
		{
			_paintRequested = value;
			this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()} _paintRequested = {_paintRequested}");
		}
	}

	private void PaintFrame()
	{
		var timestamp = Stopwatch.GetTimestamp();
		this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()}: PaintFrame begins with timestamp {timestamp}");

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

		if (IsRenderingActive)
		{
			((ICompositionTarget)this).RequestNewFrame();
		}

		FramePainted?.Invoke();
		if (rootElement.XamlRoot is not null)
		{
			XamlRootMap.GetHostForRoot(rootElement.XamlRoot)?.InvalidateRender();
		}

		this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()}: PaintFrame ends");
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

			InvokeRendering();

			return lastRenderedFrame.nativeElementClipPath;
		}
	}

	void ICompositionTarget.RequestNewFrame()
	{
		var shouldEnqueue = false;
		lock (_renderingStateGate)
		{
			LogRenderState();
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
			LogRenderState();
		}

		if (shouldEnqueue)
		{
			long? lastTimestampNullable;
			lock (_frameGate)
			{
				lastTimestampNullable = _lastRenderedFrame?.timestamp;
			}

			long minimumTimestamp = 0;
			if (lastTimestampNullable is { } lastTimestamp)
			{
				minimumTimestamp = GetNextMultiple(lastTimestamp, (long)(Stopwatch.Frequency / _fps));
			}

			this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()}: Requested paint with minimumTimestamp = {minimumTimestamp}");
			NativeDispatcher.Main.EnqueuePaint(this, OnDispatcherNewFrameCallback, minimumTimestamp);
		}
	}

	private static long GetNextMultiple(long target, long divisor) => ((target + divisor - 1) / divisor) * divisor;

	private void OnDispatcherNewFrameCallback()
	{
		this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()}: {nameof(OnDispatcherNewFrameCallback)}");
		NativeDispatcher.CheckThreadAccess();

		lock (_renderingStateGate)
		{
			LogRenderState();
			AssertRenderStateMachine();
			if (_paintedAheadOfTime)
			{
				_paintedAheadOfTime = false;
				lock (_frameGate)
				{
					if (_lastRenderedFrame is not null)
					{
						_lastRenderedFrame = _lastRenderedFrame.Value with { timestamp = GetNextMultiple(Stopwatch.GetTimestamp(), (long)(Stopwatch.Frequency / _fps)) };
						this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()}: {nameof(OnDispatcherNewFrameCallback)}: updating last frame timestamp since it was painted ahead of time to {_lastRenderedFrame.Value.timestamp}");
					}
				}
				if (_paintRequestedAfterAheadOfTimePaint)
				{
					_paintRequestedAfterAheadOfTimePaint = false;
					this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()}: {nameof(OnDispatcherNewFrameCallback)}: painted ahead of time and got a new frame request since. Doing nothing this tick and rescheduling another tick");
					((ICompositionTarget)this).RequestNewFrame();
				}
				else
				{
					this.LogTrace()?.Trace($"{nameof(OnDispatcherNewFrameCallback)}: painted ahead of time and no new frame was requested since.");
				}
			}
			else if (PaintRequested)
			{
				lock (_renderingStateGate)
				{
					PaintRequested = false;
				}
				this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()}: PaintFrame fired from {nameof(OnDispatcherNewFrameCallback)}");
				PaintFrame();
			}
			AssertRenderStateMachine();
			LogRenderState();
		}
	}

	internal void OnPaintFrameOpportunity()
	{
		// If we get an opportunity to get call PaintFrame earlier than OnDispatcherNewFrameCallback, then we do that
		// but skip the PaintFrame call in the next OnDispatcherNewFrameCallback so that overall we're still keeping
		// the rate of PaintFrame calls the same.
		NativeDispatcher.CheckThreadAccess();

		if (SkiaRenderHelper.CanRecordPicture(ContentRoot.VisualTree.RootElement))
		{
			var shouldPaint = false;
			lock (_renderingStateGate)
			{
				LogRenderState();
				AssertRenderStateMachine();
				if (PaintRequested && !_paintedAheadOfTime)
				{
					PaintRequested = false;
					_paintedAheadOfTime = true;
					shouldPaint = true;
				}
				AssertRenderStateMachine();
				LogRenderState();
			}

			if (shouldPaint)
			{
				this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()}: OnPaintFrameOpportunity: Calling PaintFrame early ");
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

	private void LogRenderState()
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			lock (_renderingStateGate)
			{
				this.Log().Trace($"CompositionTarget#{GetHashCode()}: Render state machine: _paintRequested = {_paintRequested}, _paintedAheadOfTime = {_paintedAheadOfTime}, _paintRequestedAfterAheadOfTimePaint={_paintRequestedAfterAheadOfTimePaint}");
			}
		}
	}
}
