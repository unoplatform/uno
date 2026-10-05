#nullable enable

using System;

namespace Uno.UI.Composition;

/// <summary>
/// A uniform frame clock for per-frame motion to evaluate against.
/// </summary>
/// <remarks>
/// Hosts that know when each vsync happened report it, and those times are used as they are. Hosts that don't
/// only give an instant sampled after the vsync, with milliseconds of jitter that motion would turn into v·Δt of
/// position error, so those get the grid the frames are actually shown on instead, recovered from the median
/// frame interval.
/// </remarks>
internal sealed class FrameClock
{
	private const int Window = 32;
	private const int MinSamples = 8;

	// A gap this many periods long is the loop having been idle, not an interval the display ran at.
	private const int IdleGapPeriods = 4;

	// No display refreshes slower than this.
	private const long MaxFrameIntervalInTicks = TimeSpan.TicksPerSecond / 20;

	private readonly long[] _deltas = new long[Window];
	private int _index;
	private int _count;
	private long _lastRaw;
	private long _lastVsync;
	private long _clock;

	// The median of the window, refreshed only when a sample lands: it is read several times per frame.
	private long _median;

	/// <summary>Estimated interval between presented frames, for motion that needs a nominal step.</summary>
	public long IntervalInTicks => _count >= MinSamples ? _median : TimeSpan.TicksPerSecond / 60;

	/// <summary>
	/// Drops the grid's phase so the next timestamp re-anchors on the real clock. The sample window survives:
	/// below <see cref="MinSamples"/> the interval falls back to 1/60s, which would mis-step every motion on a
	/// display that is not 60Hz.
	/// </summary>
	public void Reset() => _lastRaw = 0;

	public long NextTimestamp(long raw)
	{
		var previous = _clock;
		_lastVsync = 0;

		if (_lastRaw == 0)
		{
			_lastRaw = raw;
			return _clock = Math.Max(raw, previous);
		}

		var delta = raw - _lastRaw;
		_lastRaw = raw;

		var period = _count >= MinSamples ? _median : 0;
		if (!TrySample(delta, period) || period <= 0)
		{
			return _clock = Math.Max(raw, previous);
		}

		// Advance by whole frames, never fewer than one, then correct a sixteenth of the sub-period phase.
		// Rounding unconditionally keeps a period that is a whole multiple of the tick rate from flipping
		// sides on jitter.
		var frames = Math.Max(1, (long)Math.Round((raw - _clock) / (double)period, MidpointRounding.AwayFromZero));
		_clock += frames * period;
		_clock += (raw - _clock) / 16;

		// The one-frame floor banks lead on intervals shorter than a period. Unbounded, a median that then
		// shrinks turns that lead into a run of repeated timestamps once clamped below.
		var maxLead = period / 2;
		if (_clock - raw > maxLead)
		{
			_clock = raw + maxLead;
		}

		// A backward step makes elapsed time negative, which a curve reads as "not started yet".
		return _clock = Math.Max(_clock, previous);
	}

	/// <summary>
	/// A vsync time the host reported: it is on the display's cadence already, at whatever rate the display runs,
	/// so it is only sampled for the interval and kept from stepping back.
	/// </summary>
	public long NextVsyncTimestamp(long vsync)
	{
		// The grid re-anchors if the host ever stops reporting vsyncs.
		_lastRaw = 0;

		if (vsync == _lastVsync)
		{
			return _clock;
		}

		if (_lastVsync != 0)
		{
			TrySample(vsync - _lastVsync, _count >= MinSamples ? _median : 0);
		}

		_lastVsync = vsync;
		return _clock = Math.Max(vsync, _clock);
	}

	/// <summary>
	/// The latest vsync at or before <paramref name="now"/>, for a tick no vsync armed (a driver starting between
	/// frames), extrapolated from the last one the host reported. Without one, it is <paramref name="now"/>.
	/// </summary>
	public long CurrentVsyncTimestamp(long now)
	{
		if (_lastVsync == 0 || now < _lastVsync)
		{
			return _clock = Math.Max(now, _clock);
		}

		return _clock = Math.Max(LatestVsyncAtOrBefore(_lastVsync, IntervalInTicks, now), _clock);
	}

	/// <summary>
	/// The latest vsync at or before <paramref name="now"/> on the cadence of <paramref name="anchor"/>, a vsync that
	/// may be on either side of now. Unit-agnostic.
	/// </summary>
	public static long LatestVsyncAtOrBefore(long anchor, long period, long now)
	{
		var periods = Math.DivRem(now - anchor, period, out var remainder);
		return anchor + (remainder < 0 ? periods - 1 : periods) * period;
	}

	/// <summary>
	/// How many vsyncs each frame spans when frames are <paramref name="frameInterval"/> apart, or 0 when that isn't
	/// a whole number of refresh periods. Unit-agnostic.
	/// </summary>
	public static int GetVsyncDivisor(long frameInterval, long period)
	{
		// A rate this close to a divisor is that divisor: 60fps frames on a 119.88Hz display.
		const double Tolerance = 0.02;

		if (frameInterval <= 0 || period <= 0)
		{
			return 0;
		}

		var vsyncsPerFrame = frameInterval / (double)period;
		var divisor = Math.Round(vsyncsPerFrame);
		return divisor >= 1 && Math.Abs(vsyncsPerFrame - divisor) <= divisor * Tolerance ? (int)divisor : 0;
	}

	/// <returns>Whether the interval was admitted.</returns>
	private bool TrySample(long delta, long period)
	{
		// Admitting an idle gap would skew the median, which motion also back-dates its launch by. The absolute
		// bound matters while frames are sparse: gaps are all there is to sample, and the median would become one.
		if (delta <= 0 || delta > MaxFrameIntervalInTicks || (period > 0 && delta >= period * IdleGapPeriods))
		{
			return false;
		}

		_deltas[_index] = delta;
		_index = (_index + 1) % Window;
		if (_count < Window)
		{
			_count++;
		}

		if (_count >= MinSamples)
		{
			_median = Median();
		}

		return true;
	}

	private long Median()
	{
		Span<long> sorted = stackalloc long[Window];
		_deltas.AsSpan(0, _count).CopyTo(sorted);
		sorted = sorted[.._count];
		sorted.Sort();
		return sorted[_count / 2];
	}
}
