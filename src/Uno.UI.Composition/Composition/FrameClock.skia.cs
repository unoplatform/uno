#nullable enable

using System;

namespace Uno.UI.Composition;

/// <summary>
/// The timestamps per-frame motion evaluates against, and an estimate of the interval between frames.
/// </summary>
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

	// The median of the window, refreshed only when a sample lands: it is read several times per frame.
	private long _median;

	/// <summary>Estimated interval between presented frames, for motion that needs a nominal step.</summary>
	public long IntervalInTicks => _count >= MinSamples ? _median : TimeSpan.TicksPerSecond / 60;

	/// <summary>
	/// Forgets the last frame, so the gap to the next one is not sampled as an interval. The sample window
	/// survives: below <see cref="MinSamples"/> the interval falls back to 1/60s, which would mis-step every
	/// motion on a display that is not 60Hz.
	/// </summary>
	public void Reset() => _lastRaw = 0;

	public long NextTimestamp(long raw)
	{
		if (_lastRaw != 0)
		{
			Sample(raw - _lastRaw);
		}

		_lastRaw = raw;

		// A backward step makes elapsed time negative, which a curve reads as "not started yet".
		return _clock = Math.Max(raw, _clock);
	}

	private void Sample(long delta)
	{
		var period = _count >= MinSamples ? _median : 0;

		// Admitting an idle gap would skew the median, which motion also back-dates its launch by. The absolute
		// bound matters while frames are sparse: gaps are all there is to sample, and the median would become one.
		if (delta > MaxFrameIntervalInTicks || (period > 0 && delta >= period * IdleGapPeriods))
		{
			return;
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
