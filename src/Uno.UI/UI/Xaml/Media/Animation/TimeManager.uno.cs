#nullable enable

using System;
using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;

namespace Microsoft.UI.Xaml.Media.Animation;

/// <summary>The part of WinUI's IFrameScheduler the timing tree uses to ask for its next tick.</summary>
internal interface IFrameScheduler
{
	/// <summary>Requests a tick in <paramref name="delayInMilliseconds"/>, or on the next frame when 0.</summary>
	void RequestAdditionalFrame(uint delayInMilliseconds);
}

partial class TimeManager
{
	private static TimeManager? _instance;

	private FrameScheduler? _frameScheduler;
	private double _lastFrameTimeInSeconds;
	private bool _hasTickedThisFrame;

	/// <summary>The time manager of the core (CCoreServices::GetTimeManager).</summary>
	internal static TimeManager Instance => _instance ??= new TimeManager();

	internal IFrameScheduler GetFrameScheduler() => _frameScheduler ??= new FrameScheduler(this);

	internal static void RequestAdditionalFrame(uint delayInMilliseconds = 0)
		=> Instance.GetFrameScheduler().RequestAdditionalFrame(delayInMilliseconds);

	// RefreshAlignedClock::GetLastTickTimeInSeconds: the time of the frame being produced.
	private double GetLastTickTimeInSeconds() => _lastFrameTimeInSeconds;

	// RefreshAlignedClock::GetNextTickTimeInSeconds
	private static double GetNextTickTimeInSeconds() => GetCurrentClockTimeInSeconds();

	private static double GetCurrentClockTimeInSeconds()
		=> Compositor.GetSharedCompositor().TimestampInTicks / (double)TimeSpan.TicksPerSecond;

	/// <summary>
	/// The first tick of a frame (CCoreServices::Tick, before layout).
	/// </summary>
	private void TickFrame(double frameTimeInSeconds)
	{
		_lastFrameTimeInSeconds = frameTimeInSeconds;
		_hasTickedThisFrame = true;

		try
		{
			Tick(newTimelinesOnly: false, GetFrameScheduler());
		}
		catch
		{
			Uno.UI.Xaml.Core.CoreServices.Instance.FlushDeferredAnimationOperationQueue(doDeferredOperation: false);
			throw;
		}

		// If there are any pending SetValue calls (queued during Tick), invoke them now (CCoreServices::Tick).
		Uno.UI.Xaml.Core.CoreServices.Instance.FlushDeferredAnimationOperationQueue();
	}

	/// <summary>
	/// The second tick of a frame, after layout: ticks the timelines begun during layout (Loaded handlers, visual state
	/// changes) so their first values are part of the frame being rendered.
	/// </summary>
	internal void TickNewTimelinesAfterLayout()
	{
		if (_hasTickedThisFrame)
		{
			_hasTickedThisFrame = false;
			Tick(newTimelinesOnly: true, GetFrameScheduler());
		}
	}

	private sealed class RootTimeline : Timeline
	{
	}

	/// <summary>
	/// Turns tick requests into frame-driver ticks (immediate) or timers (delayed).
	/// </summary>
	/// <remarks>
	/// Frames stop while no window shows anything (hidden or minimized), but time does not: a fallback timer ticks directly
	/// when a requested frame is overdue, so storyboards still progress and complete.
	/// </remarks>
	private sealed class FrameScheduler : IFrameScheduler
	{
		private const double FallbackTickIntervalInSeconds = 0.1;

		private readonly TimeManager _owner;
		private bool _isFrameRequested;
		private bool _isSubscribed;
		private double _lastTickRealTime;

		private DispatcherQueueTimer? _delayTimer;
		private double _delayTimerDueTime = double.MaxValue;

		private DispatcherQueueTimer? _fallbackTimer;

		public FrameScheduler(TimeManager owner) => _owner = owner;

		public void RequestAdditionalFrame(uint delayInMilliseconds)
		{
			if (delayInMilliseconds == 0)
			{
				RequestImmediateFrame();
			}
			else
			{
				ScheduleDelayedFrame(delayInMilliseconds);
			}
		}

		private void RequestImmediateFrame()
		{
			if (_isFrameRequested)
			{
				return;
			}

			_isFrameRequested = true;

			if (!_isSubscribed)
			{
				_isSubscribed = true;
				Compositor.GetSharedCompositor().FrameTicks.FrameStarting += OnFrameStarting;
			}

			EnsureFallbackTimer();
		}

		private void OnFrameStarting(object? sender, long timestamp) => Tick(timestamp / (double)TimeSpan.TicksPerSecond);

		private void Tick(double frameTimeInSeconds)
		{
			_isFrameRequested = false;
			_lastTickRealTime = GetCurrentClockTimeInSeconds();

			_owner.TickFrame(frameTimeInSeconds);

			if (!_isFrameRequested && _isSubscribed)
			{
				_isSubscribed = false;
				Compositor.GetSharedCompositor().FrameTicks.FrameStarting -= OnFrameStarting;
			}
		}

		private void EnsureFallbackTimer()
		{
			if (_fallbackTimer is null)
			{
				_fallbackTimer = new DispatcherQueueTimer { IsRepeating = true, Interval = TimeSpan.FromSeconds(FallbackTickIntervalInSeconds) };
				_fallbackTimer.Tick += OnFallbackTimerTick;
			}

			if (!_fallbackTimer.IsRunning)
			{
				_lastTickRealTime = GetCurrentClockTimeInSeconds();
				_fallbackTimer.Start();
			}
		}

		private void OnFallbackTimerTick(DispatcherQueueTimer sender, object args)
		{
			if (!_isFrameRequested)
			{
				sender.Stop();
				return;
			}

			var now = GetCurrentClockTimeInSeconds();
			if (now - _lastTickRealTime >= FallbackTickIntervalInSeconds)
			{
				Tick(now);

				// Not part of a frame: there is no layout pass to follow.
				_owner._hasTickedThisFrame = false;
			}
		}

		private void ScheduleDelayedFrame(uint delayInMilliseconds)
		{
			var dueTime = GetCurrentClockTimeInSeconds() + delayInMilliseconds / 1000.0;
			if (_delayTimer is { IsRunning: true } && dueTime >= _delayTimerDueTime)
			{
				return;
			}

			if (_delayTimer is null)
			{
				_delayTimer = new DispatcherQueueTimer { IsRepeating = false };
				_delayTimer.Tick += OnDelayTimerTick;
			}

			_delayTimer.Stop();
			_delayTimerDueTime = dueTime;
			_delayTimer.Interval = TimeSpan.FromMilliseconds(delayInMilliseconds);
			_delayTimer.Start();
		}

		private void OnDelayTimerTick(DispatcherQueueTimer sender, object args)
		{
			_delayTimerDueTime = double.MaxValue;
			RequestImmediateFrame();
		}
	}
}
