#nullable enable

using System;

namespace Uno.UI.Composition;

/// <summary>
/// A uniform frame clock for per-frame motion to evaluate against.
/// </summary>
/// <remarks>
/// Frames present one per vsync, but the UI thread reaches each frame with milliseconds of jitter. Motion
/// evaluated against that raw instant turns the jitter into v·Δt of position error, so it gets the grid the
/// frames are actually shown on instead, recovered from the median frame interval.
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
	private long _clock;

	/// <summary>Estimated interval between presented frames, for motion that needs a nominal step.</summary>
	public long IntervalInTicks => _count >= MinSamples ? Median() : TimeSpan.TicksPerSecond / 60;

	/// <summary>
	/// Drops the grid's phase so the next timestamp re-anchors on the real clock. The sample window survives:
	/// below <see cref="MinSamples"/> the interval falls back to 1/60s, which would mis-step every motion on a
	/// display that is not 60Hz.
	/// </summary>
	public void Reset() => _lastRaw = 0;

	public long NextTimestamp(long raw)
	{
		var previous = _clock;

		if (_lastRaw == 0)
		{
			_lastRaw = raw;
			return _clock = Math.Max(raw, previous);
		}

		var delta = raw - _lastRaw;
		_lastRaw = raw;

		var period = _count >= MinSamples ? Median() : 0;

		// Admitting an idle gap would skew the median, which motion also back-dates its launch by. The absolute
		// bound matters while frames are sparse: gaps are all there is to sample, and the median would become one.
		if (delta > MaxFrameIntervalInTicks || (period > 0 && delta >= period * IdleGapPeriods))
		{
			return _clock = Math.Max(raw, previous);
		}

		_deltas[_index] = delta;
		_index = (_index + 1) % Window;
		if (_count < Window)
		{
			_count++;
		}

		if (period <= 0)
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

	private long Median()
	{
		Span<long> sorted = stackalloc long[Window];
		_deltas.AsSpan(0, _count).CopyTo(sorted);
		sorted = sorted[.._count];
		sorted.Sort();
		return sorted[_count / 2];
	}
}
