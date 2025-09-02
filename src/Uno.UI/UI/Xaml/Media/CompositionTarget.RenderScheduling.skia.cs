#nullable enable
using System;
using System.Diagnostics;
using Windows.Foundation;
using SkiaSharp;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.Composition;
using Uno.UI.Dispatching;
using Uno.UI.Helpers;
using Uno.UI.Hosting;

namespace Microsoft.UI.Xaml.Media;

public partial class CompositionTarget
{
	private readonly object _renderingStateGate = new();

	private float _fps = FeatureConfiguration.CompositionTarget.FrameRate;
	private float? _updatedFps; // used to update _fps but only after making sure that the old value of _fps is not currently being used in an enqueued paint action o the dispatcher queue

	private bool _paintRequested; // only set or read under _renderingStateGate
	private bool _paintedAheadOfTime; // only set or read under _renderingStateGate
	private bool _paintRequestedAfterAheadOfTimePaint; // only set or read under _renderingStateGate

	private bool PaintRequested
	{
		get => _paintRequested;
		set
		{
			_paintRequested = value;
			this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()} _paintRequested = {_paintRequested}");
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

		if (_updatedFps is { } updatedFps)
		{
			// We just got back from the dispatcher, so we know for a fact that the dispatcher doesn't have any enqueued
			// paint job. Now, we can update _fps safely. If we updated _fps immediately when the screen refresh
			// changes, we can break the invariant of always having a non-decreasing minimumTimestamp sent to the
			// dispatcher, specifically if the new refresh rate is higher than the current one.
			_fps = updatedFps;
			_updatedFps = null;
		}

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

	internal void SetRefreshRate(float fps)
	{
		if (FeatureConfiguration.CompositionTarget.UseNativeRefreshRate)
		{
			NativeDispatcher.Main.Enqueue(() => _updatedFps = fps);
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
