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
	//                           |                                       | ScheduleFrameTick (at Render priority)
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
		if (_pendingFrameRequest)
		{
			_pendingFrameRequest = false;
			ScheduleFrameTick();
		}
	}

	/// <summary>
	/// Schedules a single batched frame tick at Render priority if one is not already scheduled.
	/// Thread-safe: can be called from the UI thread or the native render thread.
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
				NativeDispatcher.Main.Enqueue(_frameTickCallback, NativeDispatcherPriority.Render);
			}
		}
	}

	/// <summary>
	/// Single batched frame tick, posted to the dispatcher as one operation at Render priority.
	/// Sequences: Layout -> Loaded events -> Layout -> CompositionTarget.Rendering -> Layout -> Render.
	/// Matches WPF MediaContext.RenderMessageHandlerCore / WinUI NWDrawTree OnTick pattern.
	/// </summary>
	internal void FrameTick()
	{
		NativeDispatcher.CheckThreadAccess();

		if (_inFrameTick)
		{
			// Re-entrant call (e.g. loaded event handler → Window.Show → SynchronousRenderAndDraw).
			// Skip to avoid corrupting the loaded event list iteration. The outer FrameTick
			// will schedule another tick after completing.
			_reentrantFrameRequested = true;
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
		return Draw(canvas, resizeFunc);
	}
}
