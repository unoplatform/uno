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
	/// Protects <see cref="_waitingForPresent"/> + <see cref="_pendingFrameRequest"/> reads
	/// in <see cref="ScheduleFrameTick"/> against UI-thread writes in <see cref="FrameTick"/> /
	/// <see cref="OnFramePresented"/>. <see cref="ICompositionTarget.RequestNewFrame"/> can be
	/// called from non-UI threads, so a lock is needed (volatile alone leaves a check-then-act
	/// race that drops or duplicates schedules).
	/// </summary>
	private readonly Lock _scheduleGate = new();

	/// <summary>
	/// Throttle flag: when true, ScheduleFrameTick defers until OnFramePresented clears it.
	/// Only set on hosts with a render thread (SupportsRenderThrottle == true).
	/// Prevents the UI thread from recording frames faster than the render thread presents.
	/// Read/written under <see cref="_scheduleGate"/>.
	/// </summary>
	private bool _waitingForPresent;

	/// <summary>
	/// Set when ScheduleFrameTick is called while throttled. OnFramePresented schedules
	/// the deferred FrameTick when it clears the throttle.
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
	/// Called by the host's render thread (posted to UI thread) after a frame is presented.
	/// Clears the throttle and schedules the next FrameTick if one was deferred.
	/// </summary>
	internal void OnFramePresented()
	{
		NativeDispatcher.CheckThreadAccess();
		bool reschedule;
		lock (_scheduleGate)
		{
			_waitingForPresent = false;
			reschedule = _pendingFrameRequest;
			_pendingFrameRequest = false;
		}

		// Throttled hosts (Win32) fire FrameRendered here — this is the actual "frame on screen"
		// moment, matching WinUI's CompositionTarget.Rendered semantics. Unthrottled hosts fire
		// it from inside Render() since they have no present-completion callback to hook.
		FrameRendered?.Invoke();

		if (reschedule)
		{
			ScheduleFrameTick();
		}
	}

	/// <summary>
	/// Schedules a single batched frame tick if one is not already scheduled.
	/// Thread-safe: can be called from the UI thread or the native render thread.
	/// </summary>
	internal void ScheduleFrameTick()
	{
		lock (_scheduleGate)
		{
			if (_waitingForPresent)
			{
				// Render thread hasn't finished presenting the previous frame.
				// Remember the request — it will be scheduled when OnFramePresented fires.
				_pendingFrameRequest = true;
				return;
			}

			if (_frameTickScheduled)
			{
				return;
			}

			_frameTickScheduled = true;
		}

		// Schedule at Normal priority. Coalescing (above) plus the per-host throttle
		// (_waitingForPresent on hosts with a render thread) bound frame production to
		// one in-flight tick per present, so the priority value doesn't need to outrun
		// other UI work — putting FrameTick above or below Normal trades freezes for
		// UI lock during animation. Equal priority + FIFO leaves neither side starved.
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
			// Resolve the host once for this tick (used for throttle).
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

			// 3. CompositionTarget.Rendering event (may dirty layout). Wall-clock based;
			//    a vsync-aligned timestamp would be more accurate but no host plumbs one
			//    through yet — see follow-up F2.
			InvokeRendering(Stopwatch.GetElapsedTime(_start));

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
					lock (_scheduleGate)
					{
						_waitingForPresent = true;
					}
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
		return Draw(canvas, resizeFunc);
	}
}
