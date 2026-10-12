#nullable enable

using System;
using System.Diagnostics;
using System.Threading;

namespace Uno.UI.Composition;

/// <summary>
/// Decides when a host's render thread starts its next frame, and the vsync that frame belongs to. Times are
/// <see cref="Stopwatch.GetTimestamp"/> values.
/// </summary>
/// <remarks>
/// With the display's vsync grid known, a request starts a frame right away when the current vsync interval has no
/// frame yet and there is still time to make it (Chromium's "missed BeginFrame"), so input never waits for a vsync;
/// otherwise it waits for the next vsync. Either way the frame is stamped with its interval's vsync, and sustained
/// animation starts each frame on one. Without a grid, frames are paced by an interval like a timer.
/// </remarks>
internal sealed class VsyncFrameScheduler
{
	// Weight of a slower frame in the draw-time estimate.
	private const int DrawEstimateWeight = 8;

	// How long before a vsync the compositor takes the frames it shows on the vsync after (~2ms measured on macOS).
	internal static readonly long CompositorLatch = Stopwatch.Frequency / 500;

	private long _intervalTicks;
	private long _nextTimerStart;
	private long _lastFrameStart;
	private long? _lastFrameVsync;
	private bool _nextFrameIsImmediate;
	private bool _lastFrameWasExempt;
	private long _drawEstimate;

	public VsyncFrameScheduler(double fps) => SetFrameRate(fps);

	/// <summary>The rate frames are paced at when no vsync grid is known. Thread-safe.</summary>
	public void SetFrameRate(double fps)
	{
		if (fps > 0)
		{
			Interlocked.Exchange(ref _intervalTicks, (long)(Stopwatch.Frequency / fps));
		}
	}

	/// <summary>
	/// The next frame's start time and vsync. <paramref name="latestVsync"/> is the display's latest vsync at or
	/// before <paramref name="now"/>, when it is known and frames should follow it.
	/// </summary>
	public (long Start, long? Vsync) GetNextFrame(long now, long? latestVsync, long vsyncPeriod)
	{
		if (latestVsync is not { } vsync || vsyncPeriod <= 0)
		{
			var interval = Interlocked.Read(ref _intervalTicks);
			var start = Math.Clamp(_nextTimerStart, now, now + interval);
			_nextFrameIsImmediate = start == now;
			return (start, null);
		}

		// Past this, a frame started now misses the same vsync one started on the next vsync does, and queues ahead of it.
		var deadline = vsync + vsyncPeriod - Math.Min(_drawEstimate + CompositorLatch, vsyncPeriod / 2);
		if (now > deadline)
		{
			vsync += vsyncPeriod;
		}

		// A frame paced without the grid belongs to the interval it started in.
		var lastFrameVsync = _lastFrameVsync
			?? vsync + (long)Math.Floor((_lastFrameStart - vsync) / (double)vsyncPeriod) * vsyncPeriod;

		// One frame per vsync interval. Half a period of slack absorbs vsync times that jitter around the grid.
		while (vsync - lastFrameVsync <= vsyncPeriod / 2)
		{
			vsync += vsyncPeriod;
		}

		_nextFrameIsImmediate = vsync <= now;
		return (Math.Max(vsync, now), vsync);
	}

	/// <summary>
	/// Records a frame started at <paramref name="start"/> for <paramref name="vsync"/>, if known.
	/// <paramref name="unchanged"/> is a frame the window already showed, so it needed no present.
	/// </summary>
	public void OnFrame(long start, long? vsync, bool unchanged)
	{
		// Input after an idle period asks for a frame before the UI thread has recorded the change. That frame shows
		// nothing new, so once it leaves its interval to the one that does.
		if (unchanged && _nextFrameIsImmediate && !_lastFrameWasExempt)
		{
			_lastFrameWasExempt = true;
			return;
		}

		_lastFrameWasExempt = false;
		_lastFrameStart = start;
		_lastFrameVsync = vsync;

		var interval = Interlocked.Read(ref _intervalTicks);
		_nextTimerStart = _nextTimerStart + interval > start ? _nextTimerStart + interval : start + interval;
	}

	/// <summary>
	/// Records how long a presented frame took from its start to its present. The estimate follows faster frames at
	/// once and slower ones gradually, so a present that blocked on a drawable doesn't inflate it.
	/// </summary>
	public void OnFrameDrawn(long duration)
		=> _drawEstimate = _drawEstimate == 0 || duration < _drawEstimate
			? duration
			: _drawEstimate + (duration - _drawEstimate) / DrawEstimateWeight;
}
