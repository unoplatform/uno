#if __SKIA__
using System;
using System.Linq;
using Microsoft.UI.Composition;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

[TestClass]
public class Given_Compositor
{
	private const long Period = TimeSpan.TicksPerSecond / 120;

	[TestMethod]
	[RunsOnUIThread]
	public void When_Skia_Backend_Then_IsSoftwareRenderer_Populated()
	{
		// Every Skia render backend must report whether it rasterizes on the CPU as soon as
		// its renderer is selected; effect brushes rely on this while recording the scene.
		Assert.IsNotNull(Compositor.GetSharedCompositor().IsSoftwareRenderer);
	}

	/// <summary>
	/// Frames present one per vsync but are not recorded on one, so a driver evaluating against the raw
	/// record instant turns that wobble into v·Δt of position error. The frame clock must hand drivers
	/// the cadence the frames are shown on instead.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public void When_Records_Jitter_Then_Frame_Clock_Steps_Evenly()
	{
		var clock = new Uno.UI.Composition.FrameClock();
		var random = new Random(42);

		var raw = new long[400];
		var stamps = new long[400];
		for (var i = 0; i < raw.Length; i++)
		{
			// ±1.5ms of record-phase wobble around an exact 120Hz cadence.
			raw[i] = TimeSpan.TicksPerSecond + i * Period + (long)((random.NextDouble() - 0.5) * 3 * TimeSpan.TicksPerMillisecond);
			stamps[i] = clock.NextTimestamp(raw[i]);
		}

		var rawWorst = Deltas(raw).Skip(100).Max(d => Math.Abs(d - Period));
		var clockWorst = Deltas(stamps).Skip(100).Max(d => Math.Abs(d - Period));

		Assert.IsTrue(rawWorst > Period / 4, $"the raw clock should be visibly uneven, was {Ms(rawWorst)}ms off");
		Assert.IsTrue(clockWorst < Period / 16, $"the frame clock should be even, was {Ms(clockWorst)}ms off");
	}

	/// <summary>The grid may not drift: a driver reaching the end of its curve must do so on time.</summary>
	[TestMethod]
	[RunsOnUIThread]
	public void When_Records_Jitter_Then_Frame_Clock_Does_Not_Drift()
	{
		var clock = new Uno.UI.Composition.FrameClock();
		var random = new Random(7);

		long first = 0, last = 0, firstRaw = 0, lastRaw = 0;
		for (var i = 0; i < 400; i++)
		{
			var raw = TimeSpan.TicksPerSecond + i * Period + (long)((random.NextDouble() - 0.5) * 3 * TimeSpan.TicksPerMillisecond);
			var stamp = clock.NextTimestamp(raw);

			if (i == 100)
			{
				(first, firstRaw) = (stamp, raw);
			}

			(last, lastRaw) = (stamp, raw);
		}

		Assert.IsTrue(Math.Abs((last - first) - (lastRaw - firstRaw)) < Period, "the frame clock drifted from the real one");
	}

	/// <summary>
	/// A record that overruns its vsync holds the previous picture on screen for two intervals, so the
	/// motion has to cover both — smoothing that away would show as a dropped frame plus a slow one.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public void When_Frame_Dropped_Then_Frame_Clock_Steps_Twice()
	{
		var clock = new Uno.UI.Composition.FrameClock();

		var stamps = new long[60];
		for (var i = 0; i < stamps.Length; i++)
		{
			// One record overruns, so the frames after it sit a whole interval later.
			var raw = TimeSpan.TicksPerSecond + i * Period + (i >= 40 ? Period : 0);
			stamps[i] = clock.NextTimestamp(raw);
		}

		Assert.AreEqual(2 * Period, stamps[40] - stamps[39], "the overrun frame should advance by two intervals");
		Assert.AreEqual(Period, stamps[45] - stamps[44], "the cadence should resume immediately after");
	}

	/// <summary>After an idle gap the grid's phase means nothing, so it must re-anchor rather than crawl.</summary>
	[TestMethod]
	[RunsOnUIThread]
	public void When_Loop_Goes_Idle_Then_Frame_Clock_Reanchors()
	{
		var clock = new Uno.UI.Composition.FrameClock();

		long stamp = 0, raw = 0;
		for (var i = 0; i < 60; i++)
		{
			raw = TimeSpan.TicksPerSecond + i * Period;
			stamp = clock.NextTimestamp(raw);
		}

		raw += 5 * TimeSpan.TicksPerSecond;
		stamp = clock.NextTimestamp(raw);

		Assert.IsTrue(Math.Abs(stamp - raw) < Period, $"expected to re-anchor near the real clock, was {Ms(stamp - raw)}ms away");
	}

	/// <summary>
	/// The record loop can wake more than once inside a refresh interval. The grid must still only ever
	/// move forward: a curve reads a negative elapsed time as "not started" and snaps back to its origin.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public void When_Records_Bunch_Up_Then_Frame_Clock_Never_Steps_Back()
	{
		var clock = new Uno.UI.Composition.FrameClock();

		var stamps = new long[80];
		var raw = TimeSpan.TicksPerSecond;
		for (var i = 0; i < stamps.Length; i++)
		{
			// A steady cadence, then a burst of records packed into a single interval.
			raw += i < 40 ? Period : Period / 10;
			stamps[i] = clock.NextTimestamp(raw);
		}

		var worst = Deltas(stamps).Min();
		Assert.IsTrue(worst >= 0, $"the frame clock stepped backwards by {Ms(-worst)}ms");
	}

	/// <summary>
	/// The pause between two motions is not a frame interval. Short bursts separated by pauses would
	/// otherwise fill the window with those pauses, and the same interval back-dates a fling's launch —
	/// so a skewed one starts the curve part-way down its travel.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public void When_Motion_Comes_In_Short_Bursts_Then_Frame_Interval_Is_Not_Skewed()
	{
		var clock = new Uno.UI.Composition.FrameClock();

		var raw = TimeSpan.TicksPerSecond;
		for (var i = 0; i < 40; i++)
		{
			raw += Period;
			clock.NextTimestamp(raw);
		}

		for (var burst = 0; burst < 40; burst++)
		{
			for (var i = 0; i < 2; i++)
			{
				raw += Period;
				clock.NextTimestamp(raw);
			}

			raw += TimeSpan.TicksPerMillisecond * 500;
			clock.NextTimestamp(raw);
		}

		Assert.AreEqual(Period, clock.IntervalInTicks, $"pauses skewed the interval to {Ms(clock.IntervalInTicks)}ms");
	}

	/// <summary>
	/// A burst of slow frames flips the median period, and the one-frame floor has meanwhile banked lead
	/// against the old, shorter one. Unbounded, that lead outlives the burst: the monotonicity clamp turns it
	/// into a run of repeated timestamps, which on screen is a motion frozen until real time catches up.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public void When_Frames_Hitch_Then_Frame_Clock_Does_Not_Freeze()
	{
		const long Vsync = TimeSpan.TicksPerSecond / 60;

		var clock = new Uno.UI.Composition.FrameClock();

		var stamps = new long[200];
		var raw = TimeSpan.TicksPerSecond;
		for (var i = 0; i < stamps.Length; i++)
		{
			// 60Hz, then a hitch slow enough to flip the median yet short of the idle-gap rejection at four
			// periods, then 60Hz again.
			raw += i is >= 60 and < 80 ? 58 * TimeSpan.TicksPerMillisecond : Vsync;
			stamps[i] = clock.NextTimestamp(raw);
		}

		var worst = Deltas(stamps).Min();
		Assert.IsTrue(worst >= 0, $"the frame clock stepped backwards by {Ms(-worst)}ms");

		// A single repeated pair is the grid re-anchoring onto the new period; a longer run is banked lead
		// draining off one frame at a time.
		var frozen = LongestRepeat(stamps);
		Assert.IsTrue(frozen <= 2, $"the frame clock repeated the same timestamp {frozen} times in a row");
	}

	/// <summary>
	/// The sample window deliberately survives a reset, so the first frames of a motion are stepped against
	/// the median of the one before it. A fast motion following a slow one therefore banks a frame of lead
	/// per frame until the window turns over, which is exactly what the lead bound exists for.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public void When_Motion_Follows_A_Slower_One_Then_Frame_Clock_Does_Not_Freeze()
	{
		const long Vsync = TimeSpan.TicksPerSecond / 60;

		var clock = new Uno.UI.Composition.FrameClock();

		var raw = TimeSpan.TicksPerSecond;
		for (var i = 0; i < 20; i++)
		{
			raw += 45 * TimeSpan.TicksPerMillisecond;
			clock.NextTimestamp(raw);
		}

		// A pause, then a second motion at the display's real cadence.
		clock.Reset();
		raw += 300 * TimeSpan.TicksPerMillisecond;

		var stamps = new long[120];
		for (var i = 0; i < stamps.Length; i++)
		{
			raw += Vsync;
			stamps[i] = clock.NextTimestamp(raw);
		}

		var worst = Deltas(stamps).Min();
		Assert.IsTrue(worst > 0, $"the second motion froze or stepped back, worst step was {Ms(worst)}ms");
	}

	private static long[] Deltas(long[] values)
		=> Enumerable.Range(1, values.Length - 1).Select(i => values[i] - values[i - 1]).ToArray();

	private static int LongestRepeat(long[] values)
	{
		int longest = 1, run = 1;
		for (var i = 1; i < values.Length; i++)
		{
			run = values[i] == values[i - 1] ? run + 1 : 1;
			longest = Math.Max(longest, run);
		}

		return longest;
	}

	private static double Ms(long ticks) => ticks / (double)TimeSpan.TicksPerMillisecond;
}
#endif
