#nullable enable

using System;

namespace Uno.UI.Composition;

/// <summary>
/// A uniform frame clock for per-frame motion drivers to evaluate against.
/// </summary>
/// <remarks>
/// Frames present one per vsync but ticks are not scheduled on one, so the raw clock wobbles by
/// milliseconds around a cadence that is otherwise exact. A driver whose position is a function of time
/// turns that wobble into v·Δt of position error, which at scroll speeds is a visible fraction of a
/// frame step — so drivers get the grid the frames are actually shown on, recovered from the median
/// tick interval, rather than the instant the UI thread happened to get here.
/// </remarks>
internal sealed class FrameClock
{
	private const int Window = 32;
	private const int MinSamples = 8;

	private readonly long[] _deltas = new long[Window];
	private int _index;
	private int _count;
	private long _lastRaw;
	private long _clock;

	/// <summary>Estimated interval between presented frames, for drivers that need a nominal step.</summary>
	public long IntervalInTicks => _count >= MinSamples ? Median() : TimeSpan.TicksPerSecond / 60;

	/// <summary>
	/// Drops the grid's phase, which means nothing across a gap between motions. The sample window
	/// deliberately survives: below <see cref="MinSamples"/> the interval falls back to 1/60s, which drivers
	/// back-date a launch by, so clearing it would mis-launch every motion on a display that is not 60Hz.
	/// </summary>
	public void Reset() => _lastRaw = 0;

	public long NextTimestamp(long raw)
	{
		if (_lastRaw == 0)
		{
			_lastRaw = raw;
			return _clock = raw;
		}

		var delta = raw - _lastRaw;
		_lastRaw = raw;

		var period = _count >= MinSamples ? Median() : 0;

		// A gap far longer than a frame is the loop having been idle between motions, not an interval the
		// display ever ran at. Admitting it would skew the median, which also sets the interval a fling
		// back-dates its launch by — turning a timing artefact into a position error.
		if (period <= 0 || delta < period * 4)
		{
			_deltas[_index] = delta;
			_index = (_index + 1) % Window;
			if (_count < Window)
			{
				_count++;
			}
		}

		if (period <= 0)
		{
			return _clock = raw;
		}

		var previous = _clock;

		// Advance by whole frames, never fewer than one, correct the sub-period phase, then bound how far the
		// grid may lead real time. Rounding unconditionally rather than branching once the error passes a
		// threshold keeps a period that is a whole multiple of the tick rate from flipping sides on jitter.
		var frames = Math.Max(1, (long)Math.Round((raw - _clock) / (double)period, MidpointRounding.AwayFromZero));
		_clock += frames * period;
		_clock += (raw - _clock) / 16;

		// The one-frame floor banks lead on ticks shorter than a period; left unbounded, a median period that
		// then shrinks turns that lead into a backward step, which the clamp below freezes into a run of
		// repeated timestamps. Half a period, so a re-anchor after an idle gap still lands within one.
		var maxLead = period / 2;
		if (_clock - raw > maxLead)
		{
			_clock = raw + maxLead;
		}

		// Monotone by construction above, asserted here so it stays that way under any future edit: a
		// backward step makes a fling's elapsed time negative, and the curve reads that as "not started"
		// and snaps the content back to where the flick began.
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
