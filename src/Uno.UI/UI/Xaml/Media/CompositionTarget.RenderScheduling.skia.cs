#nullable enable
using System;
using System.Diagnostics;
using System.Threading;
using SkiaSharp;
using Windows.Foundation;
using Uno.Foundation.Logging;
using Uno.UI.Composition;
using Uno.UI.Dispatching;
using Uno.UI.Helpers;
using Uno.UI.Hosting;
using Uno.UI.Xaml.Core;

namespace Microsoft.UI.Xaml.Media;

public partial class CompositionTarget
{
	//                      +---------+            +-------------------------------------------+
	//                      | Visual  |            |    CompositionTarget (batched FrameTick)   |
	//                      +---------+            +-------------------------------------------+
	// ------------------------\ |                                       |
	// | some property changes |-|                                       |
	// |-----------------------| |                                       |
	//                           |                                       |
	//                           | RequestNewFrame                       |
	//                           |-------------------------------------->|
	//                           |                                       |
	//                           |                                       | ScheduleFrameTick (via host.ScheduleFrameCallback,
	//                           |                                       |                    coalesced)
	//                           |                                       |----.
	//                           |                                       |    |
	//                           |                                       |<---'
	//                           |                                       |
	//                           |                                       | FrameTick: Layout -> Loaded -> InvokeRendering -> Render
	//                           |                                       |----.
	//                           |                                       |    |
	//                           |                                       |<---'
	//                           |                                       |
	//                           |                                       | InvalidateRender (native platform draws on next VSync)
	//                           |                                       |----.
	//                           |                                       |    |
	//                           |                                       |<---'
	//                           |                                       |
	//                           |                                       | OnNativePlatformFrameRequested -> Draw (previous SKPicture)
	//                           |                                       |
	//             ------------\ |                                       |
	//             | Repeat... |-|                                       |
	//             |-----------| |                                       |
	//                           |                                       |

	/// <summary>
	/// Guards against scheduling duplicate frame ticks. Thread-safe via Interlocked.
	/// </summary>
	private bool _frameTickScheduled;

	/// <summary>
	/// Cached delegate for FrameTick scheduling to avoid per-call lambda allocation.
	/// </summary>
	private Action? _frameTickCallback;

	/// <summary>
	/// Re-entrancy guard for FrameTick. FrameTick can be called re-entrantly when loaded
	/// event handlers trigger SynchronousRenderAndDraw (e.g., during Window.Show on Win32).
	/// </summary>
	private bool _inFrameTick;

	/// <summary>
	/// Throttle flag: when true, ScheduleFrameTick defers until OnFramePresented clears it.
	/// Only set on hosts with a render thread (SupportsRenderThrottle == true).
	/// Prevents the UI thread from recording frames faster than the render thread presents.
	/// Volatile: written on UI thread (FrameTick / OnFramePresented), read in ScheduleFrameTick
	/// which may be called from a non-UI thread via RequestNewFrame.
	/// </summary>
	private volatile bool _waitingForPresent;

	/// <summary>
	/// Set when ScheduleFrameTick is called while throttled. OnFramePresented schedules
	/// the deferred FrameTick when it clears the throttle.
	/// Volatile: written in ScheduleFrameTick (any thread), read in OnFramePresented (UI thread).
	/// </summary>
	private volatile bool _pendingFrameRequest;

	/// <summary>
	/// Set when FrameTick is called re-entrantly (e.g. loaded event → Window.Show → SynchronousRenderAndDraw).
	/// The outer FrameTick schedules another tick after completing if this is set.
	/// </summary>
	private bool _reentrantFrameRequested;

	/// <summary>
	/// Diagnostic: incremented every time FrameTick re-entry is detected and the inner
	/// tick is deferred. WinUI fail-fasts on the equivalent condition (XAML_FAIL_FAST in
	/// NWDrawTree); we are more lenient and only log + defer, but a sustained increase
	/// here indicates a third-party handler pumping the message loop or otherwise
	/// re-entering the render pipeline. Exposed via <see cref="ReentrantFrameTickCount"/>.
	/// </summary>
	private long _reentrantFrameTickCount;

	/// <summary>
	/// Number of times <see cref="FrameTick"/> has been re-entered and deferred over the
	/// life of this <see cref="CompositionTarget"/>. Use to diagnose handlers that pump
	/// the message loop or otherwise call back into the render pipeline.
	/// </summary>
	internal long ReentrantFrameTickCount => Interlocked.Read(ref _reentrantFrameTickCount);

	void ICompositionTarget.RequestNewFrame()
	{
		// Only schedule the next FrameTick. Don't call host.InvalidateRender() here —
		// the render thread is signaled from Render() after a new frame is recorded.
		ScheduleFrameTick();
	}

	/// <summary>
	/// Called by the host's render thread (posted to UI thread) after a frame is presented.
	/// Clears the throttle and schedules the next FrameTick if one was deferred.
	/// </summary>
	internal void OnFramePresented()
	{
		NativeDispatcher.CheckThreadAccess();
		_waitingForPresent = false;

		// FrameRendered fires here — this is the "frame on screen" moment, matching WinUI's
		// CompositionTarget.Rendered semantics. Subscribers get a post-present signal rather
		// than a post-record one.
		FrameRendered?.Invoke();

		if (_pendingFrameRequest)
		{
			_pendingFrameRequest = false;
			ScheduleFrameTick();
		}
	}

	/// <summary>
	/// Schedules a single batched frame tick if one is not already scheduled.
	/// Thread-safe: can be called from the UI thread or the native render thread.
	///
	/// Dispatched via <see cref="IXamlRootHost.ScheduleFrameCallback"/> so each host can
	/// use its native vsync primitive (Choreographer on Android, rAF on WASM, etc.). The
	/// default implementation falls back to the dispatcher at Normal priority for hosts
	/// that haven't been migrated to a vsync primitive yet.
	/// </summary>
	internal void ScheduleFrameTick()
	{
		if (_waitingForPresent)
		{
			// Render thread hasn't finished presenting the previous frame.
			// Remember the request — it will be scheduled when OnFramePresented fires.
			_pendingFrameRequest = true;
			return;
		}

		if (!Interlocked.Exchange(ref _frameTickScheduled, true))
		{
			_frameTickCallback ??= () =>
			{
				Interlocked.Exchange(ref _frameTickScheduled, false);
				FrameTick();
			};

			if (ContentRoot.XamlRoot is { } xamlRoot
				&& XamlRootMap.GetHostForRoot(xamlRoot) is { } host)
			{
				host.ScheduleFrameCallback(_frameTickCallback);
			}
			else
			{
				NativeDispatcher.Main.Enqueue(_frameTickCallback, NativeDispatcherPriority.Normal);
			}
		}
	}

	/// <summary>
	/// Single batched frame tick, posted to the dispatcher as one operation.
	/// Sequences: Layout -> Loaded events -> Layout -> CompositionTarget.Rendering -> Layout -> Render.
	/// Matches WPF MediaContext.RenderMessageHandlerCore / WinUI NWDrawTree OnTick pattern.
	///
	/// Trade-off: layout always runs before render here, which is structurally cleaner
	/// but means composition-only animations (e.g. CompositionTarget.Rendering subscribers
	/// that mutate composition-layer properties without dirtying layout) still pay one
	/// UpdateLayout walk per frame. UpdateLayout short-circuits on a clean tree so the
	/// cost is tolerable.
	///
	/// Known divergences from WinUI's NWDrawTree (xcpcore.cpp:6036) that are NOT addressed here:
	///
	///  • Animation tick ordering. WinUI ticks its TimeManager FIRST (advancing storyboard
	///    values to the current frame's time), THEN runs layout. Uno's CPUBoundAnimator-derived
	///    animators (the path Storyboards use) advance their values from a CompositionTarget.Rendering
	///    subscription, which fires AFTER the first layout in this method. Effect: the first
	///    UpdateLayout above sees stale animation values; the second UpdateLayout (after Rendering)
	///    catches up. The visible rendering is correct, but the first layout pass is wasted work
	///    during continuous animation. Fixing this requires moving the animation tick to a
	///    pre-layout hook — outside the scope of this branch because it touches the animation
	///    system's subscription model.
	///
	///  • No second animation tick after layout. WinUI does TimeManager.Tick(newTimelinesOnly=TRUE)
	///    after layout (xcpcore.cpp:6331) to pick up storyboards spawned during layout (e.g. a
	///    VisualState transition triggered by a measured size). Uno does not, so such storyboards
	///    start one frame late.
	///
	///  • No frame-skip backpressure. WinUI's m_framesToSkip mechanism advances state without
	///    rendering when the GPU is behind. Our throttle is binary (waiting/not). Acceptable
	///    on Win32 (vsync is reliable); could matter for hosts with variable present latency.
	/// </summary>
	internal void FrameTick()
	{
		NativeDispatcher.CheckThreadAccess();

		if (_inFrameTick)
		{
			// Re-entrant call (e.g. loaded event handler → Window.Show → SynchronousRenderAndDraw,
			// or a third-party Loaded handler that pumps the message loop).
			// Skip to avoid corrupting the loaded event list iteration. The outer FrameTick
			// will schedule another tick after completing.
			_reentrantFrameRequested = true;
			Interlocked.Increment(ref _reentrantFrameTickCount);

			// WinUI fail-fasts on tick re-entry (XAML_FAIL_FAST in NWDrawTree). We're more
			// lenient because shipping fail-fast on user-handler re-entry would be too
			// aggressive, but it's still a sign of a third-party handler doing something
			// unexpected — log so the divergence is observable.
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn(
					$"CompositionTarget#{GetHashCode()}: FrameTick re-entered (total: {_reentrantFrameTickCount}). " +
					"A handler invoked during Loaded or CompositionTarget.Rendering pumped the message loop " +
					"(e.g. modal dialog, blocking Win32 call). Deferring the inner tick.");
			}
			return;
		}

		if (ContentRoot.VisualTree.RootElement is not { } rootElement)
		{
			return;
		}

		_inFrameTick = true;
		try
		{
			// Resolve the host once for this tick (used for throttle + VSync timestamp).
			var host = ContentRoot.XamlRoot is { } xr
				? XamlRootMap.GetHostForRoot(xr)
				: null;

			// 1. Layout pass
			rootElement.UpdateLayout();

			// 2. Loaded events (may dirty layout again)
			if (CoreServices.Instance.EventManager.ShouldRaiseLoadedEvent)
			{
				CoreServices.Instance.EventManager.RaiseLoadedEvent();
				rootElement.UpdateLayout();
			}

			// 3. CompositionTarget.Rendering event (may dirty layout).
			//    Use the host's VSync timestamp when available (e.g. Android Choreographer)
			//    for accurate animation timing; fall back to wall clock otherwise.
			var vsync = host?.FrameVsyncTimestamp ?? 0;
			InvokeRendering(vsync != 0
				? Stopwatch.GetElapsedTime(_start, vsync)
				: Stopwatch.GetElapsedTime(_start));

			// 4. Re-layout after Rendering callbacks (matches WinUI NWDrawTree which runs
			//    UpdateLayout after CallPerFrameCallback). Without this, Rendering handlers
			//    that modify layout-affecting properties would render with stale layout.
			rootElement.UpdateLayout();

			// 5. Record SKPicture from visual tree
			if (SkiaRenderHelper.CanRecordPicture(rootElement))
			{
				// Set throttle BEFORE Render() to prevent RequestNewFrame() inside Render()
				// (called when CompositionTarget.Rendering has subscribers) from scheduling
				// another FrameTick immediately. Without this, one extra FrameTick fires per
				// displayed frame during continuous animations, doubling CPU work.
				if (host is { SupportsRenderThrottle: true })
				{
					_waitingForPresent = true;
				}

				Render();
			}
		}
		finally
		{
			_inFrameTick = false;

			// If a re-entrant call was suppressed, schedule another tick to process
			// the deferred state change. This prevents lost frames when loaded event
			// handlers indirectly trigger RequestNewFrame.
			if (_reentrantFrameRequested)
			{
				_reentrantFrameRequested = false;
				ScheduleFrameTick();
			}
		}
	}

	/// <summary>
	/// Synchronous layout + render without processing loaded events.
	/// Used by Win32's SynchronousRenderAndDraw which may be called during loaded event
	/// processing (re-entrant with FrameTick). This matches the original OnRenderFrameOpportunity behavior.
	/// </summary>
	internal void SynchronousRender(bool forceLayout)
	{
		NativeDispatcher.CheckThreadAccess();

		if (ContentRoot.VisualTree.RootElement is not { } rootElement)
		{
			return;
		}

		if (forceLayout)
		{
			rootElement.UpdateLayout();
		}

		if (SkiaRenderHelper.CanRecordPicture(rootElement))
		{
			Render();
		}
	}

	/// <summary>
	/// Called from each platform's rendering logic in response to the native windowing/composition
	/// engine's signal requesting the Uno app to draw something _right now_, usually synced to the
	/// refresh rate of the screen (e.g. Android's IRenderer.OnDrawFrame).
	/// </summary>
	internal SKPath OnNativePlatformFrameRequested(SKCanvas? canvas, Func<Size, SKCanvas> resizeFunc)
	{
		this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()}: {nameof(OnNativePlatformFrameRequested)}");

		// Draw previous frame's SKPicture on render/GL thread
		var path = Draw(canvas, resizeFunc);

		// On non-Win32 hosts, OnNativePlatformFrameRequested IS the vsync callback that
		// signals "previous frame is on its way to the display, you can prepare the next".
		// By the time we get here, Draw() has already consumed the frame and released the
		// render throttle (via OnFrameConsumed). Forwarding to OnFramePresented here is
		// therefore only for the post-present "frame rendered" notification and related
		// bookkeeping (FrameRendered + FPS tracking), giving those hosts the same
		// post-present signal that Win32 gets from its render thread.
		//
		// Win32 (SupportsRenderThrottle == true) is excluded because its render thread
		// already calls OnFramePresented after SwapBuffers/BitBlt completes — auto-calling
		// here would duplicate the FrameRendered notification and FPS bookkeeping (and
		// might fire from a non-vsync moment like a WM_PAINT for window uncovering).
		//
		// Note: if the platform stops calling OnNativePlatformFrameRequested (window
		// minimised / hidden), rendering work stops as well. That matches the pre-branch
		// behaviour where the platform vsync callback was the only thing driving Render()
		// — no callback, no render. Animations resume when the platform resumes vsync
		// delivery.
		var host = ContentRoot.XamlRoot is { } xr
			? XamlRootMap.GetHostForRoot(xr)
			: null;
		if (host is not { SupportsRenderThrottle: true })
		{
			if (NativeDispatcher.Main.HasThreadAccess)
			{
				OnFramePresented();
			}
			else
			{
				NativeDispatcher.Main.Enqueue(OnFramePresented, NativeDispatcherPriority.High);
			}
		}

		return path;
	}
}
