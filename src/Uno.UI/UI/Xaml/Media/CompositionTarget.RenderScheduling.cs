#nullable enable
using System;
using System.Diagnostics;
using System.Threading;
using Windows.Foundation;
using Uno.Foundation.Logging;
using Uno.UI.Composition;
using Uno.UI.Composition.Drawing;
using Uno.UI.Dispatching;
using Uno.UI.Helpers;
using Uno.UI.Hosting;

namespace Microsoft.UI.Xaml.Media;

public partial class CompositionTarget
{
	//                      +---------+            +-------------------------------------------+                                                                                   +---------------+ +-----------------------------+        +-------------------+
	//                      | Visual  |            | CompositionTargetNotNecessarilyOnUIThread |                                                                                   | IXamlRootHost | | CompositionTargetOnUIThread |        | NativeDispatcher  |
	//                      +---------+            +-------------------------------------------+                                                                                   +---------------+ +-----------------------------+        +-------------------+
	// ------------------------\ |                                       |                                                                                                                 |                        |                                 |
	// | some property changes |-|                                       |                                                                                                                 |                        |                                 |
	// |-----------------------| |                                       |                                                                                                                 |                        |                                 |
	//                           |                                       |                                                                                                                 |                        |                                 |
	//                           | RequestNewFrame                       |                                                                                                                 |                        |                                 |
	//                           |-------------------------------------->|                                                                                                                 |                        |                                 |
	//                           |                                       |                                                                                                                 |                        |                                 |
	//                           |                                       | InvalidateRender (the native platform will later call us back likely on the next monitor VSync)                 |                        |                                 |
	//                           |                                       |---------------------------------------------------------------------------------------------------------------->|                        |                                 |
	// ------------------------\ |                                       |                                                                                                                 |                        |                                 |
	// | some property changes |-|                                       |                                                                                                                 |                        |                                 |
	// |-----------------------| |                                       |                                                                                                                 |                        |                                 |
	//                           |                                       |                                                                                                                 |                        |                                 |
	//                           | RequestNewFrame                       |                                                                                                                 |                        |                                 |
	//                           |-------------------------------------->|                                                                                                                 |                        |                                 |
	//                           |        -----------------------------\ |                                                                                                                 |                        |                                 |
	//                           |        | RequestNewFrame is ignored |-|                                                                                                                 |                        |                                 |
	//                           |        |----------------------------| |                                                                                                                 |                        |                                 |
	//                           |                                       |                                                                                                                 |                        |                                 |
	//                           | RequestNewFrame                       |                                                                                                                 |                        |                                 |
	//                           |-------------------------------------->|                                                                                                                 |                        |                                 |
	//                           |        -----------------------------\ |                                                                                                                 |                        |                                 |
	//                           |        | RequestNewFrame is ignored |-|                                                                                                                 |                        |                                 |
	//                           |        |----------------------------| |                                                                                                                 |                        |                                 |
	//                           |                                       |                           ------------------------------------------------------------------------------------\ |                        |                                 |
	//                           |                                       |                           | native platform render callback in response to the previous InvalidateRender call |-|                        |                                 |
	//                           |                                       |                           |-----------------------------------------------------------------------------------| |                        |                                 |
	//                           |                                       |                                                                                                                 |                        |                                 |
	//                           |                                       |                                                                                  OnNativePlatformFrameRequested |                        |                                 |
	//                           |                                       |<----------------------------------------------------------------------------------------------------------------|                        |                                 |
	//                           |                                       |                                                                                                                 |                        |                                 |
	//                           |                                       | Draw the pixels from the last SKPicture and returns native element clip path pair generated in Render()         |                        |                                 |
	//                           |                                       |---------------------------------------------------------------------------------------------------------------->|                        |                                 |
	//                           |                                       |                                                                                                                 |                        |                                 |
	//                           |                                       | EnqueueRender (the NativeDispatcher will call us back when it thinks it's the best time to do so)               |                        |                                 |
	//                           |                                       |--------------------------------------------------------------------------------------------------------------------------------------------------------------------------->|
	//                           |                                       |                                                                                                                 |                        |                                 |
	//                           |                                       |                                                                                                                 |                        |           EnqueueRenderCallback |
	//                           |                                       |                                                                                                                 |                        |<--------------------------------|
	//                           |                                       |                                                                                                                 |           -----------\ |                                 |
	//                           |                                       |                                                                                                                 |           | Render() |-|                                 |
	//                           |                                       |                                                                                                                 |           |----------| |                                 |
	// ------------------------\ |                                       |                                                                                                                 |                        |                                 |
	// | some property changes |-|                                       |                                                                                                                 |                        |                                 |
	// |-----------------------| |                                       |                                                                                                                 |                        |                                 |
	//             ------------\ |                                       |                                                                                                                 |                        |                                 |
	//             | Repeat... |-|                                       |                                                                                                                 |                        |                                 |
	//             |-----------| |                                       |                                                                                                                 |                        |                                 |
	//                           |                                       |                                                                                                                 |                        |                                 |
	private readonly object _renderingStateGate = new();

	private bool _renderRequested; // only set or read under _renderingStateGate
	private bool _renderedAheadOfTime; // only set or read under _renderingStateGate
	private bool _renderRequestedAfterAheadOfTimePaint; // only set or read under _renderingStateGate
	private bool _shouldEnqueueRenderOnNextNativePlatformFrameRequested = true; // only set from the UI thread, only reset from the rendering/gpu thread

	// When the host stops delivering frames, the outstanding request latches and every later one coalesces into
	// it, so the app goes quiet with nothing in the log to say why. Written from the rendering thread, read from
	// the UI thread.
	private long _lastNativeFrameTimestamp = Stopwatch.GetTimestamp();
	private long _lastStalledRenderLogTimestamp;
	private int _stalledRenderReports;

	/// <summary>How long a render request may stay outstanding before it is reported as a stall.</summary>
	private const int StalledRenderReportMs = 2000;

	/// <summary>Minimum interval between stall reports, so a stalled window logs once rather than per request.</summary>
	private const int StalledRenderReportIntervalMs = 5000;

	/// <summary>Reports per stall, so a window the host legitimately stopped drawing (minimized) doesn't log forever.</summary>
	private const int MaxStalledRenderReports = 3;

	private bool RenderRequested
	{
		get => _renderRequested;
		set
		{
			_renderRequested = value;
			this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()} {nameof(_renderRequested)} = {_renderRequested}");
		}
	}

	void ICompositionTarget.RequestNewFrame()
	{
		var shouldEnqueue = false;
		lock (_renderingStateGate)
		{
			LogRenderState();
			AssertRenderStateMachine();
			if (!_renderedAheadOfTime && !RenderRequested)
			{
				RenderRequested = true;
				shouldEnqueue = true;
			}
			else if (_renderedAheadOfTime)
			{
				_renderRequestedAfterAheadOfTimePaint = true;

				// Still ask for a frame. Clearing this state depends on one arriving to run the render callback,
				// and the only other request is the one the ahead-of-time paint made, so if that is lost nothing
				// asks again and rendering waits for the stall recovery instead.
				shouldEnqueue = true;
			}
			AssertRenderStateMachine();
			LogRenderState();
		}

		// Re-invalidating a request the host never answered is what keeps a lost frame from stopping rendering
		// for good: the request latches, every later one coalesces into it, and nothing else would ever ask again.
		if (shouldEnqueue || IsRenderRequestStalled())
		{
			if (ContentRoot.XamlRoot is { } xamlRoot && XamlRootMap.GetHostForRoot(xamlRoot) is { } host)
			{
				host.InvalidateRender();
			}
			this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()}: {nameof(ICompositionTarget.RequestNewFrame)} invalidated render");
		}
		else
		{
			this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()}: {nameof(ICompositionTarget.RequestNewFrame)} found no need to invalidate render.");
		}
	}

	/// <summary>
	/// Whether a render request has been outstanding while the host produced no frame, so the request should be
	/// re-issued. Rate-limited, so a host that is merely slow re-asks at most once per
	/// <see cref="StalledRenderReportIntervalMs"/> rather than on every request. Also reports it: a stall that
	/// recovers this way is invisible otherwise, and it is the one signal that separates "the host stopped
	/// drawing" from "nothing asked for a frame".
	/// </summary>
	private bool IsRenderRequestStalled()
	{
		var sinceFrameMs = (long)Stopwatch.GetElapsedTime(Interlocked.Read(ref _lastNativeFrameTimestamp)).TotalMilliseconds;
		if (sinceFrameMs < StalledRenderReportMs)
		{
			return false;
		}

		var lastLog = Interlocked.Read(ref _lastStalledRenderLogTimestamp);
		if (lastLog != 0 && Stopwatch.GetElapsedTime(lastLog).TotalMilliseconds < StalledRenderReportIntervalMs)
		{
			return false;
		}

		Interlocked.Exchange(ref _lastStalledRenderLogTimestamp, Stopwatch.GetTimestamp());

		// The frame we are about to ask for has to reach the UI thread, and this handshake is the other half that
		// can be left holding a lost frame: it is cleared when a frame is taken and only set again by the render
		// callback that frame was supposed to schedule.
		Interlocked.Exchange(ref _shouldEnqueueRenderOnNextNativePlatformFrameRequested, true);

		if (_stalledRenderReports++ < MaxStalledRenderReports && this.Log().IsEnabled(LogLevel.Warning))
		{
			this.Log().Warn(
				$"CompositionTarget#{GetHashCode()}: a render request has been outstanding for {sinceFrameMs}ms with no "
				+ $"frame from the host (renderRequested={_renderRequested}, renderedAheadOfTime={_renderedAheadOfTime}, "
				+ $"requestedAfterAheadOfTimePaint={_renderRequestedAfterAheadOfTimePaint}); asking again.");
		}

		return true;
	}

	private void EnqueueRenderCallback()
	{
		this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()}: {nameof(EnqueueRenderCallback)}");
		NativeDispatcher.CheckThreadAccess();

		Interlocked.Exchange(ref _shouldEnqueueRenderOnNextNativePlatformFrameRequested, true);

		lock (_renderingStateGate)
		{
			LogRenderState();
			AssertRenderStateMachine();
			if (_renderedAheadOfTime)
			{
				_renderedAheadOfTime = false;
				if (_renderRequestedAfterAheadOfTimePaint)
				{
					_renderRequestedAfterAheadOfTimePaint = false;
					this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()}: {nameof(EnqueueRenderCallback)}: rendered ahead of time and got a new frame request since. Doing nothing this tick and rescheduling another tick");
					((ICompositionTarget)this).RequestNewFrame();
				}
				else
				{
					this.LogTrace()?.Trace($"{nameof(EnqueueRenderCallback)}: rendered ahead of time and no new frame was requested since.");
				}
			}
			else if (RenderRequested)
			{
				lock (_renderingStateGate)
				{
					RenderRequested = false;
				}
				this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()}: {nameof(Draw)} fired from {nameof(EnqueueRenderCallback)}");
				Render();
			}
			AssertRenderStateMachine();
			LogRenderState();
		}
	}

	/// <summary>
	/// This method is called from each platform's rendering logic in response to the native windowing/composition
	/// engine's signal requesting the Uno app to draw something _right now_, usually synced to the refresh rate
	/// of the screen (e.g. Android's IRenderer.OnDrawFrame). This class does not assume that this method will only
	/// be called once per <see cref="IXamlRootHost.InvalidateRender"/> call, but the contract allows any number
	/// of repeated calls, even if no new invalidations are requested.
	/// </summary>
	internal IGeometry OnNativePlatformFrameRequested(ISwapChain swapChain, global::System.Numerics.Matrix4x4? rootTransform = null, Action<IDrawingSession>? overlay = null)
	{
		this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()}: {nameof(OnNativePlatformFrameRequested)}");

		Interlocked.Exchange(ref _lastNativeFrameTimestamp, Stopwatch.GetTimestamp());
		_stalledRenderReports = 0;

		if (Interlocked.Exchange(ref _shouldEnqueueRenderOnNextNativePlatformFrameRequested, false))
		{
			NativeDispatcher.Main.EnqueueRender(this, EnqueueRenderCallback);
		}

		var nativeElementClipPath = Draw(swapChain, rootTransform, overlay);
		swapChain.Present();
		return nativeElementClipPath;
	}

	internal void OnRenderFrameOpportunity()
	{
		// If we get an opportunity to get call Render earlier than EnqueuePaintCallback, then we do that
		// but skip the Render call in the next EnqueuePaintCallback so that overall we're still keeping
		// the rate of Render calls the same.
		NativeDispatcher.CheckThreadAccess();

		if (FrameRenderHelper.CanRecordFrame(ContentRoot.VisualTree.RootElement))
		{
			var shouldRender = false;
			lock (_renderingStateGate)
			{
				LogRenderState();
				AssertRenderStateMachine();
				if (RenderRequested && !_renderedAheadOfTime)
				{
					RenderRequested = false;
					_renderedAheadOfTime = true;
					shouldRender = true;
				}
				AssertRenderStateMachine();
				LogRenderState();
			}

			if (shouldRender)
			{
				this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()}: {nameof(OnRenderFrameOpportunity)}: Calling {nameof(Draw)} early ");
				Render();
			}
		}
	}

	[Conditional("DEBUG")]
	private void AssertRenderStateMachine()
	{
		lock (_renderingStateGate)
		{
			Debug.Assert(!_renderRequestedAfterAheadOfTimePaint || _renderedAheadOfTime);
			Debug.Assert(!_renderedAheadOfTime || !RenderRequested);
		}
	}

	private void LogRenderState()
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			lock (_renderingStateGate)
			{
				this.Log().Trace($"CompositionTarget#{GetHashCode()}: Render state machine: {nameof(_renderRequested)} = {_renderRequested}, {nameof(_renderedAheadOfTime)} = {_renderedAheadOfTime}, {nameof(_renderRequestedAfterAheadOfTimePaint)}={_renderRequestedAfterAheadOfTimePaint}");
			}
		}
	}
}
