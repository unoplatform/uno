#nullable enable
using System;
using System.Diagnostics;
using System.Threading;
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
	private readonly object _renderingStateGate = new();

	private bool _paintRequested; // only set or read under _renderingStateGate
	private bool _paintedAheadOfTime; // only set or read under _renderingStateGate
	private bool _paintRequestedAfterAheadOfTimePaint; // only set or read under _renderingStateGate
	private bool _shouldEnqueuePaintOnNextNativePlatformFrameRequested = true; // only set from the UI thread, only reset from the rendering/gpu thread

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
			XamlRootMap.GetHostForRoot(ContentRoot.XamlRoot!)!.InvalidateRender();
			// long? lastTimestampNullable;
			// lock (_frameGate)
			// {
			// 	lastTimestampNullable = _lastRenderedFrame?.timestamp;
			// }
			//
			// long minimumTimestamp = 0;
			// if (lastTimestampNullable is { } lastTimestamp)
			// {
			// 	minimumTimestamp = 0;
			// }
			//
			this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()}: RequestNewFrame invalidated render");
			// NativeDispatcher.Main.EnqueuePaint(this, OnDispatcherNewFrameCallback, minimumTimestamp);
		}
		else
		{
			this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()}: RequestNewFrame found no need to invalidate render.");
		}
	}

	private void EnqueuePaintCallback()
	{
		this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()}: {nameof(EnqueuePaintCallback)}");
		NativeDispatcher.CheckThreadAccess();

		Interlocked.Exchange(ref _shouldEnqueuePaintOnNextNativePlatformFrameRequested, true);

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
						_lastRenderedFrame = _lastRenderedFrame.Value with { timestamp = 0 };
						this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()}: {nameof(EnqueuePaintCallback)}: updating last frame timestamp since it was painted ahead of time to {_lastRenderedFrame.Value.timestamp}");
					}
				}
				if (_paintRequestedAfterAheadOfTimePaint)
				{
					_paintRequestedAfterAheadOfTimePaint = false;
					this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()}: {nameof(EnqueuePaintCallback)}: painted ahead of time and got a new frame request since. Doing nothing this tick and rescheduling another tick");
					((ICompositionTarget)this).RequestNewFrame();
				}
				else
				{
					this.LogTrace()?.Trace($"{nameof(EnqueuePaintCallback)}: painted ahead of time and no new frame was requested since.");
				}
			}
			else if (PaintRequested)
			{
				lock (_renderingStateGate)
				{
					PaintRequested = false;
				}
				this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()}: PaintFrame fired from {nameof(EnqueuePaintCallback)}");
				Render();
			}
			AssertRenderStateMachine();
			LogRenderState();
		}
	}

	internal SKPath OnNativePlatformFrameRequested(SKCanvas? canvas, Func<Size, SKCanvas> resizeFunc)
	{
		this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()}: {nameof(OnNativePlatformFrameRequested)}");

		if (Interlocked.Exchange(ref _shouldEnqueuePaintOnNextNativePlatformFrameRequested, false))
		{
			NativeDispatcher.Main.EnqueuePaint(this, EnqueuePaintCallback);
		}

		return Render(canvas, resizeFunc);
	}

	internal void OnPaintFrameOpportunity()
	{
		// If we get an opportunity to get call PaintFrame earlier than EnqueuePaintCallback, then we do that
		// but skip the PaintFrame call in the next EnqueuePaintCallback so that overall we're still keeping
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
				Render();
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
