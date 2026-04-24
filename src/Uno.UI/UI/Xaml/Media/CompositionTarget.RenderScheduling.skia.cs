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
	//                           |                                       | ScheduleFrameTick (at Normal priority, coalesced)
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
	/// Guards against scheduling duplicate frame ticks. Read/written under <see cref="_scheduleGate"/>.
	/// </summary>
	private bool _frameTickScheduled;

	/// <summary>
	/// Re-entrancy guard for FrameTick. FrameTick can be called re-entrantly when loaded
	/// event handlers trigger SynchronousRenderAndDraw (e.g., during Window.Show on Win32).
	/// </summary>
	private bool _inFrameTick;

	/// <summary>
	/// Guards frame-scheduling state against concurrent access. Two distinct uses:
	///  - <see cref="ScheduleFrameTick"/> uses it for the <see cref="_frameTickScheduled"/>
	///    check-then-act that coalesces duplicate schedules.
	///  - <see cref="FrameTick"/> and <see cref="OnFrameConsumed"/> use it for the
	///    throttle fields (<see cref="_waitingForPresent"/>, <see cref="_pendingFrameRequest"/>)
	///    so the FrameTick "throttled? → set pending : arm throttle" sequence is atomic
	///    against the OnFrameConsumed "clear throttle, read pending" sequence.
	/// <see cref="ICompositionTarget.RequestNewFrame"/> can be called from non-UI threads,
	/// so a lock is needed — volatile alone leaves check-then-act races that drop or
	/// duplicate schedules.
	/// </summary>
	private readonly Lock _scheduleGate = new();

	/// <summary>
	/// Throttle flag: when true, <see cref="FrameTick"/> defers the render-side work
	/// (Rendering event + Render call) and sets <see cref="_pendingFrameRequest"/> so
	/// <see cref="OnFrameConsumed"/> can reschedule. Set before Render() runs in
	/// FrameTick. Cleared by <see cref="OnFrameConsumed"/>, which is called from
	/// <see cref="Draw"/> when the render thread borrows the recorded SKPicture.
	///
	/// This enables pipelining on hosts with dedicated render threads (Win32): the
	/// throttle clears as soon as the render thread picks up the frame, allowing the
	/// UI thread to prepare the next frame while the current one is being presented
	/// (SwapBuffers/BitBlt). The pipeline depth is naturally capped at 1 frame ahead
	/// because recording a new frame re-arms the throttle.
	///
	/// On hosts without a render thread (WASM, Android GL thread), Draw runs at vsync
	/// time so the pipeline doesn't overlap, but clearing at pickup is still correct.
	///
	/// Read/written under <see cref="_scheduleGate"/>.
	/// </summary>
	private bool _waitingForPresent;

	/// <summary>
	/// Set inside <see cref="FrameTick"/> when the throttle defers the render-side work
	/// for this tick. <see cref="OnFrameConsumed"/> schedules the deferred FrameTick
	/// when it clears the throttle.
	/// Read/written under <see cref="_scheduleGate"/>.
	/// </summary>
	private bool _pendingFrameRequest;

	/// <summary>
	/// Set when FrameTick is called re-entrantly (e.g. loaded event → Window.Show → SynchronousRenderAndDraw).
	/// The outer FrameTick schedules another tick after completing if this is set.
	/// </summary>
	private bool _reentrantFrameRequested;

	/// <summary>
	/// Cached entry-point delegate for the dispatcher. Lazy-initialised once per
	/// CompositionTarget to avoid per-frame closure allocation
	/// (~3,600/min during 60 fps animation).
	/// </summary>
	private Action? _frameTickEntry;

	void ICompositionTarget.RequestNewFrame()
	{
		// Only schedule the next FrameTick. Don't call host.InvalidateRender() here —
		// the render thread is signaled from Render() after a new frame is recorded.
		ScheduleFrameTick();
	}

	/// <summary>
	/// Called from <see cref="Draw"/> when the render/GL thread borrows the recorded
	/// SKPicture. Clears the throttle so the UI thread can record the next frame while
	/// this one is being presented — enabling pipelining on hosts with dedicated render
	/// threads (Win32).
	///
	/// Thread-safe: called from the render thread (Win32), GL thread (Android), or
	/// UI thread (WASM).
	/// </summary>
	private void OnFrameConsumed()
	{
		bool reschedule;
		lock (_scheduleGate)
		{
			_waitingForPresent = false;
			reschedule = _pendingFrameRequest;
			_pendingFrameRequest = false;
		}

		if (reschedule)
		{
			ScheduleFrameTick();
		}
	}

	/// <summary>
	/// Called after a frame is actually presented (pixels on screen).
	/// On Win32: posted to the UI thread by the render thread after SwapBuffers/BitBlt.
	/// On others: called from <see cref="OnNativePlatformFrameRequested"/> after Draw.
	/// Fires the <see cref="FrameRendered"/> event (WinUI CompositionTarget.Rendered
	/// semantics). Throttle was already cleared by <see cref="OnFrameConsumed"/>.
	/// </summary>
	internal void OnFramePresented()
	{
		NativeDispatcher.CheckThreadAccess();
		_fpsHelper.OnFramePresentCompleted();
		FrameRendered?.Invoke();
	}

	/// <summary>
	/// Schedules a single batched frame tick if one is not already scheduled.
	/// Thread-safe: can be called from the UI thread or the native render thread.
	///
	/// FrameTick is ALWAYS enqueued when scheduled — the render throttle
	/// (<see cref="_waitingForPresent"/>) is NOT checked here. This matches WinUI's
	/// NWDrawTree which is always dispatched via DispatcherQueueTimer
	/// (xcpwindow.cpp:1287-1289 — "ticking will be interleaved with input").
	/// Keeping FrameTick visible to the dispatcher means Low-priority work like
	/// <c>RunIdleAsync</c> naturally waits for it. The throttle is enforced inside
	/// <see cref="FrameTick"/> instead, around the Rendering event + Render call.
	/// </summary>
	internal void ScheduleFrameTick()
	{
		lock (_scheduleGate)
		{
			if (_frameTickScheduled)
			{
				return;
			}

			_frameTickScheduled = true;
		}

		// Schedule at Normal priority. Coalescing (above) plus the in-FrameTick render
		// throttle bound frame production to one Render() per present, so the priority
		// value doesn't need to outrun other UI work — putting FrameTick above or below
		// Normal trades freezes for UI lock during animation. Equal priority + FIFO leaves
		// neither side starved.
		NativeDispatcher.Main.Enqueue(_frameTickEntry ??= RunFrameTickEntry, NativeDispatcherPriority.Normal);
	}

	private void RunFrameTickEntry()
	{
		lock (_scheduleGate)
		{
			_frameTickScheduled = false;
		}
		FrameTick();
	}

	/// <summary>
	/// Single batched frame tick, posted to the dispatcher as one operation.
	/// Sequences: Layout -> Loaded events -> Layout -> CompositionTarget.Rendering -> Layout -> Render.
	/// Matches WPF MediaContext.RenderMessageHandlerCore / WinUI NWDrawTree OnTick pattern.
	///
	/// Pipelining: on hosts with a dedicated render thread (Win32), the throttle clears
	/// when the render thread picks up the previous frame (see <see cref="OnFrameConsumed"/>),
	/// NOT when it finishes presenting. This allows the UI thread to record Frame N+1
	/// while the render thread presents Frame N, overlapping CPU and GPU work. The pipeline
	/// depth is capped at 1 frame ahead: recording re-arms the throttle, so if the render
	/// thread hasn't picked up the last frame yet, this tick defers render-side work.
	///
	/// Trade-off: layout always runs before render here, which is structurally cleaner
	/// but means composition-only animations (e.g. CompositionTarget.Rendering subscribers
	/// that mutate composition-layer properties without dirtying layout) still pay one
	/// UpdateLayout walk per frame. UpdateLayout short-circuits on a clean tree so the
	/// cost is tolerable. The longer-term fix is the FrameScheduler abstraction (which
	/// lets composition-only frames bypass layout entirely).
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
	///  • Wall-clock vs tick-aligned time. WinUI passes m_pTimeManager->GetLastTickTime() to the
	///    Rendering event, so subscribers and the animation system see the same "now". We pass
	///    Stopwatch.GetElapsedTime(_start), which drifts by however long the tick has taken to
	///    reach this point. Becomes consistent once a vsync-aligned timestamp is plumbed through
	///    via a future FrameVsyncTimestamp / FrameScheduler.
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

			// WinUI fail-fasts on tick re-entry (XAML_FAIL_FAST in NWDrawTree). We're more
			// lenient because shipping fail-fast on user-handler re-entry would be too
			// aggressive, but it's still a sign of a third-party handler doing something
			// unexpected — log so the divergence is observable.
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn(
					$"CompositionTarget#{GetHashCode()}: FrameTick re-entered. " +
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
			// 1. Layout pass — ALWAYS runs (visible to dispatcher / WaitForIdle).
			rootElement.UpdateLayout();

			// 2. Loaded events — ALWAYS run (may dirty layout again).
			if (CoreServices.Instance.EventManager.ShouldRaiseLoadedEvent)
			{
				CoreServices.Instance.EventManager.RaiseLoadedEvent();
				rootElement.UpdateLayout();
			}

			// 3-5. Render-side work (CompositionTarget.Rendering + post-Rendering layout
			//      + Render). Gated by the throttle: if the render thread hasn't picked
			//      up the previous frame yet, skip render-side work this tick.
			//      OnFrameConsumed (called from Draw when the frame is borrowed) will
			//      reschedule when the throttle clears.
			//
			//      On hosts with a dedicated render thread (Win32), the throttle clears
			//      as soon as the render thread starts drawing — allowing the UI thread
			//      to prepare the next frame in parallel (pipelining). On single-threaded
			//      hosts (WASM), Draw runs at vsync time so no overlap occurs.
			//
			//      Why gated *here* rather than at ScheduleFrameTick: gating at scheduling
			//      time would defer FrameTick outside the dispatcher queue, hiding it from
			//      RunIdleAsync. WinUI never does that (xcpwindow.cpp:1287-1289 — its tick
			//      is always dispatched via DispatcherQueueTimer). By keeping FrameTick
			//      visible to the dispatcher and gating only Rendering+Render, layout/Loaded
			//      always settle on time for tests while animation rate stays vsync-paced
			//      (Rendering only fires when not throttled, so animations advance once per
			//      displayed frame).
			if (SkiaRenderHelper.CanRecordPicture(rootElement))
			{
				bool shouldRender;
				lock (_scheduleGate)
				{
					if (_waitingForPresent)
					{
						_pendingFrameRequest = true;
						shouldRender = false;
					}
					else
					{
						_waitingForPresent = true;
						shouldRender = true;
					}
				}

				if (shouldRender)
				{
					// CompositionTarget.Rendering event (may dirty layout). Wall-clock based;
					// a vsync-aligned timestamp would be more accurate but no host plumbs one
					// through yet.
					InvokeRendering(Stopwatch.GetElapsedTime(_start));

					// Re-layout after Rendering callbacks (matches WinUI NWDrawTree which runs
					// UpdateLayout after CallPerFrameCallback). Without this, Rendering handlers
					// that modify layout-affecting properties would render with stale layout.
					rootElement.UpdateLayout();

					Render();
				}
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
	/// refresh rate of the screen (e.g. Android's IRenderer.OnDrawFrame, WASM's requestAnimationFrame).
	/// May be called on the UI thread (WASM) or on a background thread (Android GL thread).
	/// </summary>
	internal SKPath OnNativePlatformFrameRequested(SKCanvas? canvas, Func<Size, SKCanvas> resizeFunc)
	{
		this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()}: {nameof(OnNativePlatformFrameRequested)}");

		// Draw previous frame's SKPicture on render/GL thread
		var path = Draw(canvas, resizeFunc);

		// On non-Win32 hosts, OnNativePlatformFrameRequested IS the vsync callback that
		// signals "previous frame is on its way to the display, you can prepare the next".
		// Forward it to OnFramePresented to release the throttle armed in FrameTick — this
		// gives those hosts the same 1-FrameTick-per-vsync pacing Win32 gets via its render
		// thread, and prevents the dispatcher from being saturated by FrameTicks during
		// continuous animation (which would also starve idle work).
		//
		// Win32 (SupportsRenderThrottle == true) is excluded because its render thread
		// already calls OnFramePresented after SwapBuffers/BitBlt completes — auto-calling
		// here would be a redundant clear (and might fire from a non-vsync moment like
		// a WM_PAINT for window uncovering).
		//
		// Note: if the platform stops calling OnNativePlatformFrameRequested (window
		// minimised / hidden), the throttle stays armed and FrameTicks halt. That matches
		// the pre-branch behaviour where the platform vsync callback was the only thing
		// driving Render() — no callback, no render. Animations resume when the platform
		// resumes vsync delivery.
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
